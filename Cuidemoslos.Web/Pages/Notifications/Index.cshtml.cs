using Cuidemoslos.DAL.Persistence;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Cuidemoslos.Web.Pages.Notifications;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public record Row(DateTime CreatedAt, string Subject, string Patient);
    public List<Row> Items { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Items = await _db.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .Take(200)
            .Select(n => new Row(n.CreatedAt, n.Subject, _db.Patients.Where(p => p.Id == n.PatientId).Select(p => p.FullName).First()))
            .ToListAsync();
    }
}
