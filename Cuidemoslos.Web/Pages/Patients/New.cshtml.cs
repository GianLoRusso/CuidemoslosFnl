using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Cuidemoslos.DAL.Persistence;
using Cuidemoslos.Domain.Entities;

namespace Cuidemoslos.Web.Pages.Patients;

public class NewModel : PageModel
{
    private readonly AppDbContext _db;
    public NewModel(AppDbContext db) { _db = db; }

    [BindProperty] public string FullName { get; set; } = "";
    [BindProperty] public string Email { get; set; } = "";

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        // Validación mínima
        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email))
        {
            ModelState.AddModelError(string.Empty, "Nombre y Email son obligatorios.");
            return Page(); // vuelve a mostrar el formulario con error
        }

        //  validar formato de Email más a fondo con regex o FluentValidation

        _db.Patients.Add(new Patient { FullName = FullName, Email = Email });
        await _db.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}
