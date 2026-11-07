using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SistemaOS.App.ViewModels;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;
using SistemaOS.Infrastructure.Data;
using Xunit;

namespace SistemaOS.Tests.ViewModels;

public class DashboardViewModelTests : TestBase
{
    private readonly DashboardViewModel _viewModel;
    private readonly Cliente _cliente;

    public DashboardViewModelTests()
    {
        _cliente = SeedCliente("Cliente Teste", "11999999999", "12345678909");
        _viewModel = GetService<DashboardViewModel>();

        // Wait for async initialization
        Task.Delay(500).Wait();
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Assert
        _viewModel.TotalOrdensMes.Should().Be(0);
        _viewModel.FaturamentoMes.Should().Be(0);
        _viewModel.TicketMedio.Should().Be(0);
        _viewModel.TempoMedioAtendimentoHoras.Should().Be(0);
        _viewModel.SlaCumprimentoPercentual.Should().Be(0);
        _viewModel.OrdensPorStatus.Should().BeEmpty();
        _viewModel.FaturamentoUltimos7Dias.Should().HaveCount(7); // 7 days initialized
        _viewModel.TopClientes.Should().BeEmpty();
        _viewModel.TopProdutos.Should().BeEmpty();
    }

    [Fact]
    public async Task CarregarDashboardAsync_ShouldCalculateKPIs_WhenOrdensExist()
    {
        // Arrange
        var produto = new Produto
        {
            Codigo = "PROD001",
            Nome = "Produto Teste",
            PrecoCusto = 50,
            PrecoVenda = 100,
            QuantidadeAtual = 10,
            QuantidadeMinima = 2,
            UnidadeMedida = "UN"
        };
        DbContext.Produtos.Add(produto);
        DbContext.SaveChanges();

        // Use dates within current month (yesterday and today) - 1 day = 24 hours
        var dataAbertura = DateTime.Today.AddDays(-1); // Yesterday
        var dataConclusao = DateTime.Today; // Today

        var os = new OrdemServico
        {
            ClienteId = _cliente.Id,
            Cliente = _cliente,
            Equipamento = "Notebook",
            Status = StatusOS.Entregue,
            DataAbertura = dataAbertura,
            DataConclusao = dataConclusao,
            Itens = new List<ItemOS>
            {
                new() { Descricao = "Serviço", Quantidade = 1, ValorUnitario = 200, ProdutoId = produto.Id, Produto = produto }
            }
        };
        DbContext.Ordens.Add(os);
        DbContext.SaveChanges();

        // Debug: Check what's in the database
        var dbOrdens = await DbContext.Ordens.ToListAsync();
        var dbItens = await DbContext.Itens.ToListAsync();
        if (dbOrdens.Count == 0 || dbItens.Count == 0)
        {
            throw new Exception($"DB empty: Ordens={dbOrdens.Count}, Itens={dbItens.Count}");
        }
        foreach (var o in dbOrdens)
        {
            if (o.Itens == null || o.Itens.Count == 0)
            {
                throw new Exception($"OS {o.Id} has no Itens: {o.Itens?.Count ?? -1}");
            }
            foreach (var i in o.Itens)
            {
                if (i.Total != i.Quantidade * i.ValorUnitario)
                {
                    throw new Exception($"Item {i.Id} Total mismatch: {i.Total} vs {i.Quantidade * i.ValorUnitario}");
                }
            }
        }

        // Act
        await _viewModel.CarregarDashboardCommand.ExecuteAsync(null);

        // Assert
        _viewModel.TotalOrdensMes.Should().Be(1);
        _viewModel.OrdensEntregues.Should().Be(1);
        _viewModel.FaturamentoMes.Should().Be(200);
        _viewModel.TicketMedio.Should().Be(200);
        // 1 day = ~24 hours
        _viewModel.TempoMedioAtendimentoHoras.Should().BeApproximately(24, 2);
        _viewModel.SlaCumprimentoPercentual.Should().Be(100); // Within 48h SLA
        _viewModel.ClientesAtivos.Should().Be(1);
        _viewModel.OrdensPorStatus.Should().ContainSingle(s => s.Status == "Entregue" && s.Count == 1);
    }

