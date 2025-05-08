using FileService.Extensions;
using FileService.Features;
using MeteorCloud.Shared.Jwt;
using Microsoft.AspNetCore.Http.Features;

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

// Register Dependencies
builder.Services.RegisterServices(configuration);

// Bind for port 5298
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5298);
    options.Limits.MaxRequestBodySize = 500_000_000; // 500 MB
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500_000_000; // 500 MB
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

app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();


app.UseEndpoints(endpoints =>
{
    UploadFileEndpoint.Register(endpoints);
    DeleteFileEndpoint.Register(endpoints);
    DeleteFolderEndpoint.Register(endpoints);
    DownloadFileEndpoint.Register(endpoints);
    ViewFileEndpoint.Register(endpoints);
    UploadFastLinkFileEndpoint.Register(endpoints);
    DeleteFastLinkFileEndpoint.Register(endpoints);
    MoveFileEndpoint.Register(endpoints);
});


app.Run();