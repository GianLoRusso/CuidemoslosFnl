using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cuidemoslos.Web.Pages.Outbox;

public class IndexModel : PageModel
{
    public List<(string Name, string Url, DateTime Created)> Items { get; set; } = new();

    public void OnGet()
    {
        var wwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var outbox = Path.Combine(wwwroot, "outbox");
        Directory.CreateDirectory(outbox);

        Items = Directory.EnumerateFiles(outbox, "*.html")
            .Select(p => (Name: Path.GetFileName(p),
                          Url: "/outbox/" + Path.GetFileName(p),
                          Created: System.IO.File.GetCreationTimeUtc(p)))
            .OrderByDescending(x => x.Created)
            .ToList();
    }
}
