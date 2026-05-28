using FluentValidation;

namespace notX.Application.Features.Notifications.Commands.CreateNotificationsBatch;

public sealed class CreateNotificationsBatchCommandValidator
    : AbstractValidator<CreateNotificationsBatchCommand>
{
    public CreateNotificationsBatchCommandValidator()
    {
        RuleFor(x => x.Notifications)
            .NotEmpty().WithMessage("Pelo menos uma notificação é obrigatória.")
            .Must(n => n.Count <= 1000).WithMessage("O lote não pode exceder 1000 notificações.");

        RuleForEach(x => x.Notifications).ChildRules(item =>
        {
            item.RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Tipo de notificação inválido.");

            item.RuleFor(x => x.Title)
                .NotEmpty().WithMessage("O título é obrigatório.")
                .MaximumLength(500).WithMessage("O título não pode ter mais de 500 caracteres.");

            item.RuleFor(x => x.Content)
                .NotEmpty().WithMessage("O conteúdo é obrigatório.");

            item.RuleFor(x => x.Recipient)
                .NotEmpty().WithMessage("O destinatário é obrigatório.")
                .EmailAddress().WithMessage("O destinatário deve ser um endereço de e-mail válido.")
                .When(x => x.Type == notX.Domain.Enums.NotificationType.Email);
        });
    }
}
