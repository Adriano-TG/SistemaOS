using FluentValidation;
using SistemaOS.Core.Entities;

namespace SistemaOS.App.Validators;

public class ClienteValidator : AbstractValidator<Cliente>
{
    public ClienteValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres")
            .MinimumLength(2).WithMessage("Nome deve ter pelo menos 2 caracteres");

        RuleFor(x => x.Telefone)
            .NotEmpty().WithMessage("Telefone é obrigatório")
            .Matches(@"^\(?\d{2}\)?\s?\d{4,5}-?\d{4}$").WithMessage("Telefone inválido (ex: (11) 99999-9999)");

        RuleFor(x => x.WhatsApp)
            .Matches(@"^\(?\d{2}\)?\s?\d{4,5}-?\d{4}$").WithMessage("WhatsApp inválido (ex: (11) 99999-9999)")
            .When(x => !string.IsNullOrWhiteSpace(x.WhatsApp));

        RuleFor(x => x.CpfCnpj)
            .NotEmpty().WithMessage("CPF/CNPJ é obrigatório")
            .Must(ValidarCpfCnpj).WithMessage("CPF/CNPJ inválido");

        RuleFor(x => x.Endereco)
            .MaximumLength(500).WithMessage("Endereço deve ter no máximo 500 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.Endereco));
    }

    private static bool ValidarCpfCnpj(string documento)
    {
        if (string.IsNullOrWhiteSpace(documento)) return false;

        var soNumeros = new string(documento.Where(char.IsDigit).ToArray());

        // CPF: 11 dígitos
        if (soNumeros.Length == 11)
            return ValidarCpf(soNumeros);

        // CNPJ: 14 dígitos
        if (soNumeros.Length == 14)
            return ValidarCnpj(soNumeros);

        return false;
    }

    private static bool ValidarCpf(string cpf)
    {
        if (cpf.Length != 11) return false;
        if (cpf.Distinct().Count() == 1) return false; // Todos dígitos iguais

        int[] multiplicador1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] multiplicador2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        string tempCpf = cpf.Substring(0, 9);
        int soma = 0;

        for (int i = 0; i < 9; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];

        int resto = soma % 11;
        int digito1 = resto < 2 ? 0 : 11 - resto;
        tempCpf += digito1;

        soma = 0;
        for (int i = 0; i < 10; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];

        resto = soma % 11;
        int digito2 = resto < 2 ? 0 : 11 - resto;
        tempCpf += digito2;

        return cpf.EndsWith(digito1.ToString() + digito2.ToString());
    }

    private static bool ValidarCnpj(string cnpj)
    {
        if (cnpj.Length != 14) return false;
        if (cnpj.Distinct().Count() == 1) return false;

        int[] multiplicador1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] multiplicador2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        string tempCnpj = cnpj.Substring(0, 12);
        int soma = 0;

        for (int i = 0; i < 12; i++)
            soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];

        int resto = soma % 11;
        int digito1 = resto < 2 ? 0 : 11 - resto;
        tempCnpj += digito1;

        soma = 0;
        for (int i = 0; i < 13; i++)
            soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];

        resto = soma % 11;
        int digito2 = resto < 2 ? 0 : 11 - resto;
        tempCnpj += digito2;

        return cnpj.EndsWith(digito1.ToString() + digito2.ToString());
    }
}