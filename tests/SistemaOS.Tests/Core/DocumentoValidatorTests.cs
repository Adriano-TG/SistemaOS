using FluentAssertions;
using SistemaOS.Core.Validations;
using Xunit;

namespace SistemaOS.Tests.Core;

public class DocumentoValidatorTests
{
    #region CPF Tests

    [Theory]
    [InlineData("12345678909", true)]   // CPF válido
    [InlineData("123.456.789-09", true)] // CPF formatado
    [InlineData("00000000000", false)]   // CPF inválido (todos iguais)
    [InlineData("11111111111", false)]   // CPF inválido (todos iguais)
    [InlineData("12345678900", false)]   // CPF inválido (dígito errado)
    [InlineData("123456789", false)]     // CPF curto
    [InlineData("", false)]              // Vazio
    [InlineData("   ", false)]           // Apenas espaços
    [InlineData("\t\n", false)]          // Apenas whitespace
    [InlineData("123.456.789-0A", false)] // Com letra
    [InlineData("123456789012", false)]  // CPF longo (12 dígitos)
    [InlineData("1234567890123", false)] // CPF muito longo (13 dígitos)
    public void IsCpfValido_ShouldReturnExpectedResult(string cpf, bool expected)
    {
        // Act
        var result = DocumentoValidator.IsCpfValido(cpf);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    // CPFs válidos conhecidos (dígitos calculados corretamente)
    [InlineData("11144477735")]  // 111.444.777-35
    [InlineData("22255588846")]  // 222.555.888-46
    [InlineData("33366699957")]  // 333.666.999-57
    [InlineData("44477700083")]  // 444.777.000-83
    [InlineData("55588811194")]  // 555.888.111-94
    [InlineData("66699922203")]  // 666.999.222-03
    [InlineData("77700033340")]  // 777.000.333-40
    [InlineData("88811144450")]  // 888.111.444-50
    [InlineData("99922255561")]  // 999.222.555-61
    public void IsCpfValido_ShouldAcceptKnownValidCpfs(string cpf)
    {
        // Act
        var result = DocumentoValidator.IsCpfValido(cpf);

        // Assert
        result.Should().BeTrue($"CPF {cpf} deve ser válido");
    }

    [Theory]
    // CPFs com dígito verificador resultando em 10 (que vira 0)
    [InlineData("00000000191")] // Resto = 10 → dígito = 0
    [InlineData("11111111200")] // Resto = 10 → dígito = 0
    public void IsCpfValido_ShouldHandleRestoDez(string cpf)
    {
        // Act
        var result = DocumentoValidator.IsCpfValido(cpf);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCpfValido_ShouldIgnoreFormatting()
    {
        // Arrange
        var cpfSemFormatacao = "12345678909";
        var cpfComFormatacao = "123.456.789-09";

        // Act
        var result1 = DocumentoValidator.IsCpfValido(cpfSemFormatacao);
        var result2 = DocumentoValidator.IsCpfValido(cpfComFormatacao);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result1.Should().Be(result2);
    }

    [Fact]
    public void IsCpfValido_ShouldReturnFalse_WhenNull()
    {
        // Act
        string? input = null;
        var result = DocumentoValidator.IsCpfValido(input);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCpfValido_ShouldReturnFalse_WhenOnlyWhitespace()
    {
        // Act
        var result = DocumentoValidator.IsCpfValido("   \t\n  ");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCpfValido_ShouldRejectSequentialNumbers()
    {
        // Arrange
        var cpfSequencial = "12345678901"; // Não é todos iguais, mas sequencial

        // Act
        var result = DocumentoValidator.IsCpfValido(cpfSequencial);

        // Assert
        // Nota: CPF sequencial pode ser válido se os dígitos baterem
        // Este teste garante que não há lógica especial para sequenciais
        result.Should().BeFalse();
    }

    #endregion

    #region CNPJ Tests

    [Theory]
    [InlineData("11222333000181", true)]       // CNPJ válido
    [InlineData("11.222.333/0001-81", true)]   // CNPJ formatado
    [InlineData("00000000000000", false)]      // CNPJ inválido (todos iguais)
    [InlineData("11111111111111", false)]      // CNPJ inválido (todos iguais)
    [InlineData("11222333000180", false)]      // CNPJ inválido (dígito errado)
    [InlineData("1122233300018", false)]       // CNPJ curto
    [InlineData("", false)]                    // Vazio
    [InlineData("   ", false)]                 // Apenas espaços
    [InlineData("\t\n", false)]                // Apenas whitespace
    [InlineData("11.222.333/0001-8A", false)]  // Com letra
    [InlineData("112223330001819", false)]     // CNPJ longo (15 dígitos)
    [InlineData("1122233300018190", false)]    // CNPJ muito longo (16 dígitos)
    public void IsCnpjValido_ShouldReturnExpectedResult(string cnpj, bool expected)
    {
        // Act
        var result = DocumentoValidator.IsCnpjValido(cnpj);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    // CNPJs válidos conhecidos (dígitos calculados corretamente)
    [InlineData("11222333000181")]
    [InlineData("11222333000262")]
    [InlineData("11222333000343")]
    [InlineData("11222333000424")]
    [InlineData("11222333000505")]
    [InlineData("11222333000696")]
    [InlineData("11222333000777")]
    [InlineData("11222333000858")]
    [InlineData("11222333000939")]
    public void IsCnpjValido_ShouldAcceptKnownValidCnpjs(string cnpj)
    {
        // Act
        var result = DocumentoValidator.IsCnpjValido(cnpj);

        // Assert
        result.Should().BeTrue($"CNPJ {cnpj} deve ser válido");
    }

    [Theory]
    // CNPJs com dígito verificador resultando em 10 (que vira 0)
    [InlineData("00000000000191")] // Resto = 10 → dígito = 0
    [InlineData("11111111111260")] // Resto = 10 → dígito = 0
    public void IsCnpjValido_ShouldHandleRestoDez(string cnpj)
    {
        // Act
        var result = DocumentoValidator.IsCnpjValido(cnpj);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCnpjValido_ShouldIgnoreFormatting()
    {
        // Arrange
        var cnpjSemFormatacao = "11222333000181";
        var cnpjComFormatacao = "11.222.333/0001-81";

        // Act
        var result1 = DocumentoValidator.IsCnpjValido(cnpjSemFormatacao);
        var result2 = DocumentoValidator.IsCnpjValido(cnpjComFormatacao);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result1.Should().Be(result2);
    }

    [Fact]
    public void IsCnpjValido_ShouldReturnFalse_WhenNull()
    {
        // Act
        string? input = null;
        var result = DocumentoValidator.IsCnpjValido(input);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCnpjValido_ShouldReturnFalse_WhenOnlyWhitespace()
    {
        // Act
        var result = DocumentoValidator.IsCnpjValido("   \t\n  ");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCnpjValido_ShouldRejectSequentialNumbers()
    {
        // Arrange
        var cnpjSequencial = "12345678000190";

        // Act
        var result = DocumentoValidator.IsCnpjValido(cnpjSequencial);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Edge Cases & Mutation Killing Tests

    [Theory]
    [InlineData("123.456.789-09")] // Pontos e hífen
    [InlineData("123 456 789 09")] // Espaços
    [InlineData("123-456-789-09")] // Hífens
    [InlineData("123/456/789-09")] // Barras
    [InlineData("(123) 456-789-09")] // Parênteses
    public void IsCpfValido_ShouldAcceptVariousFormatting(string cpf)
    {
        var result = DocumentoValidator.IsCpfValido(cpf);
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("11 222 333 0001 81")]
    [InlineData("11-222-333-0001-81")]
    [InlineData("11/222/333/0001/81")]
    public void IsCnpjValido_ShouldAcceptVariousFormatting(string cnpj)
    {
        var result = DocumentoValidator.IsCnpjValido(cnpj);
        result.Should().BeTrue();
    }

    #endregion
}