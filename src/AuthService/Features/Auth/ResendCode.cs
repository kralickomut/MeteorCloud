using AuthService.Services;
using FluentValidation;
using MeteorCloud.Shared.ApiResults;

namespace AuthService.Features.Auth;

public record ResendCodeRequest(string Email);

public class ResendCodeValidator : AbstractValidator<ResendCodeRequest>
{
    public ResendCodeValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class ResendCodeHandler
{
    private readonly CredentialService _credentialService;

    public ResendCodeHandler(CredentialService credentialService)
    {
        _credentialService = credentialService;
    }

    public async Task<ApiResult<bool>> Handle(ResendCodeRequest request, CancellationToken cancellationToken)
    {
        var result = await _credentialService.ResendVerificationCode(request.Email, cancellationToken);
        
        if (!result)
        {
            return new ApiResult<bool>(false, false, "Failed to resend verification code.");
        }
        
        return new ApiResult<bool>(true, true, "Verification code resent successfully.");
    }
}


public static class ResendCodeEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/resend-code",
            async (ResendCodeHandler handler, ResendCodeValidator validator, ResendCodeRequest request,
                CancellationToken cancellationToken) =>
            {
                var validationResult = await validator.ValidateAsync(request, cancellationToken);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage);
                    return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages, false, "Validation error."));
                }

                var result = await handler.Handle(request, cancellationToken);

                return result.Success
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            });
    }
}