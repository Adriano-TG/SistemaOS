namespace SistemaOS.Core.Entities;

public class OrdemServico
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    // Técnico responsável (opcional)
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public string Equipamento { get; set; } = string.Empty;
    public string? Marca { get; set; }
    public string? NumeroSerie { get; set; }
    public bool EmGarantia { get; set; }

    public string? DefeitoRelatado { get; set; }
    public string? LaudoTecnico { get; set; }

    public StatusOS Status { get; set; } = StatusOS.Aberta;
    public DateTime DataAbertura { get; set; } = DateTime.Now;
    public DateTime? DataConclusao { get; set; }

    public List<ItemOS> Itens { get; set; } = new();

    // Calculada: soma dos itens
    public decimal ValorTotal => Itens.Sum(i => i.Total);
}