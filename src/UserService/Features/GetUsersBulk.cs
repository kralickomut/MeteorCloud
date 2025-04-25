using FluentValidation;
using MeteorCloud.Shared.ApiResults;
using MeteorCloud.Shared.SharedDto.Users;
using UserService.Persistence;

namespace UserService.Features;

public record GetUsersBulkRequest(List<int> userIds);

public class GetUsersBulkValidator : AbstractValidator<GetUsersBulkRequest>
{
    public GetUsersBulkValidator()
    {
        RuleFor(x => x.userIds)
            .NotEmpty()
            .WithMessage("User IDs cannot be empty.")
            .Must(x => x.Count <= 100)
            .WithMessage("You can only request up to 100 user IDs at a time.");
    }
}

public class GetUsersBulkHandler
{
    private readonly Services.UserService _userService;
    
    public GetUsersBulkHandler(Services.UserService userService)
    {
        _userService = userService;
    }
    
    public async Task<ApiResult<IEnumerable<UserModel>>> Handle(GetUsersBulkRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var users = await _userService.GetUsersByIdsAsync(request.userIds, cancellationToken);
        
        var userModels = users.Select(u => new UserModel
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Description = u.Description,
            InTotalWorkspaces = u.InTotalWorkspaces,
            LastLogin = u.LastLogin,
            RegistrationDate = u.RegistrationDate,
            UpdatedAt = u.UpdatedAt
        });
        
        return new ApiResult<IEnumerable<UserModel>>(userModels);
    }
}


public class GetUsersBulkEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {

        app.MapPost("/api/users/bulk", async 
            (GetUsersBulkRequest request, GetUsersBulkHandler handler, GetUsersBulkValidator validator, CancellationToken cancellationToken) =>
        {
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
        });
    }
}