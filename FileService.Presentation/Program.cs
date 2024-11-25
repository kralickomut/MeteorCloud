using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Any, 8888));

var app = builder.Build();

app.MapGet("/files", () => Results.Ok("File list fetched with update."));
app.MapGet("/files/{id}", (string id) => Results.Ok($"File {id} fetched."));


app.Run();