using FluentValidation;
using MeteorCloud.Shared.ApiResults;
using UserService.Persistence;

namespace UserService.Features;

public record GetUserByEmailRequest(string Email);

public class GetUserByEmailValidator : AbstractValidator<GetUserByEmailRequest>
{
    public GetUserByEmailValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Invalid email format.");
    }
}

public class GetUserByEmailHandler
{
    private readonly Services.UserService _userService;

    public GetUserByEmailHandler(Services.UserService userService)
    {
        _userService = userService;
    }

    public async Task<ApiResult<int?>> Handle(GetUserByEmailRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var user = await _userService.GetUserByEmailAsync(request.Email);

        if (user == null)
        {
            return new ApiResult<int?>(null, false, "User not found.");
        }

        return new ApiResult<int?>(user.Id);
    }
}

public class GetUserByEmailEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/email/{email}",
            async (string email, GetUserByEmailValidator validator, GetUserByEmailHandler handler, CancellationToken cancellationToken) =>
            {
                var request = new GetUserByEmailRequest(email);
                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage);
                    return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages));
                }

                var response = await handler.Handle(request, cancellationToken);

                return response.Success
                    ? Results.Ok(response)
                    : Results.NotFound(response);
            }).WithName("GetUserWithEmail");
    }
}

