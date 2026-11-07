using System.Collections.Generic;
using SistemaOS.Core.Entities;

namespace SistemaOS.Core.Interfaces;

public interface IOrdemServicoRepository : IRepository<OrdemServico>
{
    Task<IReadOnlyList<OrdemServico>> GetByClienteAsync(int clienteId);
    Task<IReadOnlyList<OrdemServico>> GetByStatusAsync(StatusOS status);
    Task<IReadOnlyList<OrdemServico>> SearchAsync(string? termo, int take = 200);
    Task<OrdemServico?> GetCompletaAsync(int id);
    Task<OrdemServico?> GetByIdWithItensAsync(int id);
    Task<IReadOnlyList<OrdemServico>> SearchByTermoAsync(string termo);
    Task<IReadOnlyList<OrdemServico>> GetRecentAsync(int take);
    Task<Produto?> GetProdutoByIdAsync(int produtoId);
    Task<IReadOnlyList<OrdemServico>> GetByDateRangeAsync(DateTime inicio, DateTime fim);
    Task<IReadOnlyList<OrdemServico>> GetAllWithItensAsync();
}