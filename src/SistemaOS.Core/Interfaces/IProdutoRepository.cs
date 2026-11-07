using SistemaOS.Core.Entities;

namespace SistemaOS.Core.Interfaces;

public interface IProdutoRepository : IRepository<Produto>
{
    Task<Produto?> GetByCodigoAsync(string codigo);
    Task<IReadOnlyList<Produto>> GetAtivosAsync();
    Task<IReadOnlyList<Produto>> GetBaixoEstoqueAsync();
    Task<IReadOnlyList<Produto>> GetSemEstoqueAsync();
    Task<decimal> GetValorTotalEstoqueAsync();
    Task<IReadOnlyList<Produto>> SearchAsync(string termo);
    Task<bool> CanDeleteAsync(int produtoId);
    Task<int> GetBaixoEstoqueCountAsync();
}