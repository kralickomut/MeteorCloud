using AuthService.Persistence;
using AuthService.Services;
using FluentValidation;
using MeteorCloud.Shared.ApiResults;

namespace AuthService.Features.Credentials
{



    public record CreateCredentialsRequest(string Email, string Password, int UserId);

    public record CreateCredentialsResponse(int Id);

    public class CreateCredentialsValidator : AbstractValidator<CreateCredentialsRequest>
    {
        public CreateCredentialsValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    public class CreateCredentialsHandler
    {
        private readonly CredentialManager _credentialManager;

        public CreateCredentialsHandler(CredentialManager credentialManager)
        {
            _credentialManager = credentialManager;
        }

        public async Task<ApiResult<CreateCredentialsResponse>> Handle(CreateCredentialsRequest request)
        {
            var credential = new Credential
            {
                Email = request.Email,
                UserId = request.UserId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            var id = await _credentialManager.CreateCredentialsAsync(credential);

            return id.HasValue
                ? new ApiResult<CreateCredentialsResponse>(new CreateCredentialsResponse(id.Value))
                : new ApiResult<CreateCredentialsResponse>(null, false, "Credentials already exist.");
        }
    }

    public static class CreateCredentialsEndpoint
    {
        public static void Register(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/credentials",
                async (CreateCredentialsRequest request, CreateCredentialsHandler handler,
                    CreateCredentialsValidator validator) =>
                {
                    var validationResult = await validator.ValidateAsync(request);

                    if (!validationResult.IsValid)
                    {
                        var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage);
                        return Results.BadRequest(
                            new ApiResult<IEnumerable<string>>(errorMessages, false, "Validation failed."));
                    }

                    var response = await handler.Handle(request);

                    return response.Success is true
                        ? Results.Ok(response)
                        : Results.BadRequest(response);
                }).WithName("CreateCredentials");
        }
    }
}