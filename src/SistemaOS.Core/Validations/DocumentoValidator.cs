using System.Linq;
using System.Text.RegularExpressions;

namespace SistemaOS.Core.Validations;

public static class DocumentoValidator
{
    public static bool IsCpfValido(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return false;

        // Remove tudo que não for número
        cpf = Regex.Replace(cpf, "[^0-9]", "");

        if (cpf.Length != 11) return false;

        // Rejeita CPFs com todos os números iguais (ex: 111.111.111-11)
        if (new string(cpf[0], 11) == cpf) return false;

        var multiplicador1 = new int[9] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        var multiplicador2 = new int[10] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        var tempCpf = cpf[..9];
        var soma = 0;

        for (var i = 0; i < 9; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];

        var resto = soma % 11;
        resto = resto < 2 ? 0 : 11 - resto;

        var digito = resto.ToString();
        tempCpf += digito;
        soma = 0;

        for (var i = 0; i < 10; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];

        resto = soma % 11;
        resto = resto < 2 ? 0 : 11 - resto;

        digito += resto.ToString();

        return cpf.EndsWith(digito);
    }

    public static bool IsCnpjValido(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj)) return false;

        // Remove tudo que não for número
        cnpj = Regex.Replace(cnpj, "[^0-9]", "");

        if (cnpj.Length != 14) return false;

        if (new string(cnpj[0], 14) == cnpj) return false;

        var multiplicador1 = new int[12] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var multiplicador2 = new int[13] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var tempCnpj = cnpj[..12];
        var soma = 0;

        for (var i = 0; i < 12; i++)
            soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];

        var resto = (soma % 11);
        resto = resto < 2 ? 0 : 11 - resto;

        var digito = resto.ToString();
        tempCnpj += digito;
        soma = 0;

        for (var i = 0; i < 13; i++)
            soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];

        resto = (soma % 11);
        resto = resto < 2 ? 0 : 11 - resto;

        digito += resto.ToString();

        return cnpj.EndsWith(digito);
    }
}
