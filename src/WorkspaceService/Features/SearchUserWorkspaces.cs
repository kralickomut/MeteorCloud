using MeteorCloud.Shared.ApiResults;
using WorkspaceService.Persistence;

namespace WorkspaceService.Features;

public static class SearchWorkspacesEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/workspaces/search", async (
            int userId,
            string query,
            int? limit,
            WorkspaceRepository repo,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(query) || userId <= 0)
                return Results.BadRequest("Query and valid userId required.");

            var workspaces = await repo.SearchUserWorkspacesAsync(userId, query, limit ?? 10, ct);
            return Results.Ok(new ApiResult<List<Workspace>>(workspaces, true));
        });
    }
}