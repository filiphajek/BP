using FluentValidation;
using TaskLauncher.Api.Contracts.Requests;

namespace TaskLauncher.Api.Contracts.Validators;

public class TaskCreateRequestValidator : AbstractValidator<TaskCreateRequest>
{
    public TaskCreateRequestValidator()
    {
        RuleFor(i => i.Name.Length)
            .LessThan(128)
            .WithMessage("Name is too long");
    }
}