using FileService.Extensions;
using FileService.Features;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

// Register Dependencies
builder.Services.RegisterServices(configuration);

// Bind for port 5298
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5298);
});

var app = builder.Build();


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
    UploadFileEndpoint.Register(endpoints);
    DeleteFileEndpoint.Register(endpoints);
});

app.Run();