using Microsoft.EntityFrameworkCore;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;
using SistemaOS.Infrastructure.Data;

namespace SistemaOS.Infrastructure.Repositories;

public class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(AppDbContext context) : base(context) { }

    public async Task<Usuario?> GetByLoginAsync(string login)
    {
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Login == login);
    }

    public async Task<IReadOnlyList<Usuario>> GetAtivosAsync()
    {
        return await _dbSet.AsNoTracking()
            .Where(u => u.Ativo)
            .OrderBy(u => u.Nome)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Usuario>> GetByRoleAsync(RoleUsuario role)
    {
        return await _dbSet.AsNoTracking()
            .Where(u => u.Role == role)
            .OrderBy(u => u.Nome)
            .ToListAsync();
    }
}