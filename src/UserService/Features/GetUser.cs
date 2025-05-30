using FluentValidation;
using MeteorCloud.Shared.ApiResults;
using MeteorCloud.Shared.SharedDto.Users;

namespace UserService.Features;

public record GetUserRequest(int Id);

public record GetUserResponse(UserModel User);

public class GetUserValidator : AbstractValidator<GetUserRequest>
{
    public GetUserValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

public class GetUserHandler
{
    private readonly Services.UserService _userService;

    public GetUserHandler(Services.UserService userService)
    {
        _userService = userService;
    }

    public async Task<ApiResult<GetUserResponse>> Handle(GetUserRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userService.GetUserByIdAsync(request.Id, cancellationToken);
        
        if (user == null)
        {
            return new ApiResult<GetUserResponse>(null, false, "User not found");
        }

        var userModel = user.ToModel();
        return new ApiResult<GetUserResponse>(new GetUserResponse(userModel));

    }
}

public static class GetUserEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{id}",
                async (int id, GetUserValidator validator, GetUserHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var request = new GetUserRequest(id);
                    var validationResult = await validator.ValidateAsync(request, cancellationToken);
                    if (!validationResult.IsValid)
                    {
                        var errorMessages = validationResult
                            .Errors
                            .Select(x => x.ErrorMessage);
                        
                        return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages));
                    }

                    var response = await handler
                        .Handle(request, cancellationToken);

                    return response.Success
                        ? Results.Ok(response)
                        : Results.NotFound(response);
                })
            .WithName("GetUser")
            .RequireAuthorization();
    }
}