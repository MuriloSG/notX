using FluentValidation;

namespace notX.Application.Features.Notifications.Commands.CreateNotification;

public sealed class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
{
    public CreateNotificationCommandValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Tipo de notificação inválido.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(500).WithMessage("O título não pode ter mais de 500 caracteres.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("O conteúdo é obrigatório.");

        RuleFor(x => x.Recipient)
            .NotEmpty().WithMessage("O destinatário é obrigatório.")
            .EmailAddress().WithMessage("O destinatário deve ser um endereço de e-mail válido.")
            .When(x => x.Type == notX.Domain.Enums.NotificationType.Email);
    }
}
