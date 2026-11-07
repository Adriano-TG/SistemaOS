using SistemaOS.Core.Entities;

namespace SistemaOS.Core.Interfaces;

public interface IEstoqueService
{
    Task<bool> BaixarEstoqueAsync(int ordemServicoId);
    Task<bool> VerificarEstoqueSuficienteAsync(int produtoId, decimal quantidade);
    Task<Produto?> GetProdutoAsync(int produtoId);
    Task<bool> AdicionarMovimentoAsync(int produtoId, TipoMovimentoEstoque tipo, decimal quantidade, decimal precoUnitario, string? observacao = null);
    Task<decimal> GetEstoqueAtualAsync(int produtoId);
}