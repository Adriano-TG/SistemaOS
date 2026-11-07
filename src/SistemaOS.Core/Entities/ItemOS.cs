using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaOS.Core.Entities;

public class ItemOS
{
    public int Id { get; set; }
    public int OrdemServicoId { get; set; }

    // Referência opcional ao produto do estoque
    public int? ProdutoId { get; set; }
    public virtual Produto? Produto { get; set; }

    public string Descricao { get; set; } = string.Empty;
    public decimal Quantidade { get; set; } = 1;
    public decimal ValorUnitario { get; set; }

    // Calculada: não vai para o banco
    public decimal Total => Quantidade * ValorUnitario;
}