    [Fact]
    public async Task CarregarDashboardAsync_ShouldCalculateSLA_WhenWithin48Hours()
    {
        // Arrange
        // Use dates within current month (yesterday and today)
        var dataAbertura = DateTime.Today.AddDays(-1); // Yesterday
        var dataConclusao = dataAbertura.AddHours(10); // 10 hours later (within 48h)

        var os = new OrdemServico
        {
            ClienteId = _cliente.Id,
            Cliente = _cliente,
            Equipamento = "Celular",
            Status = StatusOS.Entregue,
            DataAbertura = dataAbertura,
            DataConclusao = dataConclusao,
            Itens = new List<ItemOS>
            {
                new() { Descricao = "Reparo", Quantidade = 1, ValorUnitario = 150 }
            }
        };
        DbContext.Ordens.Add(os);
        DbContext.SaveChanges();

        // Act
        await _viewModel.CarregarDashboardCommand.ExecuteAsync(null);

        // Assert
        _viewModel.TempoMedioAtendimentoHoras.Should().BeLessThan(48);
        _viewModel.SlaCumprimentoPercentual.Should().Be(100);
    }

    [Fact]
    public async Task CarregarDashboardAsync_ShouldCalculateFaturamentoUltimos7Dias()
    {
        // Arrange
        // Use dates within last 7 days (all within current month) - use DateTime.Today for consistent Kind
        var hoje = DateTime.Today;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
        var seteDiasAtras = hoje.AddDays(-6);

        // Create 3 orders on different days within the last 7 days, ensuring DataAbertura >= inicioMes
        // Day 1: 5 days ago (within last 7 days)
        var data1Conclusao = hoje.AddDays(-5);
        var data1Abertura = data1Conclusao.AddDays(-1);
        if (data1Abertura < inicioMes) data1Abertura = inicioMes;

        // Day 2: 3 days ago
        var data2Conclusao = hoje.AddDays(-3);
        var data2Abertura = data2Conclusao.AddDays(-1);
        if (data2Abertura < inicioMes) data2Abertura = inicioMes;

        // Day 3: Today
        var data3Conclusao = hoje;
        var data3Abertura = data3Conclusao.AddDays(-1);
        if (data3Abertura < inicioMes) data3Abertura = inicioMes;

        var os1 = new OrdemServico
        {
            ClienteId = _cliente.Id,
            Cliente = _cliente,
            Equipamento = "Equip 1",
            Status = StatusOS.Entregue,
            DataAbertura = data1Abertura,
            DataConclusao = data1Conclusao,
            Itens = new List<ItemOS> { new() { Descricao = "Serviço", Quantidade = 1, ValorUnitario = 100 } }
        };

        var os2 = new OrdemServico
        {
            ClienteId = _cliente.Id,
            Cliente = _cliente,
            Equipamento = "Equip 2",
            Status = StatusOS.Entregue,
            DataAbertura = data2Abertura,
            DataConclusao = data2Conclusao,
            Itens = new List<ItemOS> { new() { Descricao = "Serviço", Quantidade = 1, ValorUnitario = 200 } }
        };

        var os3 = new OrdemServico
        {
            ClienteId = _cliente.Id,
            Cliente = _cliente,
            Equipamento = "Equip 3",
            Status = StatusOS.Entregue,
            DataAbertura = data3Abertura,
            DataConclusao = data3Conclusao,
            Itens = new List<ItemOS> { new() { Descricao = "Serviço", Quantidade = 1, ValorUnitario = 300 } }
        };

        DbContext.Ordens.AddRange(os1, os2, os3);
        DbContext.SaveChanges();

        // Act
        await _viewModel.CarregarDashboardCommand.ExecuteAsync(null);

        // Assert
        _viewModel.FaturamentoUltimos7Dias.Should().HaveCount(7);
        var totalFaturado = _viewModel.FaturamentoUltimos7Dias.Sum(f => f.Valor);
        totalFaturado.Should().Be(600); // 100 + 200 + 300
    }

    [Fact]
    public async Task CarregarDashboardAsync_ShouldCalculateTopClientes()
    {
        // Arrange
        var cliente2 = SeedCliente("Cliente VIP", "11888888888", "98765432109");

        // Use dates within current month - use past dates (yesterday and day before)
        var baseDate = DateTime.Today.AddDays(-1); // Yesterday

        var os1 = new OrdemServico
        {
            ClienteId = _cliente.Id,
            Cliente = _cliente,
            Equipamento = "PC",
            Status = StatusOS.Entregue,
            DataAbertura = baseDate,
            DataConclusao = baseDate.AddDays(1),
            Itens = new List<ItemOS> { new() { Descricao = "A", Quantidade = 1, ValorUnitario = 500 } }
        };

        var os2 = new OrdemServico
        {
            ClienteId = cliente2.Id,
            Cliente = cliente2,
            Equipamento = "Mac",
            Status = StatusOS.Entregue,
            DataAbertura = baseDate.AddDays(-1), // Day before yesterday
            DataConclusao = baseDate,
            Itens = new List<ItemOS> { new() { Descricao = "B", Quantidade = 1, ValorUnitario = 1500 } }
        };

        DbContext.Ordens.AddRange(os1, os2);
        DbContext.SaveChanges();

        // Act
        await _viewModel.CarregarDashboardCommand.ExecuteAsync(null);

        // Assert
        _viewModel.TopClientes.Should().HaveCount(2);
        _viewModel.TopClientes.First().Nome.Should().Be("Cliente VIP");
        _viewModel.TopClientes.First().TotalFaturado.Should().Be(1500);
    }

