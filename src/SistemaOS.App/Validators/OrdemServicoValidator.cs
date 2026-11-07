using FluentValidation;
using SistemaOS.Core.Entities;

namespace SistemaOS.App.Validators;

public class OrdemServicoValidator : AbstractValidator<OrdemServico>
{
    public OrdemServicoValidator()
    {
        RuleFor(x => x.ClienteId)
            .GreaterThan(0).WithMessage("Cliente deve ser selecionado");

        RuleFor(x => x.Equipamento)
            .NotEmpty().WithMessage("Equipamento é obrigatório")
            .MaximumLength(200).WithMessage("Equipamento deve ter no máximo 200 caracteres")
            .MinimumLength(2).WithMessage("Equipamento deve ter pelo menos 2 caracteres");

        RuleFor(x => x.Marca)
            .MaximumLength(100).WithMessage("Marca deve ter no máximo 100 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.Marca));

        RuleFor(x => x.NumeroSerie)
            .MaximumLength(100).WithMessage("Número de série deve ter no máximo 100 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.NumeroSerie));

        RuleFor(x => x.DefeitoRelatado)
            .MaximumLength(1000).WithMessage("Defeito relatado deve ter no máximo 1000 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.DefeitoRelatado));

        RuleFor(x => x.LaudoTecnico)
            .MaximumLength(2000).WithMessage("Laudo técnico deve ter no máximo 2000 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.LaudoTecnico));

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status inválido");

        // Validação dos itens
        RuleForEach(x => x.Itens).SetValidator(new ItemOSValidator());

        RuleFor(x => x.Itens)
            .NotEmpty().WithMessage("Pelo menos um item é obrigatório")
            .When(x => x.Status != StatusOS.Cancelada);
    }
}

public class ItemOSValidator : AbstractValidator<ItemOS>
{
    public ItemOSValidator()
    {
        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descrição do item é obrigatória")
            .MaximumLength(200).WithMessage("Descrição deve ter no máximo 200 caracteres");

        RuleFor(x => x.Quantidade)
            .GreaterThan(0).WithMessage("Quantidade deve ser maior que zero");

        RuleFor(x => x.ValorUnitario)
            .GreaterThanOrEqualTo(0).WithMessage("Valor unitário não pode ser negativo");

        RuleFor(x => x.Total)
            .GreaterThanOrEqualTo(0).WithMessage("Total não pode ser negativo");
    }
}