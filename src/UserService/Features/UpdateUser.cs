using FluentValidation;
using MassTransit;
using MeteorCloud.Messaging.Events;
using MeteorCloud.Shared.ApiResults;

namespace UserService.Features;

public record UpdateUserRequest(string Name, string Description);

public record UpdateUserResponse(string Name, string Description);

public class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(50)
            .WithMessage("Name must be at most 50 characters long.");
        
        RuleFor(x => x.Description)
            .MaximumLength(200)
            .WithMessage("Description must be at most 200 characters long.");
    }
}

public class UpdateUserHandler
{
    private readonly Services.UserService _userService;
    private readonly IPublishEndpoint _publishEndpoint;
    
    public UpdateUserHandler(Services.UserService userService, IPublishEndpoint publishEndpoint)
    {
        _userService = userService;
        _publishEndpoint = publishEndpoint;
    }
    
    public async Task<ApiResult<UpdateUserResponse>> Handle(int userId, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return new ApiResult<UpdateUserResponse>(null, false, "User not found.");
        }

        if (user.Name == request.Name && user.Description == request.Description)
        {
            return new ApiResult<UpdateUserResponse>(null, true, "Changes saved");
        }

        if (user.Name != request.Name)
        {
            await _publishEndpoint.Publish(new UserNameChangedEvent()
            {
                UserId = userId,
                NewName = request.Name
            });
        }
        
        user.Name = request.Name;
        user.Description = request.Description;
    
        await _userService.UpdateUserAsync(user);

        return new ApiResult<UpdateUserResponse>(new UpdateUserResponse(user.Name, user.Description));
    }
}

public class UpdateUserEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/users",
                async (HttpContext context,UpdateUserRequest request, UpdateUserValidator validator, UpdateUserHandler handler, CancellationToken cancellationToken) =>
                {
                    var userId = int.Parse(context.User.FindFirst("id")?.Value ?? "0");
                    
                    if (userId == 0)
                    {
                        return Results.Unauthorized();
                    }
                    
                    var validationResult = await validator.ValidateAsync(request, cancellationToken);
                    if (!validationResult.IsValid)
                    {
                        var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage);
                        return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages));
                    }
                    
                    var response = await handler.Handle(userId, request, cancellationToken);

                    return response.Success
                        ? Results.Ok(response)
                        : Results.NotFound(response);
                }).WithName("UpdateUser")
            .RequireAuthorization();
    }
}