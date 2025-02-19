using FluentValidation;
using MeteorCloud.API.DTOs.Auth;

namespace MeteorCloud.API.Validation.Auth;

public class LoginValidator : AbstractValidator<UserLoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Invalid email format");
        RuleFor(x => x.Password).NotEmpty();
    }
}