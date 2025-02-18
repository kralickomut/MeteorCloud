using FluentValidation;
using MeteorCloud.Shared.ApiResults;
using UserService.Persistence;
using UserService.Services;

namespace UserService.Features
{
        public record UpdateUserRequest(int Id, string FirstName, string LastName, string Email);

        public class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
        {
            public UpdateUserValidator()
            {
                RuleFor(x => x.FirstName).NotEmpty();
                RuleFor(x => x.LastName).NotEmpty();
                RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Invalid email address");
            }
        }

        public class UpdateUserHandler
        {
            private readonly UserManager _userManager;

            public UpdateUserHandler(UserManager userManager)
            {
                _userManager = userManager;
            }

            public async Task<ApiResult<object>> Handle(UpdateUserRequest request)
            {
                var updatedUser = new User
                {
                    Id = request.Id,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email
                };

                var success = await _userManager.UpdateUserAsync(updatedUser);

                return success
                    ? new ApiResult<object>(null)
                    : new ApiResult<object>(null, false, "User not found");
            }
        }


        public static class UpdateUserEndpoint
        {
            public static void Register(IEndpointRouteBuilder app)
            {
                app.MapPut("/api/users",
                    async (UpdateUserRequest request, UpdateUserValidator validator, UpdateUserHandler handler) =>
                    {
                        var validationResult = await validator.ValidateAsync(request);

                        if (!validationResult.IsValid)
                        {
                            var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage);
                            return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages, false));
                        }

                        var response = await handler.Handle(request);

                        return response.Success == true
                            ? Results.Ok(response)
                            : Results.NotFound(response);
                    }).WithName("UpdateUser");
            }
        }
    
}