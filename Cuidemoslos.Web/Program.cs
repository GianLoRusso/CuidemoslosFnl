using System;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Cuidemoslos.DAL.Persistence;
using Cuidemoslos.Domain.Entities;
using Cuidemoslos.Domain.Validation;
using Cuidemoslos.Services.DependencyInjection;
using Cuidemoslos.Services.Email;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Auth0.AspNetCore.Authentication;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Razor Pages (permitir /Auth sin login)
builder.Services.AddRazorPages(o =>
{
    o.Conventions.AllowAnonymousToFolder("/Auth");
});

// Validadores
builder.Services.AddValidatorsFromAssemblyContaining<PatientValidator>();

// EF Core / PostgreSQL
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Servicios propios
builder.Services.AddCuidemoslosServices();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Cuidemoslos API",
        Version = "v1",
        Description = "Endpoints para la app del paciente y panel del profesional"
    });
});

// Healthchecks
builder.Services.AddHealthChecks();

// ====== AUTH0 + COOKIES (único bloque de autenticación) ======
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme; // "Cookies"
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = Auth0Constants.AuthenticationScheme;               // "Auth0"
    })
    .AddCookie(options =>
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    })
    .AddAuth0WebAppAuthentication(options =>
    {
        // OJO: ahora leemos "Auth0:..." (no "Auth")
        options.Domain = builder.Configuration["Auth0:Domain"];
        options.ClientId = builder.Configuration["Auth0:ClientId"];
        options.ClientSecret = builder.Configuration["Auth0:ClientSecret"];

        // Si en Auth0 configuraste /callback, descomenta:
        // options.CallbackPath = "/callback";
        // Si NO lo seteás, el callback por defecto es /signin-auth0 (recomendado).
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy; // exige login por defecto
});

var app = builder.Build();

// Proxy headers (Render)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Archivos estáticos
app.UseStaticFiles();

// Middleware de excepciones + bitácora
app.Use(async (ctx, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Unhandled exception");

        using var scope = ctx.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.AuditLogs.Add(new AuditLog
        {
            Category = "Exception",
            Action = "Unhandled",
            Level = "Error",
            Data = ex.ToString(),
            UserName = ctx.User.Identity?.Name
        });
        await db.SaveChangesAsync();

        ctx.Response.StatusCode = 500;
        await ctx.Response.WriteAsync("Error interno del servidor");
    }
});

// Swagger (público)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cuidemoslos API v1");
    c.RoutePrefix = "swagger";
});
app.MapSwagger().AllowAnonymous();

// Auth
app.UseAuthentication();
app.UseAuthorization();

// API Key para /api/*
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api"))
    {
        var provided = ctx.Request.Headers["X-Api-Key"].FirstOrDefault();
        var expected = app.Configuration["API_KEY"];

        if (string.IsNullOrEmpty(expected) || provided != expected)
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await ctx.Response.WriteAsync("Unauthorized");
            return;
        }
    }
    await next();
});

// Healthcheck (público)
app.MapHealthChecks("/health").AllowAnonymous();

// Migración DB al iniciar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ====== LOGIN / LOGOUT con Auth0 ======

// Redirige al login de Auth0
app.MapGet("/Auth/Login", async (HttpContext ctx) =>
{
    var returnUrl = ctx.Request.Query["ReturnUrl"].FirstOrDefault() ?? "/";
    var props = new LoginAuthenticationPropertiesBuilder()
        .WithRedirectUri(returnUrl)
        .Build();
    await ctx.ChallengeAsync(Auth0Constants.AuthenticationScheme, props);
    return Results.Empty;
}).AllowAnonymous();

// Logout: primero cookie, luego Auth0 con RedirectUri
app.MapGet("/Auth/Logout", async (HttpContext ctx) =>
{
    var returnUrl = "/";
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    await ctx.SignOutAsync(Auth0Constants.AuthenticationScheme, new AuthenticationProperties
    {
        RedirectUri = returnUrl
    });

    return Results.Empty;
}).AllowAnonymous();

// ====== API: Estado de ánimo ======
app.MapPost("/api/mood", async (
    AppDbContext db,
    IEmailSender email,
    int patientId, int score, string? notes, string proEmail) =>
{
    var p = await db.Patients.FindAsync(patientId);
    if (p == null) return Results.NotFound("Paciente no existe.");

    var entry = new MoodEntry { PatientId = patientId, Score = score, Notes = notes };
    db.MoodEntries.Add(entry);
    db.AuditLogs.Add(new AuditLog
    {
        Category = "Business",
        Action = "MoodEntry.Created",
        Level = "Info",
        Data = $"PatientId={patientId};Score={score}"
    });

    try
    {
        if (score <= 2)
        {
            var subject = $"Alerta Cuidémoslos: Estado crítico de {p.FullName}";
            var body = $"<p>El paciente <b>{p.FullName}</b> reportó estado <b>Muy bajo ({score})</b>.</p>";

            await email.SendAsync(proEmail, subject, body);

            db.Notifications.Add(new Notification
            {
                PatientId = patientId,
                Subject = subject,
                Body = body
            });

            db.AuditLogs.Add(new AuditLog
            {
                Category = "Business",
                Action = "Email.Sent",
                Level = "Info",
                Data = $"To={proEmail}"
            });
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { ok = true });
    }
    catch (Exception ex)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Category = "Exception",
            Action = "Email.Error",
            Level = "Error",
            Data = ex.Message
        });
        await db.SaveChangesAsync();
        Log.Error(ex, "Error al enviar notificación");
        return Results.Problem("No se pudo enviar notificación.");
    }
}).AllowAnonymous();

// REST adicionales de demo
app.MapPost("/api/auth/login", (string email, string password) =>
{
    if (email == "admin@cuidemoslos.local" && password == "Admin123!")
        return Results.Ok(new { token = "demo-token", email });
    return Results.Unauthorized();
});

app.MapGet("/api/patients/{id}/mood", async (AppDbContext db, int id) =>
{
    var moods = await db.MoodEntries
        .Where(m => m.PatientId == id)
        .OrderByDescending(m => m.CreatedAt)
        .Take(30)
        .ToListAsync();
    return Results.Ok(moods);
});

app.MapGet("/api/professionals/{id}/dashboard", async (AppDbContext db, int id) =>
{
    var totalPatients = await db.Patients.CountAsync();
    var alerts = await db.Notifications.CountAsync();
    return Results.Ok(new { totalPatients, alerts });
});

app.MapGet("/api/reports/export", async (AppDbContext db, DateTime from, DateTime to) =>
{
    var logs = await db.AuditLogs
        .Where(a => a.CreatedAt >= from && a.CreatedAt <= to)
        .ToListAsync();
    return Results.Ok(logs);
});

// Razor Pages
app.MapRazorPages();

app.Run();
