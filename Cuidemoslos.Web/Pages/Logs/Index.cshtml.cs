using Cuidemoslos.DAL.Persistence;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Cuidemoslos.Web.Pages.Logs;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public record Row(DateTime CreatedAt, string Level, string Action, string? Data);
    public List<Row> Items { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Items = await _db.AuditLogs
            .OrderByDescending(a => a.CreatedAt)
            .Take(500)
            .Select(a => new Row(a.CreatedAt, a.Level, a.Action, a.Data))
            .ToListAsync();
    }
}
