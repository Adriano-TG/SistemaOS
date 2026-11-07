using FluentValidation;
using SistemaOS.Core.Entities;

namespace SistemaOS.App.Validators;

public class ProdutoValidator : AbstractValidator<Produto>
{
    public ProdutoValidator()
    {
        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("Código é obrigatório")
            .MaximumLength(100).WithMessage("Código deve ter no máximo 100 caracteres");

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres")
            .MinimumLength(2).WithMessage("Nome deve ter pelo menos 2 caracteres");

        RuleFor(x => x.Descricao)
            .MaximumLength(500).WithMessage("Descrição deve ter no máximo 500 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.Descricao));

        RuleFor(x => x.PrecoCusto)
            .GreaterThanOrEqualTo(0).WithMessage("Preço de custo não pode ser negativo");

        RuleFor(x => x.PrecoVenda)
            .GreaterThan(0).WithMessage("Preço de venda deve ser maior que zero")
            .GreaterThanOrEqualTo(x => x.PrecoCusto).WithMessage("Preço de venda deve ser maior ou igual ao preço de custo");

        RuleFor(x => x.QuantidadeAtual)
            .GreaterThanOrEqualTo(0).WithMessage("Quantidade atual não pode ser negativa");

        RuleFor(x => x.QuantidadeMinima)
            .GreaterThanOrEqualTo(0).WithMessage("Quantidade mínima não pode ser negativa");

        RuleFor(x => x.UnidadeMedida)
            .NotEmpty().WithMessage("Unidade de medida é obrigatória")
            .MaximumLength(50).WithMessage("Unidade de medida deve ter no máximo 50 caracteres");
    }
}