    [Fact]
    public async Task CarregarDashboardAsync_ShouldCalculateTopProdutos()
    {
        // Arrange
        var prod1 = new Produto { Codigo = "P1", Nome = "Produto A", PrecoCusto = 10, PrecoVenda = 20, QuantidadeAtual = 10, QuantidadeMinima = 1, UnidadeMedida = "UN" };
        var prod2 = new Produto { Codigo = "P2", Nome = "Produto B", PrecoCusto = 20, PrecoVenda = 40, QuantidadeAtual = 5, QuantidadeMinima = 1, UnidadeMedida = "UN" };
        DbContext.Produtos.AddRange(prod1, prod2);
        DbContext.SaveChanges();

        // Use dates within current month (yesterday and today)
        var baseDate = DateTime.Today.AddDays(-1); // Yesterday

        var os = new OrdemServico
        {
            ClienteId = _cliente.Id,
            Cliente = _cliente,
            Equipamento = "Teste",
            Status = StatusOS.Entregue,
            DataAbertura = baseDate,
            DataConclusao = baseDate.AddDays(2),
            Itens = new List<ItemOS>
            {
                new() { Descricao = "A", Quantidade = 5, ValorUnitario = 20, ProdutoId = prod1.Id, Produto = prod1 },
                new() { Descricao = "B", Quantidade = 2, ValorUnitario = 40, ProdutoId = prod2.Id, Produto = prod2 }
            }
        };
        DbContext.Ordens.Add(os);
        DbContext.SaveChanges();

        // Act
        await _viewModel.CarregarDashboardCommand.ExecuteAsync(null);

        // Assert
        _viewModel.TopProdutos.Should().HaveCount(2);
        _viewModel.TopProdutos.First().Nome.Should().Be("Produto A");
        _viewModel.TopProdutos.First().QuantidadeVendida.Should().Be(5);
    }

    [Fact]
    public async Task AtualizarCommand_ShouldReloadData()
    {
        // Arrange
        var baseDate = DateTime.Today.AddDays(-1); // Yesterday

        var os = new OrdemServico
        {
            ClienteId = _cliente.Id,
            Cliente = _cliente,
            Equipamento = "Teste",
            Status = StatusOS.Entregue,
            DataAbertura = baseDate,
            DataConclusao = baseDate.AddDays(2),
            Itens = new List<ItemOS> { new() { Descricao = "A", Quantidade = 1, ValorUnitario = 300 } }
        };
        DbContext.Ordens.Add(os);
        DbContext.SaveChanges();

        await _viewModel.CarregarDashboardCommand.ExecuteAsync(null);
        var faturamentoInicial = _viewModel.FaturamentoMes;

        // Act - add another order (today)
        var os2 = new OrdemServico
        {
            ClienteId = _cliente.Id,
            Cliente = _cliente,
            Equipamento = "Teste 2",
            Status = StatusOS.Entregue,
            DataAbertura = DateTime.Today,
            DataConclusao = DateTime.Today,
            Itens = new List<ItemOS> { new() { Descricao = "B", Quantidade = 1, ValorUnitario = 200 } }
        };
        DbContext.Ordens.Add(os2);
        DbContext.SaveChanges();

        await _viewModel.AtualizarCommand.ExecuteAsync(null);

        // Assert
        _viewModel.FaturamentoMes.Should().Be(faturamentoInicial + 200);
    }

    [Fact]
    public void FormattedProperties_ShouldReturnCorrectStrings()
    {
        // Arrange
        _viewModel.FaturamentoMes = 1234.56m;
        _viewModel.TicketMedio = 617.28m;
        _viewModel.TempoMedioAtendimentoHoras = 24.5;
        _viewModel.SlaCumprimentoPercentual = 85.5;

        // Act & Assert
        _viewModel.FaturamentoMesFormatado.Should().Be("R$ 1.234,56");
        _viewModel.TicketMedioFormatado.Should().Be("R$ 617,28");
        _viewModel.TempoMedioFormatado.Should().Be("24,5h");
        _viewModel.SlaFormatado.Should().Be("85,5%");
    }
}