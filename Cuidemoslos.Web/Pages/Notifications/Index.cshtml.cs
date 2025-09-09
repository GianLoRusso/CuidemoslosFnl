using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cuidemoslos.DAL.Persistence;
using Cuidemoslos.Domain.Entities;

namespace Cuidemoslos.Web.Pages.Notifications;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public List<(Notification N, string Patient)> Items { get; set; } = new();
    public IndexModel(AppDbContext db) { _db = db; }

    public async Task OnGetAsync()
    {
        Items = await _db.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new ValueTuple<Notification, string>(
                n,
                _db.Patients.Where(p => p.Id == n.PatientId).Select(p => p.FullName).First()))
            .ToListAsync();
    }
}

