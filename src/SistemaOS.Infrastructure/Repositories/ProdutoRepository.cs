using Microsoft.EntityFrameworkCore;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;
using SistemaOS.Infrastructure.Data;

namespace SistemaOS.Infrastructure.Repositories;

public class ProdutoRepository : Repository<Produto>, IProdutoRepository
{
    public ProdutoRepository(AppDbContext context) : base(context) { }

    public async Task<Produto?> GetByCodigoAsync(string codigo)
    {
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Codigo == codigo);
    }

    public async Task<IReadOnlyList<Produto>> GetAtivosAsync()
    {
        return await _dbSet.AsNoTracking()
            .Where(p => p.Ativo)
            .OrderBy(p => p.Nome)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Produto>> GetBaixoEstoqueAsync()
    {
        return await _dbSet.AsNoTracking()
            .Where(p => p.Ativo && p.QuantidadeAtual > 0 && p.QuantidadeAtual <= p.QuantidadeMinima && p.QuantidadeMinima > 0)
            .OrderBy(p => p.Nome)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Produto>> GetSemEstoqueAsync()
    {
        return await _dbSet.AsNoTracking()
            .Where(p => p.Ativo && p.QuantidadeAtual <= 0)
            .OrderBy(p => p.Nome)
            .ToListAsync();
    }

    public async Task<decimal> GetValorTotalEstoqueAsync()
    {
        return await _dbSet.AsNoTracking()
            .Where(p => p.Ativo)
            .SumAsync(p => p.QuantidadeAtual * p.PrecoCusto);
    }

    public async Task<IReadOnlyList<Produto>> SearchAsync(string termo)
    {
        return await _dbSet.AsNoTracking()
            .Where(p => p.Ativo &&
                (p.Codigo.ToLower().Contains(termo.ToLower()) ||
                 p.Nome.ToLower().Contains(termo.ToLower()) ||
                 (p.Descricao != null && p.Descricao.ToLower().Contains(termo.ToLower()))))
            .OrderBy(p => p.Nome)
            .ToListAsync();
    }

    public async Task<bool> CanDeleteAsync(int produtoId)
    {
        // Verifica se tem movimentos de estoque
        var temMovimentos = await _context.MovimentosEstoque
            .AsNoTracking()
            .AnyAsync(m => m.ProdutoId == produtoId);

        if (temMovimentos) return false;

        // Verifica se tem itens em ordens de serviço
        var temItensOS = await _context.Itens
            .AsNoTracking()
            .AnyAsync(i => i.ProdutoId == produtoId);

        return !temItensOS;
    }

    public async Task<int> GetBaixoEstoqueCountAsync()
    {
        return await _dbSet.AsNoTracking()
            .CountAsync(p => p.Ativo && p.QuantidadeAtual > 0 && p.QuantidadeAtual <= p.QuantidadeMinima && p.QuantidadeMinima > 0);
    }
}