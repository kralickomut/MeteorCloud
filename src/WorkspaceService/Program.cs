using WorkspaceService.Extensions;
using WorkspaceService.Features;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

// Register Dependencies
builder.Services.RegisterServices(configuration);

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

app.UseEndpoints(endpoints =>
{
    CreateWorkspaceEndpoint.Register(endpoints);
    DeleteWorkspaceEndpoint.Register(endpoints);
    UpdateWorkspaceEndpoint.Register(endpoints);
});

app.Run();