using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using MeteorCloud.API.DTOs.Auth;
using MeteorCloud.API.DTOs.User;
using MeteorCloud.API.DTOs.Workspace;
using MeteorCloud.API.Validation.Auth;
using MeteorCloud.API.Validation.User;
using MeteorCloud.API.Validation.Workspace;
using MeteorCloud.Communication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace MeteorCloud.API.Extensions;

public static class ServiceExtensions
{
    public static void RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.SuppressModelStateInvalidFilter = true; // Disable automatic model validation
            });

        services.AddFluentValidation();
        services.AddValidatorsFromAssemblyContaining<Program>();
        
        services.AddSingleton<IValidator<UserRegistrationRequest>, RegistrationValidator>();
        services.AddSingleton<IValidator<UserLoginRequest>, LoginValidator>();
        services.AddSingleton<IValidator<UpdateUserRequest>, UpdateValidator>();
        services.AddSingleton<IValidator<WorkspaceCreateRequest>, CreateValidator>();
        
        services.AddHttpClient<MSHttpClient>(client =>
        {
            client.BaseAddress = new Uri(MicroserviceEndpoints.UserService);
        });
        
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "MeteorCloud API", Version = "v1" });
        });
        
        
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(secretKey)
                };
            });

        services.AddAuthorization();

    }
}