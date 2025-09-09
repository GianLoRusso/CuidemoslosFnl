using Cuidemoslos.DAL.Persistence;
using Cuidemoslos.Domain.Entities;
using Cuidemoslos.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Cuidemoslos.DAL.Repositories;
public class PatientRepository : IPatientRepository
{
    private readonly AppDbContext _db;
    public PatientRepository(AppDbContext db) { _db = db; }
    public Task<Patient?> GetAsync(int id) => _db.Patients.FirstOrDefaultAsync(p => p.Id == id);
    public async Task AddAsync(Patient p) => await _db.Patients.AddAsync(p);
    public Task<List<Patient>> ListAsync() => _db.Patients.OrderBy(p => p.FullName).ToListAsync();
}
