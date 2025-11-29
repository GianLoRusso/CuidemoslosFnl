namespace Cuidemoslos.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }

    // Tipo de bitácora: Business / System / Exception
    public string Category { get; set; } = "Business";

    // (MoodEntry.Created, Email.Sent, User.Login, etc.)
    public string Action { get; set; } = string.Empty;

    // Nivel: Info / Warning / Error
    public string Level { get; set; } = "Info";

    // Datos adicionales (json, texto, etc.)
    public string? Data { get; set; }

    // Usuario (si hay)
    public string? UserName { get; set; }

    // Fecha/hora
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
