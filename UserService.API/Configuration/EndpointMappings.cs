using UserService.Application.Abstraction;
using UserService.Domain.Models;
// ReSharper disable All

namespace UserService.API.Configuration;

public static class EndpointMappings
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/user", async (IUserRepository _userRepository) =>
        {
            var user = new User
            {
                Username = "TestUser",
                Email = "asd@asdsasdasd.cz",
                PasswordHash = "c6x7@asd",
                CreatedOn = DateTime.Now,
                UpdatedOn = null,
                UserMetadataId = 1
            };
            
            var createdUser = await _userRepository.CreateUser(user);
            return Results.Ok(createdUser);
        });
        
        app.MapPost("/register", async (IUserRepository _userRepository) => { });
        
        
        app.MapGet("/", () =>
            Results.Ok("UserService.API is running.")
        );
    }
}