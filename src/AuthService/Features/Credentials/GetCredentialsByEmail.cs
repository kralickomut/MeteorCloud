using AuthService.Persistence;
using AuthService.Services;
using FluentValidation;
using MeteorCloud.Shared.ApiResults;

namespace AuthService.Features;

public record GetCredentialsByEmailRequest(string Email);

public record GetCredentialsByEmailResponse(Credential Credentials);


public class GetCredentialsByEmailValidator : AbstractValidator<GetCredentialsByEmailRequest>
{
    public GetCredentialsByEmailValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class GetCredentialsByEmailHandler
{
    private readonly CredentialManager _credentialManager;
    
    public GetCredentialsByEmailHandler(CredentialManager credentialManager)
    {
        _credentialManager = credentialManager;
    }

    public async Task<ApiResult<GetCredentialsByEmailResponse>> Handle(GetCredentialsByEmailRequest request)
    {
        var credentials = await _credentialManager.GetCredentialsByEmailAsync(request.Email);

        return credentials is null
            ? new ApiResult<GetCredentialsByEmailResponse>(null, false, "Credentials not found.")
            : new ApiResult<GetCredentialsByEmailResponse>(new GetCredentialsByEmailResponse(credentials));
    }
}

public static class GetCredentialsByEmailEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/credentials/{email}", 
            async (GetCredentialsByEmailHandler handler, GetCredentialsByEmailValidator validator, string email) =>
            {
                var request = new GetCredentialsByEmailRequest(email);

                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage);
                    return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages, false, "Validation failed."));
                }

                var response = await handler.Handle(request);
                
                return response.Success is true 
                    ? Results.Ok(response)
                    : Results.NotFound(response);
            });
    }
}