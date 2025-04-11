using System.Text.Json;
using AuthService.Persistence;
using AuthService.Services;
using FluentValidation;
using MeteorCloud.Shared.ApiResults;

namespace AuthService.Features.Auth;

public record RegisterRequest(string Email, string Name, string Password);

public class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6)
            .Must(ValidatePassword)
            .WithMessage("Password must contain at least one uppercase letter, one digit.");
    }


    private bool ValidatePassword(string password)
    {
        if (password.All(char.IsLetter))
        {
            return false;
        }

        if (password.All(char.IsDigit))
        {
            return false;
        }

        if (!password.Any(char.IsUpper))
        {
            return false;
        }

        return true;
    }
}

public class RegisterHandler
{
    private readonly CredentialService _credentialService;

    public RegisterHandler(CredentialService credentialService)
    {
        _credentialService = credentialService;
    }

    public async Task<ApiResult<bool>> Handle(RegisterRequest request, CancellationToken cancellationToken)
    {
        var existingCredentials = await _credentialService.GetCredentialsByEmailAsync(request.Email);

        if (existingCredentials != null)
        {
            return new ApiResult<bool>(false, false, "Email already in use.");
        }

        await _credentialService.RegisterUserAsync(request.Email, request.Name, request.Password, cancellationToken);

        return new ApiResult<bool>(true, true, "User registered successfully.");
    }
}


public static class RegisterEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register",
            async (RegisterHandler handler, RegisterValidator validator, RegisterRequest request, CancellationToken cancellationToken) =>
            {
                Console.WriteLine($"Trying to register: {JsonSerializer.Serialize(request)}");

                
                var validationResult = await validator.ValidateAsync(request, cancellationToken);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage);
                    return Results.BadRequest(validationResult.Errors);
                }

                var response = await handler.Handle(request, cancellationToken);

                return response.Success
                    ? Results.Ok(response)
                    : Results.BadRequest(response);
            });
    }
}