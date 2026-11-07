using FluentAssertions;
using SistemaOS.App.Helpers;
using Xunit;

namespace SistemaOS.Tests.Helpers;

public class FormatadorMascaraTests
{
    [Theory]
    [InlineData("12345678909", "123.456.789-09")]       // CPF completo (11 dígitos)
    [InlineData("1234567890", "123.456.789-0")]          // CPF 10 dígitos
    [InlineData("123456789", "123.456.789")]             // CPF 9 dígitos
    [InlineData("12345678", "123.456.78")]               // CPF 8 dígitos
    [InlineData("1234567", "123.456.7")]                 // CPF 7 dígitos
    [InlineData("123456", "123.456")]                    // CPF 6 dígitos
    [InlineData("12345", "123.45")]                      // CPF 5 dígitos
    [InlineData("1234", "123.4")]                        // CPF 4 dígitos
    [InlineData("123", "123")]                           // CPF 3 dígitos
    [InlineData("12", "12")]                             // CPF 2 dígitos
    [InlineData("1", "1")]                               // CPF 1 dígito
    public void FormatarCpfCnpj_ShouldFormatCpfCorrectly(string input, string expected)
    {
        // Act
        var result = FormatadorMascara_FormatarCpfCnpj(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("11222333000181", "11.222.333/0001-81")]  // CNPJ completo (14 dígitos)
    [InlineData("1122233300018", "11.222.333/0001-8")]    // CNPJ 13 dígitos
    [InlineData("112223330001", "11.222.333/0001")]       // CNPJ 12 dígitos
    [InlineData("1122233300011", "11.222.333/0001-1")]    // CNPJ 13 dígitos (14 com último)
    [InlineData("112223330001811", "11.222.333/0001-81")] // 15 dígitos -> truncado para 14
    public void FormatarCpfCnpj_ShouldFormatCnpjCorrectly(string input, string expected)
    {
        // Act
        var result = FormatadorMascara_FormatarCpfCnpj(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("11999999999", "(11) 99999-9999")]   // Celular 11 dígitos
    [InlineData("1199999999", "(11) 9999-9999")]     // Celular 10 dígitos (sem 9)
    [InlineData("1133334444", "(11) 3333-4444")]     // Fixo 10 dígitos
    [InlineData("113333444", "(11) 3333-444")]       // Fixo 9 dígitos
    [InlineData("11333344", "(11) 3333-44")]         // Fixo 8 dígitos
    [InlineData("1133334", "(11) 3333-4")]           // Fixo 7 dígitos
    [InlineData("113333", "(11) 3333")]              // 6 dígitos
    [InlineData("11333", "(11) 333")]                // 5 dígitos
    [InlineData("1133", "(11) 33")]                  // 4 dígitos
    [InlineData("113", "(11) 3")]                    // 3 dígitos
    [InlineData("11", "11")]                         // 2 dígitos -> returns as is (not >2)
    [InlineData("1", "1")]                           // 1 dígito
    public void FormatarTelefone_ShouldFormatCorrectly(string input, string expected)
    {
        // Act
        var result = FormatadorMascara_FormatarTelefone(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatarCpfCnpj_ShouldIgnoreNonDigits()
    {
        // Act
        var result = FormatadorMascara_FormatarCpfCnpj("123.456.789-09");

        // Assert
        result.Should().Be("123.456.789-09");
    }

    [Fact]
    public void FormatarTelefone_ShouldIgnoreNonDigits()
    {
        // Act
        var result = FormatadorMascara_FormatarTelefone("(11) 99999-9999");

        // Assert
        result.Should().Be("(11) 99999-9999");
    }

    [Fact]
    public void FormatarCpfCnpj_ShouldTruncateCnpjTo14Digits()
    {
        // Act
        var result = FormatadorMascara_FormatarCpfCnpj("1122233300018199"); // 16 dígitos

        // Assert
        result.Should().Be("11.222.333/0001-81"); // Apenas 14 primeiros
    }

    [Fact]
    public void FormatarTelefone_ShouldTruncateTo11Digits()
    {
        // Act
        var result = FormatadorMascara_FormatarTelefone("1199999999999"); // 13 dígitos

        // Assert
        result.Should().Be("(11) 99999-9999"); // Apenas 11 primeiros
    }

    // Helper methods to test private static methods
    private static string FormatadorMascara_FormatarCpfCnpj(string t)
    {
        return typeof(FormatadorMascara)
            .GetMethod("FormatarCpfCnpj", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, new object[] { t }) as string ?? string.Empty;
    }

    private static string FormatadorMascara_FormatarTelefone(string t)
    {
        return typeof(FormatadorMascara)
            .GetMethod("FormatarTelefone", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, new object[] { t }) as string ?? string.Empty;
    }
}