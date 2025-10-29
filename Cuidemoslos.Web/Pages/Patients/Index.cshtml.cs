using Cuidemoslos.DAL.Persistence;
using Cuidemoslos.Web.ViewModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Cuidemoslos.Web.Pages.Patients;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public List<PatientListItemVM> Patients { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Patients = await _db.Patients
            .Select(p => new PatientListItemVM(
                p.Id, p.FullName, p.Email, p.CreatedAt,
                _db.Notifications.Count(n => n.PatientId == p.Id)))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}

