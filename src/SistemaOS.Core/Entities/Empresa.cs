namespace SistemaOS.Core.Entities;

public class Empresa
{
    public int Id { get; set; }
    public string Cnpj { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string NomeFantasia { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;

    // Endereço
    public string Cep { get; set; } = string.Empty;
    public string Logradouro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;

    // O segredo para os PDFs ficarem profissionais
    public string CaminhoLogo { get; set; } = string.Empty;
    // 👇 NOVAS CONFIGURAÇÕES DE IMPRESSÃO 👇
    // 0 = 1 Via, 1 = 2 Vias
    public int TipoOsIndex { get; set; } = 0;

    // 0 = Retrato, 1 = Paisagem
    public int OrientacaoIndex { get; set; } = 0;

    // 0 = A4, 1 = Bobina 80mm, 2 = Bobina 58mm
    public int TamanhoPapelIndex { get; set; } = 0;
}