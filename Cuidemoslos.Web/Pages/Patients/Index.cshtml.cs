using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cuidemoslos.DAL.Persistence;
using Cuidemoslos.Domain.Entities;

namespace Cuidemoslos.Web.Pages.Patients;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public List<Patient> Patients { get; set; } = new();

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public async Task OnGetAsync()
    {
        Patients = await _db.Patients
            .OrderBy(p => p.FullName)
            .ToListAsync();
    }
}
