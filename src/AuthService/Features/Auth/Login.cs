using AuthService.Services;
using FluentValidation;
using MassTransit;
using MeteorCloud.Messaging.Events;
using MeteorCloud.Shared.ApiResults;

namespace AuthService.Features.Auth;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, int UserId);

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6)
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

public class LoginHandler
{
    private readonly CredentialService _credentialService;
    private readonly TokenService _tokenService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<LoginHandler> _logger;

    public LoginHandler(CredentialService credentialService,
        TokenService tokenService, 
        IPublishEndpoint publishEndpoint,
        ILogger<LoginHandler> logger)
    {
        _logger = logger;
        _credentialService = credentialService;
        _tokenService = tokenService;
        _publishEndpoint = publishEndpoint;
    }


    public async Task<ApiResult<LoginResponse>> Handle(LoginRequest request)
    {
        var credentials = await _credentialService.GetCredentialsByEmailAsync(request.Email);

        if (credentials is null || !BCrypt.Net.BCrypt.Verify(request.Password, credentials.PasswordHash))
        {
            return new ApiResult<LoginResponse>(null, false, "Invalid credentials.");
        }

        if (!credentials.IsVerified)
        {
            return new ApiResult<LoginResponse>(null, false, "Account not verified.");
        }

        var token = _tokenService.GenerateToken(credentials.UserId, credentials.Email);
        await _publishEndpoint.Publish(new UserLoggedInEvent(credentials.UserId));
        _logger.LogInformation("User {UserId} logged in successfully.", credentials.UserId);
        
        return new ApiResult<LoginResponse>(new LoginResponse(token, credentials.UserId));
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

                return response.Success
                    ? Results.Ok(response)
                    : Results.BadRequest(response);
            });
    }
}