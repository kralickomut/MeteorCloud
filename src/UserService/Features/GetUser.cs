using FluentValidation;
using MeteorCloud.Shared.ApiResults;
using UserService.Persistence;
using UserService.Services;

namespace UserService.Features
{
    public record GetUserRequest(int Id);
    
    public record GetUserResponse(User User);
    
    public class GetUserValidator : AbstractValidator<GetUserRequest>
    {
        public GetUserValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }

    public class GetUserHandler
    {
        private readonly UserManager _userManager;

        public GetUserHandler(UserManager userManager)
        {
            _userManager = userManager;
        }

        public async Task<ApiResult<GetUserResponse>> Handle(GetUserRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var user = await _userManager.GetUserByIdAsync(request.Id, cancellationToken);
            return user != null
                ? new ApiResult<GetUserResponse>(new GetUserResponse(user))
                : new ApiResult<GetUserResponse>(null, false, "User not found");
        }
    }

    public static class GetUserEndpoint 
    {
        public static void Register(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/users/{id}",
                async (int id, GetUserValidator validator, GetUserHandler handler, CancellationToken cancellationToken) =>
                {
                    var request = new GetUserRequest(id);
                    var validationResult = await validator.ValidateAsync(request, cancellationToken);
                    if (!validationResult.IsValid)
                    {
                        var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage);
                        return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages));
                    }

                    var response = await handler.Handle(request, cancellationToken);

                    return response.Success == true
                        ? Results.Ok(response)
                        : Results.NotFound(response);
                }).WithName("GetUser");
        }
    }
}