namespace SistemaOS.Core.Entities;

public class Cliente
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? CpfCnpj { get; set; }
    public string? Telefone { get; set; }
    public string? Endereco { get; set; }
    public string WhatsApp { get; set; } = string.Empty;
    public DateTime DataCadastro { get; set; } = DateTime.Now;
}
