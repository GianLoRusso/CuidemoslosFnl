using Cuidemoslos.DAL.Persistence;
using Cuidemoslos.Domain.Entities;
using Cuidemoslos.Services.DependencyInjection;
using Cuidemoslos.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite("Data Source=cuidemoslos.db"));
builder.Services.AddCuidemoslosServices();
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
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cuidemoslos API v1");
    c.RoutePrefix = "swagger"; 
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.MapPost("/api/mood", async (
    AppDbContext db,
    IEmailSender email,
    int patientId, int score, string? notes, string proEmail) =>
{
    var p = await db.Patients.FindAsync(patientId);
    if (p == null) return Results.NotFound("Paciente no existe.");

    var entry = new MoodEntry { PatientId = patientId, Score = score, Notes = notes };
    db.MoodEntries.Add(entry);
    db.AuditLogs.Add(new AuditLog { Action = "MoodEntry.Created", Level = "Info", Data = $"PatientId={patientId};Score={score}" });

    try
    {
        if (score <= 2)
        {
            var subject = $"Alerta Cuidémoslos: Estado crítico de {p.FullName}";
            var body = $"<p>El paciente <b>{p.FullName}</b> reportó estado <b>Muy bajo ({score})</b>.</p>";
            await email.SendAsync(proEmail, subject, body);
            db.Notifications.Add(new Notification { PatientId = patientId, Subject = subject, Body = body });
            db.AuditLogs.Add(new AuditLog { Action = "Email.Sent", Level = "Info", Data = $"To={proEmail}" });
        }
        await db.SaveChangesAsync();
        return Results.Ok(new { ok = true });
    }
    catch (Exception ex)
    {
        db.AuditLogs.Add(new AuditLog { Action = "Email.Error", Level = "Error", Data = ex.Message });
        await db.SaveChangesAsync();
        return Results.Problem("No se pudo enviar notificación.");
    }
});
app.MapHealthChecks("/health");
app.UseStaticFiles();
app.MapRazorPages();
app.Run();

