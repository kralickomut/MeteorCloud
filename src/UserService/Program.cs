using MeteorCloud.Shared.Jwt;
using Microsoft.OpenApi.Models;
using UserService.Extensions;
using UserService.Features;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

var configuration = builder.Configuration;

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("CorsPolicy", builder =>
    {
        builder.AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithOrigins("http://localhost:4200", "https://localhost:4200");
    });
});


// Register Dependencies
builder.Services.RegisterServices(configuration);

// Bind for port 5295
var port = Environment.GetEnvironmentVariable("PORT") ?? "80";

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
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

app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    GetUserEndpoint.Register(endpoints);
    GetUserByEmailEndpoint.Register(endpoints);
    UpdateUserEndpoint.Register(endpoints);
    GetUsersBulkEndpoint.Register(endpoints);
});


app.Run();