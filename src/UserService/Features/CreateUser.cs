using System.Data;
using System.Text.RegularExpressions;
using FluentValidation;
using MeteorCloud.Shared.ApiResults;
using UserService.Extensions;
using UserService.Persistence;
using UserService.Services;

namespace UserService.Features
{

    public record CreateUserRequest(string FirstName, string LastName, string Email, string Password);
    
    public class CreateUserValidator : AbstractValidator<CreateUserRequest>
    {

        public CreateUserValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required");
            RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required");
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");
        }
    }

    public class CreateUserHandler
    {
        private readonly UserManager _userManager;

        public CreateUserHandler(UserManager userManager)
        {
            _userManager = userManager;
        }

        public async Task<ApiResult<int>> Handle(CreateUserRequest request)
        {
            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
            };

            var id = await _userManager.CreateUserAsync(user);

            return id.HasValue
                ? new ApiResult<int>(id.Value)
                : new ApiResult<int>(-1, success: false, "There is already a user with this email");
        }
    }

    public static class CreateUserEndpoint
    {
        public static void Register(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/users",
                async (CreateUserRequest request, CreateUserValidator validator, CreateUserHandler handler) =>
                {
                    var validationResult = await validator.ValidateAsync(request);
                    if (!validationResult.IsValid)
                    {
                        var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage);
                        return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages, success: false));
                    }

                    var response = await handler.Handle(request);
                    
                    return response.Success == true
                        ? Results.Ok(response)
                        : Results.BadRequest(response);
                    
                }).WithName("CreateUser");
        }
    }
}