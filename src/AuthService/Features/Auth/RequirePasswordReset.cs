using AuthService.Services;
using FluentValidation;
using MassTransit;
using MeteorCloud.Messaging.Events.Auth;
using MeteorCloud.Shared.ApiResults;

namespace AuthService.Features.Auth;

public record RequirePasswordResetRequest(string Email);

public class RequirePasswordResetValidator : AbstractValidator<RequirePasswordResetRequest>
{
    public RequirePasswordResetValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Invalid email format.");
    }
}

public class RequirePasswordResetHandler
{
    private readonly CredentialService _credentialService;
    private readonly IPublishEndpoint _publishEndpoint;

    public RequirePasswordResetHandler(CredentialService credentialService, IPublishEndpoint publishEndpoint)
    {
        _credentialService = credentialService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<ApiResult<bool>> Handle(RequirePasswordResetRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var existingCredentials = await _credentialService.GetCredentialsByEmailAsync(request.Email);
        if (existingCredentials == null)
        {
            return new ApiResult<bool>(false,false, "User not found.");
        }

        var token = await _credentialService.SetResetPasswordToken(existingCredentials.UserId);
        Console.WriteLine($"Token REQUIRED PASSWORD RESET: {token}");
        
        if (token == Guid.Empty)
        {
            return new ApiResult<bool>(false, false, "Failed to proceed. Please try again.");
        }
        
        await _publishEndpoint.Publish(new PasswordResetRequiredEvent()
        {
            Email = request.Email,
            Token = token
        });
        
        return new ApiResult<bool>(true, true, "Password reset token generated successfully.");
    }
}


public class PasswordResetRequiredEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/require-password-reset", async
        (
            RequirePasswordResetRequest request,
            RequirePasswordResetHandler handler,
            RequirePasswordResetValidator validator,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.Handle(request, cancellationToken);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        });
    }
}