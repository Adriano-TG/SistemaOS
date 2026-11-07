using System;
using FluentAssertions;
using SistemaOS.Core.Entities;
using Xunit;

namespace SistemaOS.Tests.Core;

public class EntitiesTests
{
    [Fact]
    public void Cliente_ShouldHaveDefaultValues()
    {
        // Act
        var cliente = new Cliente();

        // Assert
        cliente.Nome.Should().BeNullOrEmpty();
        cliente.Telefone.Should().BeNullOrEmpty();
        cliente.CpfCnpj.Should().BeNullOrEmpty();
        cliente.Endereco.Should().BeNullOrEmpty();
        cliente.WhatsApp.Should().BeNullOrEmpty();
        cliente.DataCadastro.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void OrdemServico_ShouldHaveDefaultValues()
    {
        // Act
        var os = new OrdemServico();

        // Assert
        os.DataAbertura.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        os.Equipamento.Should().BeNullOrEmpty();
        os.Marca.Should().BeNullOrEmpty();
        os.NumeroSerie.Should().BeNullOrEmpty();
        os.DefeitoRelatado.Should().BeNullOrEmpty();
        os.LaudoTecnico.Should().BeNullOrEmpty();
        os.Status.Should().Be(StatusOS.Aberta);
        os.EmGarantia.Should().BeFalse();
        os.Itens.Should().NotBeNull();
        os.DataConclusao.Should().BeNull();
        os.ValorTotal.Should().Be(0);
    }

    [Fact]
    public void ItemOS_ShouldCalculateTotalCorrectly()
    {
        // Arrange
        var item = new ItemOS
        {
            Descricao = "Peça",
            Quantidade = 3,
            ValorUnitario = 50.00m
        };

        // Assert
        item.Total.Should().Be(150.00m);
    }

    [Fact]
    public void ItemOS_ShouldHandleDecimalQuantities()
    {
        // Arrange
        var item = new ItemOS
        {
            Descricao = "Metro de cabo",
            Quantidade = 2.5m,
            ValorUnitario = 10.00m
        };

        // Assert
        item.Total.Should().Be(25.00m);
    }

    [Fact]
    public void Empresa_ShouldHaveDefaultValues()
    {
        // Act
        var empresa = new Empresa();

        // Assert
        empresa.Cnpj.Should().BeNullOrEmpty();
        empresa.RazaoSocial.Should().BeNullOrEmpty();
        empresa.NomeFantasia.Should().BeNullOrEmpty();
        empresa.Telefone.Should().BeNullOrEmpty();
        empresa.Cep.Should().BeNullOrEmpty();
        empresa.Logradouro.Should().BeNullOrEmpty();
        empresa.Numero.Should().BeNullOrEmpty();
        empresa.Bairro.Should().BeNullOrEmpty();
        empresa.Cidade.Should().BeNullOrEmpty();
        empresa.Uf.Should().BeNullOrEmpty();
        empresa.CaminhoLogo.Should().BeNullOrEmpty();
        empresa.TipoOsIndex.Should().Be(0);
        empresa.OrientacaoIndex.Should().Be(0);
        empresa.TamanhoPapelIndex.Should().Be(0);
    }

    [Fact]
    public void StatusOS_ShouldHaveExpectedValues()
    {
        // Assert
        Enum.GetValues<StatusOS>().Should().BeEquivalentTo(new[]
        {
            StatusOS.Aberta,
            StatusOS.EmAnalise,
            StatusOS.AguardandoPeca,
            StatusOS.Pronta,
            StatusOS.Entregue,
            StatusOS.Cancelada
        });
    }
}