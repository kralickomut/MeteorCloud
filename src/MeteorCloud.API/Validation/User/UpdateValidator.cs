using FluentValidation;
using MeteorCloud.API.DTOs.User;

namespace MeteorCloud.API.Validation.User;

public class UpdateValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Invalid email address");
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
    }
}