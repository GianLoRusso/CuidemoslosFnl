using Cuidemoslos.BLL.Interfaces;
using Cuidemoslos.DAL.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cuidemoslos.DAL.UnitOfWork;
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    public UnitOfWork(AppDbContext db) { _db = db; }
    public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
}
