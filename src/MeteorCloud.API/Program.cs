using FluentValidation;
using FluentValidation.AspNetCore;
using MeteorCloud.API.DTOs.Auth;
using MeteorCloud.API.Extensions;
using MeteorCloud.API.Validation;
using MeteorCloud.API.Validation.Auth;
using MeteorCloud.Communication;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5222);
});

builder.Services.RegisterServices(configuration);


var app = builder.Build();


if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth Service API v1");
    });
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization(); 
app.MapControllers();

app.Run();


// TODO: Workspace update event 
// TODO: DapperContext etc -> lib