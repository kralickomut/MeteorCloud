using AuditService.Persistence;
using AuditService.Persistence.Entities;
using FluentValidation;
using MeteorCloud.Communication;
using MeteorCloud.Shared.ApiResults;
using MeteorCloud.Shared.SharedDto.Audit;
using MeteorCloud.Shared.SharedDto.Users;

namespace AuditService.Features.File;

public record GetFileHistoryByWorkspaceIdRequest(int WorkspaceId, int Page = 1, int PageSize = 10);

public class GetFileHistoryByWorkspaceIdValidator : AbstractValidator<GetFileHistoryByWorkspaceIdRequest>
{
    public GetFileHistoryByWorkspaceIdValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty()
            .WithMessage("Workspace ID cannot be empty.");
        
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than 0.");
    }
}


public class GetFileHistoryByWorkspaceIdHandler
{
    private readonly AuditRepository _repository;
    private readonly MSHttpClient _httpClient;
    private readonly ILogger<GetFileHistoryByWorkspaceIdHandler> _logger;

    public GetFileHistoryByWorkspaceIdHandler(AuditRepository repository, MSHttpClient httpClient, ILogger<GetFileHistoryByWorkspaceIdHandler> logger)
    {
        _repository = repository;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiResult<PagedResult<AuditEventModel>>> Handle(GetFileHistoryByWorkspaceIdRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _repository.GetFileHistoryByWorkspaceIdAsync(request.WorkspaceId, request.Page, request.PageSize);

        if (result.TotalCount == 0)
            return new ApiResult<PagedResult<AuditEventModel>>(new PagedResult<AuditEventModel>(), true, "No history found.");

        var userIds = result.Items.Select(x => x.PerformedByUserId).Distinct().ToList();
        
        var url = MicroserviceEndpoints.UserService.GetUsersBulk();
        var userResponse = await _httpClient.PostAsync<object, IEnumerable<UserModel>>(url, new { userIds }, cancellationToken);

        var userMap = userResponse.Success
            ? userResponse.Data.ToDictionary(u => u.Id, u => u.Name)
            : new Dictionary<int, string>();

        var models = result.Items.Select(item => new AuditEventModel
        {
            AuditEventId = item.Id,
            FileName = item.Metadata.GetValueOrDefault("FileName", "Unknown"),
            ActionByName = userMap.GetValueOrDefault(item.PerformedByUserId, "Unknown"),
            Action = item.Action,
            CreatedOn = item.Timestamp
        }).ToList();

        return new ApiResult<PagedResult<AuditEventModel>>(new PagedResult<AuditEventModel>
        {
            Items = models,
            TotalCount = result.TotalCount
        });
    }
}


public class GetFileHistoryByWorkspaceIdEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/audit/file-history/{workspaceId}",
            async (
                int workspaceId,
                int page,
                int pageSize,
                GetFileHistoryByWorkspaceIdHandler handler,
                GetFileHistoryByWorkspaceIdValidator validator,
                CancellationToken cancellationToken) =>
            {
                var request = new GetFileHistoryByWorkspaceIdRequest(workspaceId, page, pageSize);

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