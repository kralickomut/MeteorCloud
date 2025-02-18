using Microsoft.OpenApi.Models;
using UserService.Extensions;
using UserService.Features;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

// Register Dependencies
builder.Services.RegisterServices(configuration);

// Bind for port 5295
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5295);
});

var app = builder.Build();

await app.InitializeDatabaseAsync();

app.UseRouting();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Service API V1");
    });
}

app.UseEndpoints(endpoints =>
{
    CreateUserEndpoint.Register(endpoints);
    GetUserEndpoint.Register(endpoints);
    GetUsersEndpoint.Register(endpoints);
    UpdateUserEndpoint.Register(endpoints);
    DeleteUserEndpoint.Register(endpoints);
});

app.Run();