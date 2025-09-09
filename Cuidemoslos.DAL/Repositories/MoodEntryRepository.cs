using Cuidemoslos.BLL.Interfaces;
using Cuidemoslos.DAL.Persistence;
using Cuidemoslos.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Cuidemoslos.DAL.Repositories;
public class MoodEntryRepository : IMoodEntryRepository
{
    private readonly AppDbContext _db;
    public MoodEntryRepository(AppDbContext db) { _db = db; }
    public async Task AddAsync(MoodEntry e) => await _db.MoodEntries.AddAsync(e);
    public Task<List<MoodEntry>> ListByPatientAsync(int pid) =>
        _db.MoodEntries.Where(m => m.PatientId == pid).OrderByDescending(m => m.CreatedAt).ToListAsync();
}
