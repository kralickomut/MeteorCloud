using System.Reflection;
using AuthService.Extensions;
using AuthService.Features.Auth;
using MeteorCloud.Shared.Jwt;

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
            .WithOrigins("http://localhost:4200", "https://localhost:4200");
    });
});

// Register Dependencies
builder.Services.RegisterServices(configuration);

// Bind for port 5296
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

var names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
foreach (var name in names)
{
    Console.WriteLine(name);
}

app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    RegisterEndpoint.Register(endpoints);
    VerifyEndpoint.Register(endpoints);
    ResendCodeEndpoint.Register(endpoints);
    LoginEndpoint.Register(endpoints);
    ChangePasswordEndpoint.Register(endpoints);
    PasswordResetRequiredEndpoint.Register(endpoints);
    ResetPasswordEndpoint.Register(endpoints);
});

app.Run();