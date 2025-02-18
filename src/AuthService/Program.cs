using AuthService.Extensions;
using AuthService.Features;
using AuthService.Features.Auth;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

// Register Dependencies
builder.Services.RegisterServices(configuration);

// Bind for port 5296
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5296);
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
    GetCredentialsByEmailEndpoint.Register(endpoints);
    CreateCredentialsEndpoint.Register(endpoints);
    
    LoginEndpoint.Register(endpoints);
    
});

app.Run();