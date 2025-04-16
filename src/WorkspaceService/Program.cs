using MeteorCloud.Shared.Jwt;
using WorkspaceService.Extensions;
using WorkspaceService.Features;
using WorkspaceService.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

var configuration = builder.Configuration;

// Register Dependencies
builder.Services.RegisterServices(configuration);

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


// Bind for port 5297
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5297);
});

var app = builder.Build();

await app.InitializeDatabaseAsync();

app.UseRouting();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Workspace Service API V1");
    });
}

app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();


app.UseEndpoints(endpoints =>
{
    CreateWorkspaceEndpoint.Register(endpoints);
    InviteToWorkspaceEndpoint.Register(endpoints);
    GetUserWorkspacesEndpoint.Register(endpoints);
    DeleteWorkspaceEndpoint.Register(endpoints);
    GetWorkspaceByIdEndpoint.Register(endpoints);
});

app.MapHub<WorkspaceHub>("/hub/workspaces");


app.Run();