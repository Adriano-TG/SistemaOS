using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;

namespace SistemaOS.App.Services;

public class PdfService
{
    private readonly IEmpresaRepository _empresaRepository;
    private static readonly CultureInfo Br = new("pt-BR");
    private const string ViaCliente = "1ª VIA - CLIENTE";
    private const string ViaLoja = "2ª VIA - LOJA";

    public PdfService(IEmpresaRepository empresaRepository)
    {
        _empresaRepository = empresaRepository;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<string> GerarOrdemServicoAsync(OrdemServico os)
    {
        var pastaDestino = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SistemaOS", "PDFs");
        Directory.CreateDirectory(pastaDestino);

        var caminhoArquivo = Path.Combine(pastaDestino, $"OS_{os.Id}_{DateTime.Now:yyyyMMddHHmmss}.pdf");

        var empresa = await _empresaRepository.GetEmpresaAsync();

        // Configurações da tela: 0 = A4, 1 = bobina 80mm, 2 = bobina 58mm
        var bobina = empresa?.TamanhoPapelIndex is 1 or 2;
        var larguraBobina = empresa?.TamanhoPapelIndex == 2 ? 164.4f : 226.8f;
        var paisagem = !bobina && empresa?.OrientacaoIndex == 1;
        var duasVias = empresa?.TipoOsIndex == 1;

        Document.Create(container =>
        {
            // ---------- BOBINA: cada via é uma tira; a impressora corta entre elas ----------
            if (bobina)
            {
                var vias = duasVias
                    ? new string?[] { ViaCliente, ViaLoja }
                    : new string?[] { null };

                foreach (var rotulo in vias)
                {
                    container.Page(page =>
                    {
                        page.ContinuousSize(larguraBobina);
                        page.Margin(0.3f, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(8));

                        page.Content().Element(c =>
                            ComporVia(c, os, empresa, rotulo, bobina: true, compacto: true));
                    });
                }

                return;
            }

            // ---------- A4 ----------
            container.Page(page =>
            {
                page.Size(paisagem ? PageSizes.A4.Landscape() : PageSizes.A4.Portrait());
                page.Margin(duasVias && !paisagem ? 1.0f : 1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(duasVias ? 9 : 10));

                if (!duasVias)
                {
                    // Uma via: cabeçalho fixo no topo de cada página
                    page.Header().Element(c => ComporCabecalho(c, os, empresa, bobina: false));
                    page.Content().Element(c => ComporConteudo(c, os, bobina: false, compacto: false));
                    page.Footer().Element(c => ComporRodape(c));
                }
                else if (paisagem)
                {
                    // Paisagem: as duas vias lado a lado
                    page.Content().Row(row =>
                    {
                        row.RelativeItem().PaddingRight(10).Element(c =>
                            ComporVia(c, os, empresa, ViaCliente, bobina: false, compacto: true));

                        row.ConstantItem(1).LineVertical(1).LineColor(Colors.Grey.Medium);

                        row.RelativeItem().PaddingLeft(10).Element(c =>
                            ComporVia(c, os, empresa, ViaLoja, bobina: false, compacto: true));
                    });
                }
                else
                {
                    // Retrato: uma via em cima e outra embaixo, cada uma inteira na sua página
                    page.Content().Column(col =>
                    {
                        col.Item().ShowEntire().Element(c =>
                            ComporVia(c, os, empresa, ViaCliente, bobina: false, compacto: true));

                        col.Item().PaddingVertical(8).AlignCenter()
                            .Text("- - - - - - - - - -  corte aqui  - - - - - - - - - -")
                            .FontSize(7).FontColor(Colors.Grey.Medium);

                        col.Item().ShowEntire().Element(c =>
                            ComporVia(c, os, empresa, ViaLoja, bobina: false, compacto: true));
                    });
                }
            });
        })
        .GeneratePdf(caminhoArquivo);

        return caminhoArquivo;
    }

    public async Task<string> GerarLaudoAsync(OrdemServico os) => await GerarOrdemServicoAsync(os);

    /// <summary>
    /// Gera cupom de venda PDV (bobina térmica)
    /// </summary>
    public async Task<string> GerarCupomPdvAsync(OrdemServico os, Cliente? cliente, string formaPagamento, decimal valorRecebido, decimal troco)
    {
        var pastaDestino = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SistemaOS", "PDFs");
        Directory.CreateDirectory(pastaDestino);

        var caminhoArquivo = Path.Combine(pastaDestino, $"CUPOM_{os.Id}_{DateTime.Now:yyyyMMddHHmmss}.pdf");

        var empresa = await _empresaRepository.GetEmpresaAsync();

        // Cupom sempre em bobina 80mm (226.8 pontos = 80mm)
        const float larguraBobina = 226.8f;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.ContinuousSize(larguraBobina);
                page.Margin(0.3f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Courier New"));

                page.Content().Column(col =>
                {
                    col.Spacing(2);

                    // Cabeçalho empresa
                    ComporCabecalhoCupom(col, empresa);

                    // Linha separadora
                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);

                    // Dados da venda
                    col.Item().Text($"CUPOM NAO FISCAL - VENDA #{os.Id:D6}").Bold().AlignCenter();
                    col.Item().Text($"Data: {os.DataAbertura:dd/MM/yyyy HH:mm}").AlignCenter();

                    if (cliente != null)
                    {
                        col.Item().Text($"Cliente: {cliente.Nome}").FontSize(7);
                        if (!string.IsNullOrWhiteSpace(cliente.CpfCnpj))
                            col.Item().Text($"CPF/CNPJ: {cliente.CpfCnpj}").FontSize(7);
                    }

                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);

                    // Itens
                    foreach (var item in os.Itens)
                    {
                        col.Item().Text(item.Descricao).FontSize(8);
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"{item.Quantidade:0.##} x {item.ValorUnitario.ToString("C2", Br)}").FontSize(7);
                            row.ConstantItem(60).AlignRight().Text(item.Total.ToString("C2", Br)).Bold().FontSize(8);
                        });
                    }

                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);

                    // Totais
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("SUBTOTAL:").Bold();
                        row.ConstantItem(60).AlignRight().Text(os.Itens.Sum(i => i.Total).ToString("C2", Br)).Bold();
                    });

                    if (os.Itens.Sum(i => i.Total) != os.ValorTotal)
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("DESCONTO:").FontSize(7);
                            row.ConstantItem(60).AlignRight().Text((os.Itens.Sum(i => i.Total) - os.ValorTotal).ToString("C2", Br)).FontSize(7);
                        });
                    }

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("TOTAL:").Bold().FontSize(10);
                        row.ConstantItem(60).AlignRight().Text(os.ValorTotal.ToString("C2", Br)).Bold().FontSize(10);
                    });

                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);

                    // Pagamento
                    col.Item().Text($"Pagamento: {formaPagamento}").FontSize(8);
                    if (formaPagamento == "Dinheiro" || formaPagamento == "Misto")
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Recebido:").FontSize(7);
                            row.ConstantItem(60).AlignRight().Text(valorRecebido.ToString("C2", Br)).FontSize(7);
                        });
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Troco:").FontSize(7);
                            row.ConstantItem(60).AlignRight().Text(troco.ToString("C2", Br)).FontSize(7);
                        });
                    }

                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);

                    // Rodapé
                    col.Item().PaddingTop(4).AlignCenter().Text("Obrigado pela preferência!").Bold().FontSize(9);
                    col.Item().AlignCenter().Text("Volte sempre!").FontSize(8);

                    if (!string.IsNullOrWhiteSpace(empresa.Telefone))
                        col.Item().PaddingTop(2).AlignCenter().Text($"Tel: {empresa.Telefone}").FontSize(7);

                    col.Item().PaddingTop(4).AlignCenter().Text($"Sistema OS - {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(6).FontColor(Colors.Grey.Darken1);
                });
            });
        })
        .GeneratePdf(caminhoArquivo);

        return caminhoArquivo;
    }

    private void ComporCabecalhoCupom(ColumnDescriptor col, Empresa empresa)
    {
        var nome = !string.IsNullOrWhiteSpace(empresa.NomeFantasia) ? empresa.NomeFantasia : empresa.RazaoSocial;

        col.Item().AlignCenter().Text(nome).Bold().FontSize(11);

        if (!string.IsNullOrWhiteSpace(empresa.Cnpj))
            col.Item().AlignCenter().Text("CNPJ: " + empresa.Cnpj).FontSize(7);

        var endereco = Juntar(" - ",
            Juntar(", ", empresa.Logradouro, empresa.Numero),
            empresa.Bairro,
            Juntar("/", empresa.Cidade, empresa.Uf));

        if (!string.IsNullOrWhiteSpace(endereco))
            col.Item().AlignCenter().Text(endereco).FontSize(7);

        if (!string.IsNullOrWhiteSpace(empresa.Telefone))
            col.Item().AlignCenter().Text("Tel: " + empresa.Telefone).FontSize(7);
    }

    public static void Abrir(string caminho) =>
        Process.Start(new ProcessStartInfo(caminho) { UseShellExecute = true });

    // =====================  Uma via completa  =====================

    private static void ComporVia(IContainer container, OrdemServico os, Empresa? empresa,
                                  string? rotulo, bool bobina, bool compacto)
    {
        container.Column(col =>
        {
            col.Spacing(compacto ? 4 : 8);

            if (rotulo != null)
            {
                col.Item().AlignRight().Text(rotulo)
                    .FontSize(bobina ? 7 : 8).Bold().FontColor(Colors.Grey.Darken1);
            }

            col.Item().Element(c => ComporCabecalho(c, os, empresa, bobina, compacto));
            col.Item().Element(c => ComporConteudo(c, os, bobina, compacto));
        });
    }

    // =====================  Cabeçalho  =====================

    private static void ComporCabecalho(IContainer container, OrdemServico os, Empresa? empresa,
                                        bool bobina, bool compacto = false)
    {
        var nome = NomeDaEmpresa(empresa);

        if (bobina)
        {
            container.Column(col =>
            {
                col.Item().AlignCenter().Text(nome).FontSize(11).SemiBold();

                if (!string.IsNullOrWhiteSpace(empresa?.Cnpj))
                    col.Item().AlignCenter().Text("CNPJ: " + empresa!.Cnpj).FontSize(7);
                if (!string.IsNullOrWhiteSpace(empresa?.Telefone))
                    col.Item().AlignCenter().Text("Tel: " + empresa!.Telefone).FontSize(7);

                col.Item().PaddingTop(3).AlignCenter()
                    .Text($"O.S. Nº {os.Id:D5}  -  {os.DataAbertura:dd/MM/yy}").FontSize(10).Bold();
            });
            return;
        }

        container.Row(row =>
        {
            row.RelativeItem().Row(innerRow =>
            {
                if (empresa != null && !string.IsNullOrEmpty(empresa.CaminhoLogo) && File.Exists(empresa.CaminhoLogo))
                {
                    innerRow.ConstantItem(compacto ? 55 : 80).Image(empresa.CaminhoLogo);
                    innerRow.ConstantItem(10);
                }

                innerRow.RelativeItem().Column(col =>
                {
                    col.Item().Text(nome).FontSize(compacto ? 13 : 16).SemiBold();

                    if (!string.IsNullOrWhiteSpace(empresa?.Cnpj))
                        col.Item().Text("CNPJ: " + empresa!.Cnpj);

                    if (empresa != null)
                    {
                        var linha1 = Juntar(" - ", Juntar(", ", empresa.Logradouro, empresa.Numero), empresa.Bairro);
                        var linha2 = Juntar(" - ",
                            Juntar("/", empresa.Cidade, empresa.Uf),
                            string.IsNullOrWhiteSpace(empresa.Telefone) ? null : "Tel: " + empresa.Telefone);

                        if (linha1.Length > 0) col.Item().Text(linha1);
                        if (linha2.Length > 0) col.Item().Text(linha2);
                    }
                });
            });

            row.ConstantItem(compacto ? 120 : 150).Column(col =>
            {
                col.Item().Text("ORDEM DE SERVIÇO").FontSize(compacto ? 11 : 14).Bold().FontColor(Colors.Blue.Darken2);
                col.Item().Text($"Nº {os.Id:D5}").FontSize(compacto ? 15 : 18).Bold().FontColor(Colors.Red.Medium);
                col.Item().Text($"Data: {os.DataAbertura:dd/MM/yyyy}");
            });
        });
    }

    // =====================  Conteúdo  =====================

    private static void ComporConteudo(IContainer container, OrdemServico os, bool bobina, bool compacto)
    {
        var itens = os.Itens ?? new List<ItemOS>();
        var total = itens.Sum(i => i.Total);

        container.Column(col =>
        {
            col.Spacing(bobina ? 4 : compacto ? 5 : 10);

            col.Item().Element(c => Secao(c, "DADOS DO CLIENTE", s =>
            {
                s.Item().Text($"Nome: {os.Cliente?.Nome}");

                var fone = Juntar(" / ", os.Cliente?.WhatsApp, os.Cliente?.Telefone);
                if (fone.Length > 0)
                    s.Item().Text($"Telefone: {fone}");

                if (!bobina && !string.IsNullOrWhiteSpace(os.Cliente?.Endereco))
                    s.Item().Text($"Endereço: {os.Cliente!.Endereco}");
            }));

            col.Item().Element(c => Secao(c, "EQUIPAMENTO", s =>
            {
                s.Item().Text("Aparelho: " + Juntar(" - ", os.Equipamento, os.Marca));

                if (!string.IsNullOrWhiteSpace(os.NumeroSerie))
                    s.Item().Text($"Nº de série: {os.NumeroSerie}");

                s.Item().Text("Em garantia: " + (os.EmGarantia ? "SIM" : "NÃO"))
                    .FontColor(os.EmGarantia ? Colors.Green.Darken2 : Colors.Black);
            }));

            col.Item().Element(c => Secao(c, "DEFEITO RELATADO", s =>
                s.Item().Text(string.IsNullOrWhiteSpace(os.DefeitoRelatado) ? "-" : os.DefeitoRelatado)));

            if (itens.Count > 0)
            {
                col.Item().Element(c => Secao(c, "PEÇAS E SERVIÇOS", s =>
                {
                    if (bobina)
                    {
                        foreach (var i in itens)
                        {
                            s.Item().Text(i.Descricao);
                            s.Item().AlignRight().Text(
                                $"{i.Quantidade.ToString("0.##", Br)} x {i.ValorUnitario.ToString("C2", Br)} = {i.Total.ToString("C2", Br)}")
                                .FontSize(7);
                        }
                        return;
                    }

                    s.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(5);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Element(Celula).Text("Descrição").SemiBold();
                            h.Cell().Element(Celula).AlignRight().Text("Qtd").SemiBold();
                            h.Cell().Element(Celula).AlignRight().Text("Valor unit.").SemiBold();
                            h.Cell().Element(Celula).AlignRight().Text("Total").SemiBold();
                        });

                        foreach (var i in itens)
                        {
                            t.Cell().Element(Celula).Text(i.Descricao);
                            t.Cell().Element(Celula).AlignRight().Text(i.Quantidade.ToString("0.##", Br));
                            t.Cell().Element(Celula).AlignRight().Text(i.ValorUnitario.ToString("C2", Br));
                            t.Cell().Element(Celula).AlignRight().Text(i.Total.ToString("C2", Br));
                        }
                    });
                }));
            }

            col.Item().AlignRight()
                .Text($"TOTAL: {total.ToString("C2", Br)}")
                .Bold().FontSize(bobina ? 11 : 13).FontColor(Colors.Blue.Darken2);

            col.Item().PaddingTop(bobina ? 14 : compacto ? 22 : 50).Column(c =>
            {
                c.Item().LineHorizontal(1);
                c.Item().PaddingTop(2).AlignCenter().Text("Assinatura do cliente").FontSize(bobina ? 7 : 8);
            });
        });
    }

    private static void ComporRodape(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Darken1));
            x.Span("Página ");
            x.CurrentPageNumber();
            x.Span(" de ");
            x.TotalPages();
            x.Span(" - Sistema OS");
        });
    }

    // =====================  Auxiliares  =====================

    private static void Secao(IContainer c, string titulo, Action<ColumnDescriptor> conteudo)
    {
        c.Column(col =>
        {
            col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(2)
                .Text(titulo).SemiBold();
            col.Item().PaddingTop(3).Column(conteudo);
        });
    }

    private static IContainer Celula(IContainer c) =>
        c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2);

    private static string NomeDaEmpresa(Empresa? e) =>
        !string.IsNullOrWhiteSpace(e?.NomeFantasia) ? e!.NomeFantasia : e?.RazaoSocial ?? string.Empty;

    private static string Juntar(string separador, params string?[] partes) =>
        string.Join(separador, partes.Where(p => !string.IsNullOrWhiteSpace(p)));
}