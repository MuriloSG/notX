using FluentValidation;

namespace notX.Application.Features.Notifications.Commands.CreateNotificationsBatch;

public sealed class CreateNotificationsBatchCommandValidator
    : AbstractValidator<CreateNotificationsBatchCommand>
{
    public CreateNotificationsBatchCommandValidator()
    {
        RuleFor(x => x.Notifications)
            .NotEmpty().WithMessage("At least one notification is required.")
            .Must(n => n.Count <= 1000).WithMessage("Batch cannot exceed 1000 notifications.");

        RuleForEach(x => x.Notifications).ChildRules(item =>
        {
            item.RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Invalid notification type.");

            item.RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(500).WithMessage("Title must not exceed 500 characters.");

            item.RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Content is required.");
        });
    }
}
