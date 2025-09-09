using Cuidemoslos.BLL.Interfaces;
using Cuidemoslos.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cuidemoslos.BLL.UseCases.Patients;
public class CreatePatientHandler
{
    private readonly IPatientRepository _repo;
    private readonly IUnitOfWork _uow;
    public CreatePatientHandler(IPatientRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<int> HandleAsync(string fullName, string email)
    {
        var p = new Patient { FullName = fullName, Email = email };
        await _repo.AddAsync(p);
        await _uow.SaveChangesAsync();
        return p.Id;
    }
}
