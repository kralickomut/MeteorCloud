using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Any, 7777));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();