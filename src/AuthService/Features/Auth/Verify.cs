using AuthService.Services;
using FluentValidation;
using MeteorCloud.Shared.ApiResults;

namespace AuthService.Features.Auth;

public record VerifyRequest(string code);

public record VerifyResponse(string message);

public class VerifyValidator : AbstractValidator<VerifyRequest>
{
    public VerifyValidator()
    {
        RuleFor(x => x.code).NotEmpty().WithMessage("Code is required.")
            .Must(IsValidCode).WithMessage("Code must be 6 digits long and contain only numbers.");
    }
    
    private bool IsValidCode(string code)
    {
        if (code.Length != 6) return false;
        
        if (!code.All(char.IsDigit)) return false;
        
        return true; 
    }
}

public class VerifyHandler
{
    private readonly CredentialService _credentialService;

    public VerifyHandler(CredentialService credentialService)
    {
        _credentialService = credentialService;
    }

    public async Task<ApiResult<VerifyResponse>> Handle(VerifyRequest request, CancellationToken cancellationToken)
    {
        var isValid = await _credentialService.Verify(request.code, cancellationToken);

        if (!isValid)
        {
            return new ApiResult<VerifyResponse>(new VerifyResponse("Invalid or expired code, try sending another one."), false, "Invalid verification code.");
        }

        return new ApiResult<VerifyResponse>(new VerifyResponse("You were successfully verified."), true, "User verified successfully.");
    }
}


public static class VerifyEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/verify",
            async (VerifyHandler handler, VerifyValidator validator, VerifyRequest request, CancellationToken cancellationToken) =>
            {
                var validationResult = await validator.ValidateAsync(request, cancellationToken);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage);
                    return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages, false, "Validation failed."));
                }

                var response = await handler.Handle(request, cancellationToken);

                return response.Success
                    ? Results.Ok(response)
                    : Results.BadRequest(response);
            });
    }
}
