using FluentValidation;
using MeteorCloud.API.DTOs.Auth;

namespace MeteorCloud.API.Validation.Auth;

public class RegistrationValidator : AbstractValidator<UserRegistrationRequest>
{
    public RegistrationValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required.");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(5)
            .WithMessage("Password must be at least 5 characters long.");
    }
}