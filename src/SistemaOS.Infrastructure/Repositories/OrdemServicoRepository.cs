using Microsoft.EntityFrameworkCore;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;
using SistemaOS.Infrastructure.Data;

namespace SistemaOS.Infrastructure.Repositories;

public class OrdemServicoRepository : Repository<OrdemServico>, IOrdemServicoRepository
{
    public OrdemServicoRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<OrdemServico>> GetByClienteAsync(int clienteId)
    {
        return await _dbSet.AsNoTracking()
            .Include(o => o.Cliente)
            .Where(o => o.ClienteId == clienteId)
            .OrderByDescending(o => o.Id)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<OrdemServico>> GetByStatusAsync(StatusOS status)
    {
        return await _dbSet.AsNoTracking()
            .Include(o => o.Cliente)
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.Id)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<OrdemServico>> SearchAsync(string? termo, int take = 200)
    {
        var query = _dbSet.AsNoTracking().Include(o => o.Cliente).AsQueryable();

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = termo.ToLower();
            bool isNumero = int.TryParse(t, out int idPesquisa);

            query = query.Where(o =>
                (isNumero && o.Id == idPesquisa) ||
                (o.Cliente != null && o.Cliente.Nome.ToLower().Contains(t)) ||
                (o.Equipamento != null && o.Equipamento.ToLower().Contains(t)));
        }

        return await query.OrderByDescending(o => o.Id).Take(take).ToListAsync();
    }

    public async Task<OrdemServico?> GetCompletaAsync(int id)
    {
        return await _dbSet
            .Include(o => o.Cliente)
            .Include(o => o.Itens)
                .ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<OrdemServico?> GetByIdWithItensAsync(int id)
    {
        return await _dbSet
            .Include(o => o.Cliente)
            .Include(o => o.Itens)
                .ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IReadOnlyList<OrdemServico>> SearchByTermoAsync(string termo)
    {
        var t = termo.ToLower();
        bool isNumero = int.TryParse(t, out int idPesquisa);

        return await _dbSet.AsNoTracking()
            .Include(o => o.Cliente)
            .Where(o =>
                (isNumero && o.Id == idPesquisa) ||
                (o.Cliente != null && o.Cliente.Nome.ToLower().Contains(t)) ||
                (o.Equipamento != null && o.Equipamento.ToLower().Contains(t)))
            .OrderByDescending(o => o.Id)
            .Take(200)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<OrdemServico>> GetRecentAsync(int take)
    {
        return await _dbSet.AsNoTracking()
            .Include(o => o.Cliente)
            .OrderByDescending(o => o.Id)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Produto?> GetProdutoByIdAsync(int produtoId)
    {
        return await _context.Produtos.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == produtoId);
    }

    public async Task<IReadOnlyList<OrdemServico>> GetByDateRangeAsync(DateTime inicio, DateTime fim)
    {
        return await _dbSet.AsNoTracking()
            .Include(o => o.Cliente)
            .Include(o => o.Itens)
                .ThenInclude(i => i.Produto)
            .Where(o => o.DataAbertura >= inicio && o.DataAbertura <= fim)
            .OrderByDescending(o => o.Id)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<OrdemServico>> GetAllWithItensAsync()
    {
        return await _dbSet.AsNoTracking()
            .Include(o => o.Cliente)
            .Include(o => o.Itens)
                .ThenInclude(i => i.Produto)
            .OrderByDescending(o => o.Id)
            .ToListAsync();
    }
}