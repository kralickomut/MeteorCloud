using EmailService.Extensions;
using EmailService.Features;
using EmailService.Hubs;
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
            .AllowCredentials()
            .WithOrigins("http://localhost:4200");
    });
});

builder.Services.RegisterServices(configuration);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5160);
});

var app = builder.Build();

await app.InitializeDatabaseAsync();

app.UseRouting();
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Email Service API V1");
    });
}

app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    GetUnreadNotificationsEndpoint.Register(endpoints);
    MarkAsReadEndpoint.Register(endpoints);
});

app.MapHub<NotificationHub>("/hub/notifications");

app.Run();