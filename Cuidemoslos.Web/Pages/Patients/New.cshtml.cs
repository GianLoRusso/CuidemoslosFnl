using Cuidemoslos.DAL.Persistence;
using Cuidemoslos.Domain.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cuidemoslos.Web.Pages.Patients;

public class NewModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IValidator<Patient> _validator;

    public NewModel(AppDbContext db, IValidator<Patient> validator)
    {
        _db = db; _validator = validator;
    }

    [BindProperty] public string FullName { get; set; } = "";
    [BindProperty] public string Email { get; set; } = "";

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var entity = new Patient { FullName = FullName?.Trim() ?? "", Email = Email?.Trim() ?? "" };

        var result = await _validator.ValidateAsync(entity);
        if (!result.IsValid)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.ErrorMessage);
            return Page();
        }

        _db.Patients.Add(entity);
        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
