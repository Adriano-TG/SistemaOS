using Microsoft.EntityFrameworkCore;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;
using SistemaOS.Infrastructure.Data;

namespace SistemaOS.Infrastructure.Services;

public class EstoqueService : IEstoqueService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public EstoqueService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<bool> BaixarEstoqueAsync(int ordemServicoId)
    {
        try
        {
            using var db = _dbFactory.CreateDbContext();

            var os = await db.Ordens
                .Include(o => o.Itens)
                .ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(o => o.Id == ordemServicoId);

            if (os == null) return false;

            // First, verify all items have sufficient stock
            foreach (var item in os.Itens)
            {
                if (!item.ProdutoId.HasValue) continue;

                var produto = await db.Produtos.FindAsync(item.ProdutoId.Value);
                if (produto == null) continue;

                if (produto.QuantidadeAtual < item.Quantidade)
                    return false; // Estoque insuficiente - aborta toda a operação
            }

            // All items have sufficient stock, proceed with the baixas
            foreach (var item in os.Itens)
            {
                if (!item.ProdutoId.HasValue) continue;

                var produto = await db.Produtos.FindAsync(item.ProdutoId.Value);
                if (produto == null) continue;

                produto.QuantidadeAtual -= item.Quantidade;

                var movimento = new MovimentoEstoque
                {
                    ProdutoId = produto.Id,
                    Tipo = TipoMovimentoEstoque.Saida,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = produto.PrecoCusto,
                    Observacao = $"Baixa automática - OS #{os.Id}",
                    DataMovimento = DateTime.Now,
                    OrdemServicoId = os.Id
                };

                db.MovimentosEstoque.Add(movimento);
                produto.DataUltimaMovimentacao = DateTime.Now;
            }

            await db.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> VerificarEstoqueSuficienteAsync(int produtoId, decimal quantidade)
    {
        using var db = _dbFactory.CreateDbContext();
        var produto = await db.Produtos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == produtoId);
        return produto != null && produto.QuantidadeAtual >= quantidade;
    }

    public async Task<Produto?> GetProdutoAsync(int produtoId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Produtos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == produtoId);
    }

    public async Task<bool> AdicionarMovimentoAsync(int produtoId, TipoMovimentoEstoque tipo, decimal quantidade, decimal precoUnitario, string? observacao = null)
    {
        try
        {
            using var db = _dbFactory.CreateDbContext();

            var produto = await db.Produtos.FindAsync(produtoId);
            if (produto == null) return false;

            switch (tipo)
            {
                case TipoMovimentoEstoque.Entrada:
                    produto.QuantidadeAtual += quantidade;
                    break;
                case TipoMovimentoEstoque.Saida:
                    if (produto.QuantidadeAtual < quantidade) return false;
                    produto.QuantidadeAtual -= quantidade;
                    break;
                case TipoMovimentoEstoque.Ajuste:
                    produto.QuantidadeAtual = quantidade;
                    break;
                case TipoMovimentoEstoque.Inicial:
                    // Inicial: quantidade já foi definida no produto, apenas registra o movimento
                    break;
            }

            var movimento = new MovimentoEstoque
            {
                ProdutoId = produto.Id,
                Tipo = tipo,
                Quantidade = quantidade,
                PrecoUnitario = precoUnitario > 0 ? precoUnitario : produto.PrecoCusto,
                Observacao = observacao,
                DataMovimento = DateTime.Now
            };

            db.MovimentosEstoque.Add(movimento);
            produto.DataUltimaMovimentacao = DateTime.Now;

            await db.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<decimal> GetEstoqueAtualAsync(int produtoId)
    {
        using var db = _dbFactory.CreateDbContext();
        var produto = await db.Produtos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == produtoId);
        return produto?.QuantidadeAtual ?? 0;
    }
}