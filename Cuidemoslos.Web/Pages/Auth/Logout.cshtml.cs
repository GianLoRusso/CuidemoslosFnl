using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Cuidemoslos.DAL.Persistence;
using Cuidemoslos.Domain.Entities;

namespace Cuidemoslos.Web.Pages.Auth;

public class LogoutModel : PageModel
{
    public async Task<IActionResult> OnGet([FromServices] AppDbContext db)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Category = "System",
            Action = "User.Logout",
            Level = "Info",
            UserName = User.Identity?.Name
        });
        await db.SaveChangesAsync();

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Auth/Login");
    }
}
