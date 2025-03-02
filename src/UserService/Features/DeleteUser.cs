using FluentValidation;
using MeteorCloud.Shared.ApiResults;
using UserService.Services;

namespace UserService.Features
{
    public record DeleteUserRequest(int Id);
    
    public class DeleteUserValidator : AbstractValidator<DeleteUserRequest>
    {
        public DeleteUserValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
    
    public class DeleteUserHandler
    {
        private readonly UserManager _userManager;

        public DeleteUserHandler(UserManager userManager)
        {
            _userManager = userManager;
        }

        public async Task<ApiResult<object>> Handle(DeleteUserRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var success = await _userManager.DeleteUserAsync(request.Id, cancellationToken);

            return success
                ? new ApiResult<object>(null)
                : new ApiResult<object>(null, false, "User not found");
        }
    }
    
    public static class DeleteUserEndpoint
    {
        public static void Register(IEndpointRouteBuilder app)
        {
            app.MapDelete("/api/users/{id}",
                async (int id, DeleteUserValidator validator, DeleteUserHandler handler, CancellationToken cancellationToken) =>
                {
                    var request = new DeleteUserRequest(id);
                    var validationResult = await validator.ValidateAsync(request, cancellationToken);

                    if (!validationResult.IsValid)
                    {
                        var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage);
                        return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages, false));
                    }

                    var response = await handler.Handle(request, cancellationToken);

                    return response.Success == true
                        ? Results.Ok(response)
                        : Results.NotFound(response);
                }).WithName("DeleteUser");
        }
    }
}