using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Moq;
using SistemaOS.App.ViewModels;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;
using SistemaOS.Infrastructure.Data;
using SistemaOS.Infrastructure.Services;
using Xunit;

namespace SistemaOS.Tests.ViewModels;

public class ConfiguracoesViewModelTests : TestBase
{
    private readonly ConfiguracoesViewModel _viewModel;
    private readonly ITextFormatterService _textFormatter;

    public ConfiguracoesViewModelTests()
    {
        _textFormatter = GetService<ITextFormatterService>();
        _viewModel = GetService<ConfiguracoesViewModel>();
    }

    [Fact]
    public void Constructor_ShouldLoadEmpresa_WhenExists()
    {
        // Arrange
        SeedEmpresa();

        // Act - create new instance to trigger constructor
        var vm = GetService<ConfiguracoesViewModel>();

        // Assert
        vm.Cnpj.Should().Be("12.345.678/0001-90");
        vm.RazaoSocial.Should().Be("Empresa Teste LTDA");
        vm.NomeFantasia.Should().Be("Empresa Teste");
        vm.TelefoneEmpresa.Should().Be("1133334444");
        vm.CepEmpresa.Should().Be("01000000");
        vm.LogradouroEmpresa.Should().Be("Rua das Empresas");
        vm.NumeroEmpresa.Should().Be("100");
        vm.BairroEmpresa.Should().Be("Centro");
        vm.CidadeEmpresa.Should().Be("São Paulo");
        vm.UfEmpresa.Should().Be("SP");
    }

    [Fact]
    public void Constructor_ShouldUseDefaults_WhenNoEmpresa()
    {
        // Assert (no empresa seeded)
        // TextFormatterService default is 0 (TUDO MAIÚSCULO)
        _viewModel.FormatoSelecionadoIndex.Should().Be(_textFormatter.FormatoTextoIndex);
        _viewModel.TipoOsSelecionado.Should().Be(0);
        _viewModel.OrientacaoSelecionada.Should().Be(0);
        _viewModel.TamanhoPapelSelecionado.Should().Be(0);
    }

    [Fact]
    public void SalvarEmpresaCommand_ShouldCreateNewEmpresa_WhenNoneExists()
    {
        // Arrange
        _viewModel.Cnpj = "98.765.432/0001-10";
        _viewModel.RazaoSocial = "Nova Empresa LTDA";
        _viewModel.NomeFantasia = "Nova Empresa";
        _viewModel.TelefoneEmpresa = "1122223333";
        _viewModel.CepEmpresa = "02000000";
        _viewModel.LogradouroEmpresa = "Av. Nova";
        _viewModel.NumeroEmpresa = "200";
        _viewModel.BairroEmpresa = "Jardins";
        _viewModel.CidadeEmpresa = "São Paulo";
        _viewModel.UfEmpresa = "SP";

        // Act
        _viewModel.SalvarEmpresaCommand.Execute(null);

        // Assert
        _viewModel.Mensagem.Should().Be("Dados gravados com sucesso!");
        var empresa = DbContext.DadosEmpresa.First();
        empresa.Cnpj.Should().Be("98.765.432/0001-10");
        empresa.RazaoSocial.Should().Be("Nova Empresa LTDA");
    }

    [Fact]
    public void SalvarEmpresaCommand_ShouldUpdateExistingEmpresa()
    {
        // Arrange
        SeedEmpresa();
        _viewModel.RazaoSocial = "Empresa Atualizada LTDA";

        // Act - clear tracker to avoid entity tracking conflict
        ClearChangeTracker();
        _viewModel.SalvarEmpresaCommand.Execute(null);

        // Assert
        var empresa = DbContext.DadosEmpresa.First();
        empresa.RazaoSocial.Should().Be("Empresa Atualizada LTDA");
        _viewModel.Mensagem.Should().Be("Dados gravados com sucesso!");
    }

    [Fact]
    public void SalvarEmpresaCommand_ShouldSavePrintSettings()
    {
        // Arrange
        SeedEmpresa();
        _viewModel.TipoOsSelecionado = 1; // 2 Vias
        _viewModel.OrientacaoSelecionada = 1; // Paisagem
        _viewModel.TamanhoPapelSelecionado = 1; // Bobina 80mm

        // Act - clear tracker to avoid entity tracking conflict
        ClearChangeTracker();
        _viewModel.SalvarEmpresaCommand.Execute(null);

        // Assert
        var empresa = DbContext.DadosEmpresa.First();
        empresa.TipoOsIndex.Should().Be(1);
        // TamanhoPapelIndex agora é salvo corretamente (bug corrigido)
        empresa.TamanhoPapelIndex.Should().Be(1);
        // Orientação deve ser 0 para bobina
        empresa.OrientacaoIndex.Should().Be(0);
    }

    [Fact]
    public void PermiteOrientacao_ShouldBeTrue_WhenA4()
    {
        // Arrange
        _viewModel.TamanhoPapelSelecionado = 0; // A4

        // Assert
        _viewModel.PermiteOrientacao.Should().BeTrue();
    }

    [Fact]
    public void PermiteOrientacao_ShouldBeFalse_WhenBobina80mm()
    {
        // Arrange
        _viewModel.TamanhoPapelSelecionado = 1; // Bobina 80mm

        // Assert
        _viewModel.PermiteOrientacao.Should().BeFalse();
    }

    [Fact]
    public void PermiteOrientacao_ShouldBeFalse_WhenBobina58mm()
    {
        // Arrange
        _viewModel.TamanhoPapelSelecionado = 2; // Bobina 58mm

        // Assert
        _viewModel.PermiteOrientacao.Should().BeFalse();
    }

    [Fact]
    public void OnTamanhoPapelSelecionadoChanged_ShouldResetOrientacao_WhenNotA4()
    {
        // Arrange
        _viewModel.TamanhoPapelSelecionado = 0;
        _viewModel.OrientacaoSelecionada = 1; // Paisagem

        // Act
        _viewModel.TamanhoPapelSelecionado = 1; // Bobina

        // Assert
        _viewModel.OrientacaoSelecionada.Should().Be(0); // Resetado para Retrato
    }

    [Fact]
    public void OpcoesFormato_ShouldHaveThreeOptions()
    {
        // Assert
        _viewModel.OpcoesFormato.Should().HaveCount(3);
        _viewModel.OpcoesFormato.Should().Contain("TUDO MAIÚSCULO");
        _viewModel.OpcoesFormato.Should().Contain("tudo minúsculo");
        _viewModel.OpcoesFormato.Should().Contain("Primeiras Letras Maiúsculas");
    }

    [Fact]
    public void OpcoesTipoOs_ShouldHaveTwoOptions()
    {
        // Assert
        _viewModel.OpcoesTipoOs.Should().HaveCount(2);
        _viewModel.OpcoesTipoOs.Should().Contain("1 Via (Padrão)");
        _viewModel.OpcoesTipoOs.Should().Contain("2 Vias na mesma folha");
    }
}