using FluentValidation;
using MeteorCloud.Shared.ApiResults;
using Microsoft.AspNetCore.Mvc;
using UserService.Persistence;
using UserService.Services;

namespace UserService.Features
{
    
        public record GetUsersRequest(string? Search, int Page, int PageSize);

        public record GetUsersResponse(IEnumerable<User> Users);

        public class GetUsersValidator : AbstractValidator<GetUsersRequest>
        {
            public GetUsersValidator()
            {
                RuleFor(x => x.Page).GreaterThan(0);
                RuleFor(x => x.PageSize).GreaterThan(0);
            }
        }

        public class GetUsersHandler
        {
            private readonly UserManager _userManager;

            public GetUsersHandler(UserManager userManager)
            {
                _userManager = userManager;
            }

            public async Task<ApiResult<GetUsersResponse>> Handle(GetUsersRequest request)
            {
                var users = await _userManager.GetUsersAsync(request.Search, request.Page, request.PageSize);
                return new ApiResult<GetUsersResponse>(new GetUsersResponse(users));
            }
        }

        public static class GetUsersEndpoint
        {
            public static void Register(IEndpointRouteBuilder app)
            {
                app.MapPost("/api/users/list",
                    async (GetUsersRequest request, GetUsersValidator validator, GetUsersHandler handler) =>
                    {
                        var validationResult = await validator.ValidateAsync(request);

                        if (!validationResult.IsValid)
                        {
                            var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage);
                            return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages, false));
                        }

                        var response = await handler.Handle(request);
                        return Results.Ok(response);
                    }).WithName("GetUsers");
            }
        }
    
}