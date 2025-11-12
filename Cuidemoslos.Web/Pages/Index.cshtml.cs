using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

[Authorize] // respeta el FallbackPolicy
public class IndexModel : PageModel
{
    public string UserName { get; set; } = "";

    public void OnGet()
    {
        UserName = User.Identity?.Name ?? "(sin nombre)";
    }
}
