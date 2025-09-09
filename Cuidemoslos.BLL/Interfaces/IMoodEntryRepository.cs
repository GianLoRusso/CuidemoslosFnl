using Cuidemoslos.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cuidemoslos.BLL.Interfaces;
public interface IMoodEntryRepository
{
    Task AddAsync(MoodEntry entry);
    Task<List<MoodEntry>> ListByPatientAsync(int patientId);
}
