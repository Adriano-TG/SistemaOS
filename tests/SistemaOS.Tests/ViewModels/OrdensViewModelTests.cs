using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SistemaOS.App.ViewModels;
using SistemaOS.Core.Entities;
using SistemaOS.Infrastructure.Data;
using Xunit;

namespace SistemaOS.Tests.ViewModels;

public class OrdensViewModelTests : TestBase
{
    private readonly OrdensViewModel _viewModel;
    private readonly Cliente _cliente;

    public OrdensViewModelTests()
    {
        _cliente = SeedCliente("Cliente Teste");
        _viewModel = GetService<OrdensViewModel>();

        // Wait for async initialization
        Task.Delay(100).Wait();
    }

    [Fact]
    public void Constructor_ShouldLoadClientesAndOrdens()
    {
        // Assert
        _viewModel.Clientes.Should().Contain(c => c.Id == _cliente.Id);
        _viewModel.Ordens.Should().BeEmpty(); // No orders seeded yet
    }

    [Fact]
    public void NovaCommand_ShouldClearFormAndSelection()
    {
        // Arrange
        var os = SeedOrdemServico(_cliente);
        _viewModel.OrdemSelecionada = os;
        _viewModel.Equipamento = "Equipamento Teste";
        _viewModel.Mensagem = "Mensagem anterior";

        // Act
        _viewModel.NovaCommand.Execute(null);

        // Assert
        _viewModel.OrdemSelecionada.Should().BeNull();
        _viewModel.Equipamento.Should().BeEmpty();
        _viewModel.Mensagem.Should().BeEmpty();
        _viewModel.Titulo.Should().Be("Nova ordem de serviço");
    }

    [Fact]
    public void AdicionarItemCommand_ShouldAddItemToForm_WhenValid()
    {
        // Arrange
        _viewModel.NovoItemDescricao = "Peça de teste";
        _viewModel.NovoItemQuantidade = 2;
        _viewModel.NovoItemValor = 50.00m;

        // Act
        _viewModel.AdicionarItemCommand.Execute(null);

        // Assert
        _viewModel.ItensForm.Should().HaveCount(1);
        _viewModel.ItensForm.First().Descricao.Should().Be("Peça de teste");
        _viewModel.ItensForm.First().Quantidade.Should().Be(2);
        _viewModel.ItensForm.First().ValorUnitario.Should().Be(50.00m);
        _viewModel.TotalForm.Should().Be(100.00m);
        _viewModel.NovoItemDescricao.Should().BeEmpty();
    }

    [Fact]
    public void AdicionarItemCommand_ShouldNotAdd_WhenDescricaoEmpty()
    {
        // Arrange
        _viewModel.NovoItemDescricao = "";
        _viewModel.NovoItemQuantidade = 1;

        // Act
        _viewModel.AdicionarItemCommand.Execute(null);

        // Assert
        _viewModel.ItensForm.Should().BeEmpty();
        _viewModel.Mensagem.Should().Be("Informe a descrição e uma quantidade maior que zero.");
    }

    [Fact]
    public void AdicionarItemCommand_ShouldNotAdd_WhenQuantidadeZero()
    {
        // Arrange
        _viewModel.NovoItemDescricao = "Peça";
        _viewModel.NovoItemQuantidade = 0;

        // Act
        _viewModel.AdicionarItemCommand.Execute(null);

        // Assert
        _viewModel.ItensForm.Should().BeEmpty();
        _viewModel.Mensagem.Should().Be("Informe a descrição e uma quantidade maior que zero.");
    }

    [Fact]
    public void RemoverItemCommand_ShouldRemoveSelectedItem()
    {
        // Arrange
        _viewModel.NovoItemDescricao = "Item 1";
        _viewModel.NovoItemQuantidade = 1;
        _viewModel.NovoItemValor = 100;
        _viewModel.AdicionarItemCommand.Execute(null);

        _viewModel.NovoItemDescricao = "Item 2";
        _viewModel.NovoItemValor = 200;
        _viewModel.AdicionarItemCommand.Execute(null);

        _viewModel.ItemSelecionado = _viewModel.ItensForm.First();

        // Act
        _viewModel.RemoverItemCommand.Execute(null);

        // Assert
        _viewModel.ItensForm.Should().HaveCount(1);
        _viewModel.ItensForm.First().Descricao.Should().Be("Item 2");
        _viewModel.TotalForm.Should().Be(200);
    }

    [Fact]
    public void RemoverItemCommand_ShouldDoNothing_WhenNoItemSelected()
    {
        // Arrange
        _viewModel.ItemSelecionado = null;

        // Act
        _viewModel.RemoverItemCommand.Execute(null);

        // Assert
        _viewModel.ItensForm.Should().BeEmpty();
    }

