using System;
using FluentAssertions;
using Moq;
using SistemaOS.App.Services;
using SistemaOS.App.ViewModels;
using SistemaOS.App.Helpers;
using CommunityToolkit.Mvvm.Messaging;
using Xunit;

namespace SistemaOS.Tests.ViewModels;

public class MainViewModelTests : TestBase
{
    private readonly MainViewModel _viewModel;

    public MainViewModelTests()
    {
        // Clear messenger to avoid cross-test contamination
        WeakReferenceMessenger.Default.Reset();
        _viewModel = GetService<MainViewModel>();
    }

    [Fact]
    public void IrParaClientesCommand_ShouldSetTelaAtualToClientesViewModel()
    {
        // Act
        _viewModel.IrParaClientesCommand.Execute(null);

        // Assert
        _viewModel.TelaAtual.Should().BeOfType<ClientesViewModel>();
    }

    [Fact]
    public void IrParaOrdensCommand_ShouldSetTelaAtualToOrdensViewModel()
    {
        // Act
        _viewModel.IrParaOrdensCommand.Execute(null);

        // Assert
        _viewModel.TelaAtual.Should().BeOfType<OrdensViewModel>();
    }

    [Fact]
    public void IrParaPdvCommand_ShouldSetTelaAtualToPdvViewModel()
    {
        // Act
        _viewModel.IrParaPdvCommand.Execute(null);

        // Assert
        _viewModel.TelaAtual.Should().BeOfType<PdvViewModel>();
    }

    [Fact]
    public void IrParaFinanceiroCommand_ShouldSetTelaAtualToEmConstrucaoViewModel()
    {
        // Act
        _viewModel.IrParaFinanceiroCommand.Execute(null);

        // Assert
        _viewModel.TelaAtual.Should().BeOfType<EmConstrucaoViewModel>();
        var emConstrucao = _viewModel.TelaAtual as EmConstrucaoViewModel;
        emConstrucao!.Titulo.Should().Be("Financeiro");
    }

    [Fact]
    public void IrParaConfiguracoesCommand_ShouldSetTelaAtualToConfiguracoesViewModel()
    {
        // Act
        _viewModel.IrParaConfiguracoesCommand.Execute(null);

        // Assert
        _viewModel.TelaAtual.Should().BeOfType<ConfiguracoesViewModel>();
    }

    [Fact]
    public void IrParaInicioCommand_ShouldSetTelaAtualToNull()
    {
        // Arrange
        _viewModel.IrParaClientesCommand.Execute(null);
        _viewModel.TelaAtual.Should().NotBeNull();

        // Act
        _viewModel.IrParaInicioCommand.Execute(null);

        // Assert
        _viewModel.TelaAtual.Should().BeNull();
    }

    [Fact]
    public void AlternarTemaCommand_ShouldCallThemeService()
    {
        // Act
        _viewModel.AlternarTemaCommand.Execute(null);

        // Assert
        ThemeServiceMock.Verify(x => x.AlternarTema(), Times.Once);
    }

    [Fact]
    public void MudarCorCommand_ShouldCallThemeService_WhenHexProvided()
    {
        // Arrange
        const string hex = "#FF0000";

        // Act
        _viewModel.MudarCorCommand.Execute(hex);

        // Assert
        ThemeServiceMock.Verify(x => x.DefinirCor(hex), Times.Once);
    }

    [Fact]
    public void MudarCorCommand_ShouldNotCallThemeService_WhenHexNull()
    {
        // Act
        _viewModel.MudarCorCommand.Execute(null);

        // Assert
        ThemeServiceMock.Verify(x => x.DefinirCor(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void MudarTelaMessage_ShouldNavigateToOrdens_WhenValueIsOrdens()
    {
        // Act
        WeakReferenceMessenger.Default.Send(new MudarTelaMessage("Ordens"));

        // Assert
        _viewModel.TelaAtual.Should().BeOfType<OrdensViewModel>();
    }

    [Fact]
    public void MudarTelaMessage_ShouldNavigateToClientes_WhenValueIsClientes()
    {
        // Act
        WeakReferenceMessenger.Default.Send(new MudarTelaMessage("Clientes"));

        // Assert
        _viewModel.TelaAtual.Should().BeOfType<ClientesViewModel>();
    }
}