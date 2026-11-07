using FluentValidation;
using SistemaOS.Core.Entities;

namespace SistemaOS.App.Validators;

public class UsuarioValidator : AbstractValidator<Usuario>
{
    public UsuarioValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres");

        RuleFor(x => x.Login)
            .NotEmpty().WithMessage("Login é obrigatório")
            .MaximumLength(50).WithMessage("Login deve ter no máximo 50 caracteres")
            .MinimumLength(3).WithMessage("Login deve ter pelo menos 3 caracteres")
            .Matches(@"^[a-zA-Z0-9._-]+$").WithMessage("Login pode conter apenas letras, números, ponto, underscore e hífen");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Role inválida");

        RuleFor(x => x.Ativo)
            .NotNull().WithMessage("Status ativo/inativo é obrigatório");
    }
}

public class TrocaSenhaValidator : AbstractValidator<TrocaSenhaModel>
{
    public TrocaSenhaValidator()
    {
        RuleFor(x => x.SenhaAtual)
            .NotEmpty().WithMessage("Senha atual é obrigatória");

        RuleFor(x => x.NovaSenha)
            .NotEmpty().WithMessage("Nova senha é obrigatória")
            .MinimumLength(8).WithMessage("Nova senha deve ter pelo menos 8 caracteres")
            .Matches(@"[A-Z]").WithMessage("Nova senha deve conter pelo menos uma letra maiúscula")
            .Matches(@"[a-z]").WithMessage("Nova senha deve conter pelo menos uma letra minúscula")
            .Matches(@"\d").WithMessage("Nova senha deve conter pelo menos um número")
            .Matches(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]").WithMessage("Nova senha deve conter pelo menos um caractere especial");

        RuleFor(x => x.ConfirmarSenha)
            .NotEmpty().WithMessage("Confirmação de senha é obrigatória")
            .Equal(x => x.NovaSenha).WithMessage("As senhas não coincidem");
    }
}

public class TrocaSenhaModel
{
    public string SenhaAtual { get; set; } = string.Empty;
    public string NovaSenha { get; set; } = string.Empty;
    public string ConfirmarSenha { get; set; } = string.Empty;
}