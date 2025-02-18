using AuthService.Services;
using FluentValidation;
using MeteorCloud.Shared.ApiResults;

namespace AuthService.Features.Auth;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token);

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginHandler
{
    private readonly CredentialManager _credentialManager;
    private readonly TokenService _tokenService;

    public LoginHandler(CredentialManager credentialManager, TokenService tokenService)
    {
        _credentialManager = credentialManager;
        _tokenService = tokenService;
    }

    public async Task<ApiResult<LoginResponse>> Handle(LoginRequest request)
    {
        var credentials = await _credentialManager.GetCredentialsByEmailAsync(request.Email);

        if (credentials is null || !BCrypt.Net.BCrypt.Verify(request.Password, credentials.PasswordHash))
        {
            return new ApiResult<LoginResponse>(null, false, "Invalid credentials.");
        }

        var token = _tokenService.GenerateToken(credentials.UserId, credentials.Email);

        return new ApiResult<LoginResponse>(new LoginResponse(token));
    }
}

public static class LoginEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login",
            async (LoginHandler handler, LoginValidator validator, LoginRequest request) =>
            {
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage);
                    return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages, false, "Validation failed."));
                }

                var response = await handler.Handle(request);

                return response.Success is true
                    ? Results.Ok(response)
                    : Results.BadRequest(response);
            });
    }
}