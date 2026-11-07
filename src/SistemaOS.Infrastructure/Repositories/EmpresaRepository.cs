using Microsoft.EntityFrameworkCore;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;
using SistemaOS.Infrastructure.Data;

namespace SistemaOS.Infrastructure.Repositories;

public class EmpresaRepository : Repository<Empresa>, IEmpresaRepository
{
    public EmpresaRepository(AppDbContext context) : base(context) { }

    public async Task<Empresa?> GetEmpresaAsync()
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync();
    }
}