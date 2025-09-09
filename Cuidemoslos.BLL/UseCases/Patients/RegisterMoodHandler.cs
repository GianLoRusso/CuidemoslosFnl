using Cuidemoslos.BLL.Interfaces;
using Cuidemoslos.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cuidemoslos.BLL.UseCases.Patients;
public class RegisterMoodHandler
{
    private readonly IMoodEntryRepository _moods;
    private readonly IUnitOfWork _uow;
    public RegisterMoodHandler(IMoodEntryRepository moods, IUnitOfWork uow) { _moods = moods; _uow = uow; }

    public async Task<(MoodEntry entry, bool critical)> HandleAsync(int patientId, int score, string? notes)
    {
        var entry = new MoodEntry { PatientId = patientId, Score = score, Notes = notes };
        await _moods.AddAsync(entry);
        var critical = score <= 2;
        await _uow.SaveChangesAsync();
        return (entry, critical);
    }
}
