

namespace FileService.Features;

public static class ViewFileEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/file/view/{**path}", async (
            HttpContext httpContext,
            string path,
            DownloadFileHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(path) || path.Length < 2)
            {
                httpContext.Response.StatusCode = 400;
                await httpContext.Response.WriteAsJsonAsync(new { Message = "Invalid path provided." });
                return;
            }

            var decodedPath = Uri.UnescapeDataString(path);
            var request = new DownloadFileRequest(decodedPath);
            var result = await handler.Handle(request, cancellationToken);

            if (result == null)
            {
                httpContext.Response.StatusCode = 404;
                await httpContext.Response.WriteAsJsonAsync(new { Message = "File not found." });
                return;
            }

            // 🛠️ Important fix: reset the stream position
            if (result.Stream.CanSeek)
            {
                result.Stream.Position = 0;
            }

            httpContext.Response.StatusCode = 200;
            httpContext.Response.ContentType = result.ContentType;
            httpContext.Response.Headers.ContentDisposition = "inline";

            await result.Stream.CopyToAsync(httpContext.Response.Body, cancellationToken);
        });
    }
}