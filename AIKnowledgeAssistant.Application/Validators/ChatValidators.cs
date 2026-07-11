namespace AIKnowledgeAssistant.Application.Validators;

using FluentValidation;
using AIKnowledgeAssistant.Application.DTOs.Chat;

/// <summary>
/// Validates a chat question before it hits the RAG pipeline.
/// </summary>
public class AskRequestValidator : AbstractValidator<AskRequest>
{
    public AskRequestValidator()
    {
        RuleFor(x => x.Question)
            .NotEmpty()
            .WithMessage("Question is required")

            .MinimumLength(3)
            .WithMessage("Question is too short")

            .MaximumLength(2000)
            .WithMessage("Question cannot exceed 2000 characters");
    }
}
