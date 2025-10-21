using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Cuidemoslos.DAL.Persistence;
using Cuidemoslos.Domain.Entities;
using FluentValidation;
using FluentValidation.Results;

namespace Cuidemoslos.Web.Pages.Patients;

public class NewModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IValidator<Patient> _validator;

    public NewModel(AppDbContext db, IValidator<Patient> validator)
    {
        _db = db;
        _validator = validator;
    }

    [BindProperty] public string FullName { get; set; } = "";
    [BindProperty] public string Email { get; set; } = "";

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        // Crear objeto paciente con los datos del formulario
        var patient = new Patient
        {
            FullName = FullName?.Trim(),
            Email = Email?.Trim()
        };

        // Validar con FluentValidation
        ValidationResult result = await _validator.ValidateAsync(patient);
        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.ErrorMessage);
            return Page();
        }

        try
        {
            // Guardar en DB
            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();

            // Registrar auditoría
            _db.AuditLogs.Add(new AuditLog
            {
                Action = "Patient.Created",
                Level = "Info",
                Data = $"Nuevo paciente: {patient.FullName} ({patient.Email})"
            });
            await _db.SaveChangesAsync();

            TempData["Message"] = $"Paciente {patient.FullName} registrado correctamente.";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            // Registrar error
            _db.AuditLogs.Add(new AuditLog
            {
                Action = "Patient.Error",
                Level = "Error",
                Data = ex.Message
            });
            await _db.SaveChangesAsync();

            ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar el paciente.");
            return Page();
        }
    }
}
