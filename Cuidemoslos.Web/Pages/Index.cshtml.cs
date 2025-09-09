using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cuidemoslos.DAL.Persistence;

namespace Cuidemoslos.Web.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public int PatientsCount { get; set; }
    public int AlertsToday { get; set; }
    public IndexModel(AppDbContext db) { _db = db; }

    public async Task OnGetAsync()
    {
        PatientsCount = await _db.Patients.CountAsync();
        var today = DateTime.UtcNow.Date;
        AlertsToday = await _db.Notifications.CountAsync(n => n.CreatedAt >= today && n.CreatedAt < today.AddDays(1));
    }
}