    [Fact]
    public async Task SalvarCommand_ShouldCreateNewOrdem_WhenValid()
    {
        // Arrange
        _viewModel.ClienteSelecionado = _cliente;
        _viewModel.Equipamento = "Notebook HP";
        _viewModel.Marca = "HP";
        _viewModel.NumeroSerie = "SN789";
        _viewModel.DefeitoRelatado = "Tela quebrada";
        _viewModel.Status = StatusOS.Aberta;

        _viewModel.NovoItemDescricao = "Tela 15.6\"";
        _viewModel.NovoItemQuantidade = 1;
        _viewModel.NovoItemValor = 450.00m;
        _viewModel.AdicionarItemCommand.Execute(null);

        // Act
        await _viewModel.SalvarCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Mensagem.Should().Contain("salva com sucesso");
        _viewModel.Ordens.Should().HaveCount(1);

        var savedOs = DbContext.Ordens.Include(o => o.Itens).First();
        savedOs.Equipamento.Should().Be("Notebook HP");
        savedOs.ClienteId.Should().Be(_cliente.Id);
        savedOs.Itens.Should().HaveCount(1);
        savedOs.Itens.First().Descricao.Should().Be("Tela 15.6\"");
    }

    [Fact]
    public async Task SalvarCommand_ShouldFail_WhenClienteNotSelected()
    {
        // Arrange
        _viewModel.ClienteSelecionado = null;
        _viewModel.Equipamento = "Notebook";

        // Act
        await _viewModel.SalvarCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Mensagem.Should().Be("Escolha o cliente e informe o equipamento.");
        DbContext.Ordens.Should().BeEmpty();
    }

    [Fact]
    public async Task SalvarCommand_ShouldFail_WhenEquipamentoEmpty()
    {
        // Arrange
        _viewModel.ClienteSelecionado = _cliente;
        _viewModel.Equipamento = "";

        // Act
        await _viewModel.SalvarCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Mensagem.Should().Be("Escolha o cliente e informe o equipamento.");
    }

    [Fact]
    public async Task SalvarCommand_ShouldUpdateExistingOrdem_WhenOrdemSelecionada()
    {
        // Arrange
        var os = SeedOrdemServico(_cliente, StatusOS.EmAnalise);
        // Detach to avoid entity tracking conflicts when ViewModel reloads
        DbContext.Entry(os).State = EntityState.Detached;
        if (os.Itens != null)
        {
            foreach (var item in os.Itens)
            {
                DbContext.Entry(item).State = EntityState.Detached;
            }
        }
        // Also detach the cliente
        DbContext.Entry(_cliente).State = EntityState.Detached;

        _viewModel.OrdemSelecionada = os;

        // Wait for OnOrdemSelecionadaChanged to load data
        await Task.Delay(50);

        _viewModel.Equipamento = "Notebook Atualizado";
        _viewModel.Status = StatusOS.AguardandoPeca;

        // Act - clear tracker to avoid conflicts with Cliente from CarregarClientesAsync
        ClearChangeTracker();
        await _viewModel.SalvarCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Mensagem.Should().Contain("salva com sucesso");
        var updated = DbContext.Ordens.First();
        updated.Equipamento.Should().Be("Notebook Atualizado");
        updated.Status.Should().Be(StatusOS.AguardandoPeca);
    }

    [Fact]
    public async Task SalvarCommand_ShouldSetDataConclusao_WhenStatusEntregue()
    {
        // Arrange
        _viewModel.ClienteSelecionado = _cliente;
        _viewModel.Equipamento = "Teste";
        _viewModel.Status = StatusOS.Entregue;

        // Act
        await _viewModel.SalvarCommand.ExecuteAsync(null);

        // Assert
        var saved = DbContext.Ordens.First();
        saved.DataConclusao.Should().NotBeNull();
        saved.DataConclusao.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CarregarOrdensAsync_ShouldFilterByTextoPesquisa()
    {
        // Arrange
        SeedOrdemServico(_cliente, StatusOS.Aberta); // ID 1
        var cliente2 = SeedCliente("Outro Cliente");
        SeedOrdemServico(cliente2, StatusOS.Entregue); // ID 2

        // Act
        _viewModel.TextoPesquisa = "Cliente Teste";
        await _viewModel.CarregarOrdensAsync();

        // Assert
        _viewModel.Ordens.Should().HaveCount(1);
        _viewModel.Ordens.First().Cliente.Nome.Should().Be("Cliente Teste");
    }

    [Fact]
    public async Task CarregarOrdensAsync_ShouldFilterByEquipamento()
    {
        // Arrange
        SeedOrdemServico(_cliente); // Equipamento = "Notebook Dell"
        var c2 = SeedCliente("Cliente 2");
        var os2 = new OrdemServico
        {
            ClienteId = c2.Id,
            Cliente = c2,
            Equipamento = "Impressora HP",
            Status = StatusOS.Aberta
        };
        DbContext.Ordens.Add(os2);
        DbContext.SaveChanges();

        // Act
        _viewModel.TextoPesquisa = "Impressora";
        await _viewModel.CarregarOrdensAsync();

        // Assert
        _viewModel.Ordens.Should().HaveCount(1);
        _viewModel.Ordens.First().Equipamento.Should().Be("Impressora HP");
    }

    [Fact]
    public async Task CarregarOrdensAsync_ShouldFilterById_WhenNumeric()
    {
        // Arrange
        var os = SeedOrdemServico(_cliente); // ID will be 1

        // Act
        _viewModel.TextoPesquisa = "1";
        await _viewModel.CarregarOrdensAsync();

        // Assert
        _viewModel.Ordens.Should().HaveCount(1);
        _viewModel.Ordens.First().Id.Should().Be(1);
    }
}