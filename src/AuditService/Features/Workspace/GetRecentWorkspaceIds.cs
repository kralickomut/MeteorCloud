using AuditService.Persistence;
using FluentValidation;
using MeteorCloud.Shared.ApiResults;

namespace AuditService.Features.Workspace;

public record GetRecentWorkspaceIdsRequest(int UserId, int Limit = 3);

public class GetRecentWorkspaceIdsValidator : AbstractValidator<GetRecentWorkspaceIdsRequest>
{
    public GetRecentWorkspaceIdsValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.")
            .GreaterThan(0)
            .WithMessage("UserId must be greater than 0.");

        RuleFor(x => x.Limit)
            .GreaterThan(0)
            .WithMessage("Limit must be greater than 0.");
    }
}

public class GetRecentWorkspaceIdsHandler
{
    private readonly AuditRepository _auditRepository;

    public GetRecentWorkspaceIdsHandler(AuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task<ApiResult<List<int>>> Handle(GetRecentWorkspaceIdsRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var recentWorkspaceIds = await _auditRepository.GetRecentWorkspaceIdsByUserAsync(request.UserId, request.Limit);
        return new ApiResult<List<int>>(recentWorkspaceIds);
    }
}

public class GetRecentWorkspaceIdsEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/audit/recent-workspaces/{userId}",
            async (
                int userId,
                int? limit,
                GetRecentWorkspaceIdsHandler handler,
                GetRecentWorkspaceIdsValidator validator,
                CancellationToken cancellationToken) =>
            {
                var request = new GetRecentWorkspaceIdsRequest(userId, limit ?? 3);

                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(x => x.ErrorMessage);
                    return Results.BadRequest(new ApiResult<IEnumerable<string>>(errors));
                }

                var response = await handler.Handle(request, cancellationToken);

                return response.Success
                    ? Results.Ok(response)
                    : Results.NotFound(response);
            });
    }
}