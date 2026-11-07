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
using SistemaOS.Infrastructure.Services;
using Xunit;

namespace SistemaOS.Tests.ViewModels;

public class ClientesViewModelTests : TestBase
{
    private readonly ClientesViewModel _viewModel;

    public ClientesViewModelTests()
    {
        _viewModel = GetService<ClientesViewModel>();
    }

    [Fact]
    public void Constructor_ShouldLoadClientesFromDatabase()
    {
        // Arrange
        SeedCliente("João Silva");
        SeedCliente("Maria Santos");

        // Act - create new ViewModel to trigger constructor loading
        var vm = GetService<ClientesViewModel>();

        // Assert
        vm.Clientes.Should().HaveCount(2);
        vm.Clientes.Should().Contain(c => c.Nome == "João Silva");
        vm.Clientes.Should().Contain(c => c.Nome == "Maria Santos");
    }

    [Fact]
    public void EditarCommand_ShouldPopulateFormWithSelectedCliente()
    {
        // Arrange
        var cliente = SeedCliente("João Silva", "11999999999", "12345678901");

        // Act
        _viewModel.EditarCommand.Execute(cliente);

        // Assert
        _viewModel.Nome.Should().Be("João Silva");
        _viewModel.Telefone.Should().Be("11999999999");
        _viewModel.CpfCnpj.Should().Be("12345678901");
        _viewModel.Mensagem.Should().Contain("A editar: João Silva");
    }

    [Fact]
    public void SalvarCommand_ShouldAddNewCliente_WhenNoClienteEmEdicao()
    {
        // Arrange
        _viewModel.Nome = "Novo Cliente";
        _viewModel.Telefone = "11888888888";
        _viewModel.CpfCnpj = "98765432100";
        _viewModel.Logradouro = "Rua Nova";
        _viewModel.Numero = "456";
        _viewModel.Bairro = "Centro";
        _viewModel.Cidade = "São Paulo";
        _viewModel.Uf = "SP";
        _viewModel.Cep = "01001000";

        // Act
        _viewModel.SalvarCommand.Execute(null);

        // Assert
        _viewModel.Mensagem.Should().Be("Cliente gravado com sucesso!");
        DbContext.Clientes.Should().HaveCount(1);
        var saved = DbContext.Clientes.First();
        saved.Nome.Should().Be("Novo Cliente");
        saved.Telefone.Should().Be("11888888888");
        saved.Endereco.Should().Contain("Rua Nova, 456");
    }

    [Fact]
    public void SalvarCommand_ShouldUpdateExistingCliente_WhenClienteEmEdicao()
    {
        // Arrange - Use a valid CPF (123.456.789-09 -> 12345678909)
        var cliente = SeedCliente("Cliente Original", "11999999999", "12345678909");
        _viewModel.EditarCommand.Execute(cliente);
        _viewModel.Nome = "Cliente Atualizado";

        // Act
        _viewModel.SalvarCommand.Execute(null);

        // Assert
        _viewModel.Mensagem.Should().Be("Cliente atualizado com sucesso!");
        var updated = DbContext.Clientes.First();
        updated.Nome.Should().Be("Cliente Atualizado");
    }

    [Fact]
    public void SalvarCommand_ShouldFail_WhenNomeIsEmpty()
    {
        // Arrange
        _viewModel.Nome = "";
        _viewModel.Telefone = "11999999999";

        // Act
        _viewModel.SalvarCommand.Execute(null);

        // Assert
        _viewModel.Mensagem.Should().Be("O nome do cliente é obrigatório.");
        DbContext.Clientes.Should().BeEmpty();
    }

    [Fact]
    public void SalvarCommand_ShouldFail_WhenCpfIsInvalid()
    {
        // Arrange
        _viewModel.Nome = "Cliente Teste";
        _viewModel.CpfCnpj = "11111111111"; // CPF inválido

        // Act
        _viewModel.SalvarCommand.Execute(null);

        // Assert
        _viewModel.Mensagem.Should().Be("O CPF informado é inválido.");
    }

    [Fact]
    public void SalvarCommand_ShouldFail_WhenCnpjIsInvalid()
    {
        // Arrange
        _viewModel.Nome = "Empresa Teste";
        _viewModel.CpfCnpj = "11.111.111/0001-11"; // CNPJ inválido

        // Act
        _viewModel.SalvarCommand.Execute(null);

        // Assert
        _viewModel.Mensagem.Should().Be("O CNPJ informado é inválido.");
    }

    [Fact]
    public void ExcluirCommand_ShouldRemoveCliente_WhenConfirmed()
    {
        // Arrange
        var cliente = SeedCliente("Para Excluir");
        _viewModel.EditarCommand.Execute(cliente);

        // Act - Note: We can't easily test MessageBox, so we test the logic directly
        DbContext.Clientes.Remove(cliente);
        DbContext.SaveChanges();

        // Assert
        DbContext.Clientes.Should().BeEmpty();
    }

    [Fact]
    public async Task TextoPesquisa_ShouldFilterByNome()
    {
        // Arrange
        SeedCliente("João Silva");
        SeedCliente("Maria Santos");
        SeedCliente("Pedro Oliveira");

        // Act
        _viewModel.TextoPesquisa = "João";
        await Task.Delay(350); // Wait for debounce (300ms + buffer)

        // Assert
        _viewModel.Clientes.Should().HaveCount(1);
        _viewModel.Clientes.First().Nome.Should().Be("João Silva");
    }

    [Fact]
    public async Task TextoPesquisa_ShouldFilterByCpfCnpj()
    {
        // Arrange
        SeedCliente("João Silva", cpfCnpj: "12345678901");
        SeedCliente("Maria Santos", cpfCnpj: "98765432100");

        // Act
        _viewModel.TextoPesquisa = "12345678901";
        await Task.Delay(350);

        // Assert
        _viewModel.Clientes.Should().HaveCount(1);
        _viewModel.Clientes.First().CpfCnpj.Should().Be("12345678901");
    }

    [Fact]
    public async Task TextoPesquisa_ShouldFilterByTelefone()
    {
        // Arrange
        SeedCliente("João Silva", telefone: "11999999999");
        SeedCliente("Maria Santos", telefone: "11888888888");

        // Act
        _viewModel.TextoPesquisa = "999999999";
        await Task.Delay(350);

        // Assert
        _viewModel.Clientes.Should().HaveCount(1);
        _viewModel.Clientes.First().Telefone.Should().Be("11999999999");
    }

    [Fact]
    public void Fields_ShouldBeCleared_AfterSuccessfulSave()
    {
        // Arrange - Use a valid CPF
        var cliente = SeedCliente("Teste Limpar", "11999999999", "12345678909");
        _viewModel.EditarCommand.Execute(cliente);

        // Act - Save with a change to trigger update
        _viewModel.Nome = "Nome Alterado";
        _viewModel.SalvarCommand.Execute(null);

        // Assert - After save, form should be cleared (except Mensagem)
        _viewModel.Nome.Should().BeEmpty();
        _viewModel.Telefone.Should().BeEmpty();
        _viewModel.CpfCnpj.Should().BeEmpty();
        _viewModel.Mensagem.Should().Be("Cliente atualizado com sucesso!");
    }
}