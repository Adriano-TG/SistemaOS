using Microsoft.EntityFrameworkCore;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;
using SistemaOS.Infrastructure.Data;

namespace SistemaOS.Infrastructure.Repositories;

public class ClienteRepository : Repository<Cliente>, IClienteRepository
{
    public ClienteRepository(AppDbContext context) : base(context) { }

    public async Task<Cliente?> GetByCpfCnpjAsync(string cpfCnpj)
    {
        var limpo = new string(cpfCnpj.Where(char.IsDigit).ToArray());
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(c => c.CpfCnpj == limpo);
    }

    public async Task<IReadOnlyList<Cliente>> SearchByNomeAsync(string termo)
    {
        return await _dbSet.AsNoTracking()
            .Where(c => c.Nome.Contains(termo))
            .OrderBy(c => c.Nome)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Cliente>> GetAtivosAsync()
    {
        // Cliente não tem propriedade Ativo - retorna todos
        return await _dbSet.AsNoTracking()
            .OrderBy(c => c.Nome)
            .ToListAsync();
    }
}