using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cuidemoslos.DAL.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public string UserName { get; set; } = "";

    // Métricas para las tarjetas
    public int ActivePatients { get; set; }
    public int AlertsToday { get; set; }
    public int MoodLastWeek { get; set; }

    // Filtro
    public int? SelectedPatientId { get; set; }
    public List<PatientOption> Patients { get; set; } = new();

    // Últimos estados de ánimo para la tabla
    public List<MoodRow> LastMoods { get; set; } = new();

    // Aceptamos patientId como querystring: /?patientId=1
    public async Task OnGetAsync(int? patientId)
    {
        UserName = User.Identity?.Name ?? "(sin nombre)";
        SelectedPatientId = patientId;

        var now = DateTime.UtcNow;
        var weekAgo = now.AddDays(-7);
        var today = now.Date;

        // Lista de pacientes para el combo
        Patients = await _db.Patients
            .OrderBy(p => p.FullName)
            .Select(p => new PatientOption
            {
                Id = p.Id,
                Name = p.FullName
            })
            .ToListAsync();

        // Métricas generales (por ahora globales, no filtradas)
        ActivePatients = await _db.Patients
            .CountAsync(p => _db.MoodEntries
                .Any(m => m.PatientId == p.Id && m.CreatedAt >= weekAgo));

        AlertsToday = await _db.MoodEntries
            .CountAsync(m => m.Score <= 2 && m.CreatedAt >= today);

        MoodLastWeek = await _db.MoodEntries
            .CountAsync(m => m.CreatedAt >= weekAgo);

        // Query base de moods
        var moodsQuery = _db.MoodEntries.AsQueryable();

        if (patientId.HasValue)
        {
            moodsQuery = moodsQuery.Where(m => m.PatientId == patientId.Value);
        }

        LastMoods = await moodsQuery
            .OrderByDescending(m => m.CreatedAt)
            .Take(10)
            .Join(
                _db.Patients,
                m => m.PatientId,
                p => p.Id,
                (m, p) => new MoodRow
                {
                    PatientId = p.Id,
                    PatientName = p.FullName,
                    Score = m.Score,
                    CreatedAt = m.CreatedAt,
                    Notes = m.Notes
                })
            .ToListAsync();
    }

    public class MoodRow
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; } = "";
        public int Score { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Notes { get; set; }
    }

    public class PatientOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
