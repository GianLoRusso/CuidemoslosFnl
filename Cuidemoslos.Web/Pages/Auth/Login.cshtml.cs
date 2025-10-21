using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Cuidemoslos.Web.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly IConfiguration _cfg;
    public LoginModel(IConfiguration cfg) { _cfg = cfg; }

    [BindProperty] public string User { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";

    // >>> propiedad que usa la vista
    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        var okUser = _cfg["Auth:User"];
        var okPass = _cfg["Auth:Password"];

        if (User == okUser && Password == okPass)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, okUser ?? "admin") };
            var id = new ClaimsIdentity(claims, "cookie");
            await HttpContext.SignInAsync("cookie", new ClaimsPrincipal(id));
            return Redirect(returnUrl ?? "/");
        }

        ErrorMessage = "Usuario o contraseña inválidos.";
        return Page();
    }
}
