using AuthService.Services;
using FluentValidation;
using MeteorCloud.Shared.ApiResults;

namespace AuthService.Features.Auth;

public record ResetPasswordRequest(Guid Token, string NewPassword);

public class ResetPasswordValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Token is required.");
        
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required.")
            .Must(ValidatePassword)
            .WithMessage("Old password must contain at least one uppercase letter, one digit.");
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


public class ResetPasswordHandler
{
    private readonly CredentialService _credentialService;

    public ResetPasswordHandler(CredentialService credentialService)
    {
        _credentialService = credentialService;
    }

    public async Task<ApiResult<bool>> Handle(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var existingCredentials = await _credentialService.GetCredentialsByResetPasswordTokenAsync(request.Token);
        if (existingCredentials == null)
        {
            return new ApiResult<bool>(false, false, "Invalid token.");
        }

        var result = await _credentialService.ChangePassword(existingCredentials.UserId, request.NewPassword, cancellationToken);
        
        if (!result)
        {
            return new ApiResult<bool>(false, false, "Failed to reset password. Please try again.");
        }

        return new ApiResult<bool>(true, true, "Password reset token generated successfully.");
    }
}


public class ResetPasswordEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/reset-password", async
            (
                ResetPasswordRequest request,
                ResetPasswordValidator validator,
                ResetPasswordHandler handler,
                CancellationToken cancellationToken) =>
            {
                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var result = await handler.Handle(request, cancellationToken);
                return result.Success
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .WithName("ResetPassword");
    }
}