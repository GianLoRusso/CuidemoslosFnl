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

// ====== AUTH0 con callback explícito /callback ======
builder.Services.AddAuth0WebAppAuthentication(options =>
{
    options.Domain = builder.Configuration["Auth0:Domain"];
    options.ClientId = builder.Configuration["Auth0:ClientId"];
    options.ClientSecret = builder.Configuration["Auth0:ClientSecret"];
    options.CallbackPath = "/callback";
});

// Configuración del cookie
builder.Services.Configure<CookieAuthenticationOptions>(
    CookieAuthenticationDefaults.AuthenticationScheme,
    options =>
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
    });

// Autorización por defecto (todo requiere login salvo AllowAnonymous)
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

var app = builder.Build();

// Proxy headers (Render / reverso)
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

// ====== API KEY para /api/* ======
var apiKeyFromConfig = app.Configuration["API_KEY"];

if (!string.IsNullOrWhiteSpace(apiKeyFromConfig))
{
    app.Use(async (ctx, next) =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            var provided = ctx.Request.Headers["CLAVE_SUPER_SECRETA_MOBILE"].FirstOrDefault();

            if (provided != apiKeyFromConfig)
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsync("Unauthorized");
                return;
            }
        }

        await next();
    });
}
else
{
    Log.Warning("API_KEY no está configurada. Los endpoints /api NO están protegidos por API Key.");
}

// ====== API: Login móvil de paciente por email ======
app.MapPost("/api/mobile/login", async (AppDbContext db, string email) =>
{
    
    var patient = await db.Patients
        .FirstOrDefaultAsync(p => p.Email == email);

    if (patient == null)
        return Results.NotFound("Paciente no registrado");

    
    var proEmail = patient.Email ?? "profesional@test.com";

    return Results.Ok(new
    {
        id = patient.Id,
        fullName = patient.FullName,
        proEmail
    });
}).AllowAnonymous();


// Healthcheck (público)
app.MapHealthChecks("/health").AllowAnonymous();

// Migración DB al iniciar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ====== LOGIN / LOGOUT con Auth0 ======

// Login
app.MapGet("/Auth/Login", async (HttpContext ctx) =>
{
    var returnUrl = ctx.Request.Query["ReturnUrl"].FirstOrDefault();

    bool isLocal = !string.IsNullOrEmpty(returnUrl) &&
                   returnUrl.StartsWith("/") &&
                   !returnUrl.StartsWith("/Auth/Login", StringComparison.OrdinalIgnoreCase) &&
                   !returnUrl.StartsWith("/Auth/Logout", StringComparison.OrdinalIgnoreCase) &&
                   !returnUrl.StartsWith("/callback", StringComparison.OrdinalIgnoreCase);

    if (!isLocal) returnUrl = "/";

    var props = new LoginAuthenticationPropertiesBuilder()
        .WithRedirectUri(returnUrl)
        .Build();

    await ctx.ChallengeAsync(Auth0Constants.AuthenticationScheme, props);
    return Results.Empty;
}).AllowAnonymous();

// Logout
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
