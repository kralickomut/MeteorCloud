using AuthService.Services;
using FluentValidation;
using MeteorCloud.Shared.ApiResults;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AuthService.Features.Auth;

public record ChangePasswordRequest(string OldPassword, string NewPassword);

public class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty()
            .WithMessage("Old password is required.")
            .Must(ValidatePassword)
            .WithMessage("Old password must contain at least one uppercase letter, one digit.");

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

public class ChangePasswordHandler
{
    private readonly CredentialService _credentialService;
    
    public ChangePasswordHandler(CredentialService credentialService)
    {
        _credentialService = credentialService;
    }
    
    public async Task<ApiResult<bool>> Handle(ChangePasswordRequest request, int userId, CancellationToken cancellationToken)
    {
        var existingCredentials = await _credentialService.GetByUserId(userId);
        if (existingCredentials == null)
        {
            return new ApiResult<bool>(false, false, "User not found.");
        }
        
        if (!_credentialService.VerifyPassword(request.OldPassword, existingCredentials.PasswordHash))
        {
            return new ApiResult<bool>(false, false, "Old password is incorrect.");
        }
        
        if (request.OldPassword == request.NewPassword)
        {
            return new ApiResult<bool>(false, false, "New password cannot be the same as the old password.");
        }
        
        var success = await _credentialService.ChangePassword(userId, request.NewPassword, cancellationToken);
        
        return success
            ? new ApiResult<bool>(true, true, "Password changed successfully.")
            : new ApiResult<bool>(false, false, "Failed to change password.");
    }
}

public class ChangePasswordEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/change-password", async
            (HttpContext httpContext, ChangePasswordRequest request, ChangePasswordValidator validator, ChangePasswordHandler handler, CancellationToken cancellationToken) =>
            {
                var userId = httpContext.User.FindFirst("id")?.Value;

                if (!int.TryParse(userId, out var id))
                {
                    return Results.BadRequest(new ApiResult<bool>(false, false, "Invalid user ID."));
                }
                
                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage);
                    return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages));
                }

                var response = await handler.Handle(request, id, cancellationToken);

                return response.Success
                    ? Results.Ok(response)
                    : Results.NotFound(response);
            }).RequireAuthorization();
    }
}