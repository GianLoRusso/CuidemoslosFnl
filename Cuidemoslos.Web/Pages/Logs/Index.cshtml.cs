using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cuidemoslos.DAL.Persistence;
using Cuidemoslos.Domain.Entities;

namespace Cuidemoslos.Web.Pages.Logs;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public List<AuditLog> Logs { get; set; } = new();
    public IndexModel(AppDbContext db) { _db = db; }
    public async Task OnGetAsync() =>
        Logs = await _db.AuditLogs.OrderByDescending(l => l.CreatedAt).ToListAsync();
}

