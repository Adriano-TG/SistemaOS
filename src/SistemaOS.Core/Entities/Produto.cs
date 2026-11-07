using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaOS.Core.Entities;

public class Produto
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Codigo { get; set; } = string.Empty; // SKU / Código interno

    [Required, MaxLength(200)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descricao { get; set; }

    [MaxLength(50)]
    public string? UnidadeMedida { get; set; } = "UN"; // UN, PC, KG, M, etc.

    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecoCusto { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecoVenda { get; set; }

    public int QuantidadeMinima { get; set; } = 0; // Alerta de estoque mínimo

    [Column(TypeName = "decimal(18,3)")]
    public decimal QuantidadeAtual { get; set; } = 0; // Controlado via MovimentoEstoque (suporta fracionário: 0.5 kg, etc.)

    public bool Ativo { get; set; } = true;

    public DateTime DataCadastro { get; set; } = DateTime.Now;

    public DateTime? DataUltimaMovimentacao { get; set; }

    // Navigation
    public virtual ICollection<MovimentoEstoque> Movimentos { get; set; } = new List<MovimentoEstoque>();
    public virtual ICollection<ItemOS> ItensOS { get; set; } = new List<ItemOS>();

    // Computed properties (not mapped)
    [NotMapped]
    public bool EstoqueBaixo => QuantidadeAtual > 0 && QuantidadeAtual <= QuantidadeMinima && QuantidadeMinima > 0;

    [NotMapped]
    public bool SemEstoque => QuantidadeAtual <= 0;

    [NotMapped]
    public decimal ValorEstoque => QuantidadeAtual * PrecoCusto;
}

public enum TipoMovimentoEstoque
{
    Entrada = 1,
    Saida = 2,
    Ajuste = 3,
    Inicial = 4
}

public class MovimentoEstoque
{
    public int Id { get; set; }

    public int ProdutoId { get; set; }
    public virtual Produto Produto { get; set; } = null!;

    public TipoMovimentoEstoque Tipo { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal Quantidade { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecoUnitario { get; set; }

    [MaxLength(500)]
    public string? Observacao { get; set; }

    public int? OrdemServicoId { get; set; } // Se saída vinculada a OS
    public virtual OrdemServico? OrdemServico { get; set; }

    public int? UsuarioId { get; set; } // Futuro: quem fez o movimento

    public DateTime DataMovimento { get; set; } = DateTime.Now;

    [NotMapped]
    public string TipoDescricao => Tipo switch
    {
        TipoMovimentoEstoque.Entrada => "Entrada",
        TipoMovimentoEstoque.Saida => "Saída",
        TipoMovimentoEstoque.Ajuste => "Ajuste",
        TipoMovimentoEstoque.Inicial => "Estoque Inicial",
        _ => "Desconhecido"
    };

    [NotMapped]
    public decimal ValorTotal => Quantidade * PrecoUnitario;
}