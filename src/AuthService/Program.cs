using AuthService.Extensions;
using AuthService.Features.Auth;
using MeteorCloud.Shared.Jwt;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("CorsPolicy", builder =>
    {
        builder.AllowAnyMethod()
            .AllowAnyHeader()
            .WithOrigins("http://localhost:4200");
    });
});

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

app.UseCors("CorsPolicy");

app.UseEndpoints(endpoints =>
{
    RegisterEndpoint.Register(endpoints);
    VerifyEndpoint.Register(endpoints);
    ResendCodeEndpoint.Register(endpoints);
    LoginEndpoint.Register(endpoints);
    
});

app.Run();