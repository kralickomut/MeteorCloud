using System.Net;
using FileService.Presentation.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Any, 8888));

builder.Services.AddProjectServices(configuration: builder.Configuration);

var app = builder.Build();

app.MapGet("/files", () => Results.Ok("Hello"));
app.MapGet("/files/{id}", (string id) => Results.Ok($"File {id} fetched."));


app.Run();