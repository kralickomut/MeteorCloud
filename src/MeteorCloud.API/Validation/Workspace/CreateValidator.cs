using FluentValidation;
using MeteorCloud.API.DTOs.Workspace;

namespace MeteorCloud.API.Validation.Workspace;

public class CreateValidator : AbstractValidator<WorkspaceCreateRequest>
{
    public CreateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.OwnerId).GreaterThan(0);
    }
}