using Cuidemoslos.DAL.Persistence;
using Cuidemoslos.Domain.Entities;
using Cuidemoslos.Domain.Validation;
using Cuidemoslos.Services.DependencyInjection;
using Cuidemoslos.Services.Email;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Razor Pages (login anónimo) ---
builder.Services.AddRazorPages(o =>
{
    o.Conventions.AllowAnonymousToFolder("/Auth");
});

// --- Validadores (FluentValidation) ---
builder.Services.AddValidatorsFromAssemblyContaining<PatientValidator>();

// --- EF Core / PostgreSQL ---
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// --- Servicios propios (correo, etc.) ---
builder.Services.AddCuidemoslosServices();

// --- Swagger ---
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

// --- Healthchecks ---
builder.Services.AddHealthChecks();

// --- Auth por cookies (toda la web protegida excepto /Auth/*) ---
builder.Services.AddAuthentication("cookie")
    .AddCookie("cookie", o =>
    {
        o.LoginPath = "/Auth/Login";
        o.AccessDeniedPath = "/Auth/Login";
    });

builder.Services.AddAuthorization(options =>
{
    // Exigir login por defecto en todo
    options.FallbackPolicy = options.DefaultPolicy;
});

var app = builder.Build();

// --- Archivos estáticos (css/js/img) ---
app.UseStaticFiles();

// --- Auth/Authorization ---
app.UseAuthentication();
app.UseAuthorization();

// --- API Key middleware para /api/* ---
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api"))
    {
        var provided = ctx.Request.Headers["X-Api-Key"].FirstOrDefault();
        var expected = app.Configuration["API_KEY"]; // defínelo en Render

        if (string.IsNullOrEmpty(expected) || provided != expected)
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await ctx.Response.WriteAsync("Unauthorized");
            return;
        }
    }
    await next();
});

// --- Swagger (público) ---
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cuidemoslos API v1");
    c.RoutePrefix = "swagger";
});
app.MapSwagger().AllowAnonymous();

// --- Healthcheck (público) ---
app.MapHealthChecks("/health").AllowAnonymous();

// --- Migración DB al iniciar ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// --- Endpoint API (app paciente) ---
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
            Action = "Email.Error",
            Level = "Error",
            Data = ex.Message
        });
        await db.SaveChangesAsync();
        return Results.Problem("No se pudo enviar notificación.");
    }
});

// --- Razor Pages ---
app.MapRazorPages();

app.Run();
