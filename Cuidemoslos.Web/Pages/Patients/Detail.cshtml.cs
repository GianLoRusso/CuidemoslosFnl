using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cuidemoslos.DAL.Persistence;
using Cuidemoslos.Domain.Entities;
using Cuidemoslos.Services.Email;
using Cuidemoslos.BLL.UseCases.Patients; // si no lo usás, podés quitarlo

namespace Cuidemoslos.Web.Pages.Patients;

public class DetailModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IEmailSender _email;
    public Patient? Patient { get; set; }
    public List<MoodEntry> Entries { get; set; } = new();

    [BindProperty] public int Score { get; set; } = 2;
    [BindProperty] public string? Notes { get; set; }
    [BindProperty] public string ProEmail { get; set; } = "profesional@demo.local";

    public DetailModel(AppDbContext db, IEmailSender email) { _db = db; _email = email; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == id);
        if (Patient == null) return RedirectToPage("Index");
        Entries = await _db.MoodEntries.Where(m => m.PatientId == id)
                                       .OrderByDescending(m => m.CreatedAt)
                                       .ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var p = await _db.Patients.FindAsync(id);
        if (p == null) return RedirectToPage("Index");

        var entry = new MoodEntry { PatientId = p.Id, Score = Score, Notes = Notes };
        _db.MoodEntries.Add(entry);
        _db.AuditLogs.Add(new AuditLog { Action = "MoodEntry.Created", Level = "Info", Data = $"PatientId={p.Id};Score={Score}" });

        try
        {
            if (Score <= 2)
            {
                var subject = $"Alerta Cuidémoslos: Estado crítico de {p.FullName}";
                var body = $"<p>El paciente <b>{p.FullName}</b> reportó estado <b>Muy bajo ({Score})</b>.</p>";
                await _email.SendAsync(ProEmail, subject, body);
                _db.Notifications.Add(new Notification { PatientId = p.Id, Subject = subject, Body = body });
                _db.AuditLogs.Add(new AuditLog { Action = "Email.Sent", Level = "Info", Data = $"To={ProEmail}" });
            }
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _db.AuditLogs.Add(new AuditLog { Action = "Email.Error", Level = "Error", Data = ex.Message });
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }
}

