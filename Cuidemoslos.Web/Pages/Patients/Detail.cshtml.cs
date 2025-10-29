using Cuidemoslos.DAL.Persistence;
using Cuidemoslos.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Cuidemoslos.Web.Pages.Patients;

public class DetailsModel : PageModel
{
    private readonly AppDbContext _db;
    public DetailsModel(AppDbContext db) => _db = db;

    public PatientDetailVM? Patient { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var p = await _db.Patients.FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        var mood = await _db.MoodEntries
            .Where(m => m.PatientId == id)
            .OrderByDescending(m => m.CreatedAt)
            .Take(20)
            .Select(m => new MoodItemVM(m.CreatedAt, m.Score, m.Notes))
            .ToListAsync();

        var notis = await _db.Notifications
            .Where(n => n.PatientId == id)
            .OrderByDescending(n => n.CreatedAt)
            .Take(20)
            .Select(n => new NotificationItemVM(n.CreatedAt, n.Subject))
            .ToListAsync();

        Patient = new PatientDetailVM(p.Id, p.FullName, p.Email, p.CreatedAt, mood, notis);
        return Page();
    }
}
