using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Cuidemoslos.Web.Pages.Auth;

[IgnoreAntiforgeryToken] // ← TEMPORAL para evitar 400
public class LoginModel : PageModel
{
    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }

    public void OnGet(string? returnUrl = null) => ReturnUrl = returnUrl ?? "/";

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? "/";

        // TODO: reemplazar por tu validación real
        var ok = Email.Equals("admin@cuidemoslos.local", StringComparison.OrdinalIgnoreCase)
                 && Password == "Admin123!";

        if (!ok)
        {
            // Registrar login en bitácora de sistema
            using (var scope = HttpContext.RequestServices.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<Cuidemoslos.DAL.Persistence.AppDbContext>();
                db.AuditLogs.Add(new Cuidemoslos.Domain.Entities.AuditLog
                {
                    Category = "System",
                    Action = "User.Login",
                    Level = "Info",
                    UserName = Email
                });
                await db.SaveChangesAsync();
            }
            ModelState.AddModelError(string.Empty, "Credenciales inválidas");
            return Page();
        }

        var claims = new[] { new Claim(ClaimTypes.Name, "Admin") };
        var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(id));

        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/");
    }
}
