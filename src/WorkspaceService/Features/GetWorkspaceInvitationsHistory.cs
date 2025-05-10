using FluentValidation;
using MeteorCloud.Communication;
using MeteorCloud.Shared.ApiResults;
using MeteorCloud.Shared.ApiResults.SharedDto;
using MeteorCloud.Shared.SharedDto.Users;
using WorkspaceService.Persistence.Entities;
using WorkspaceService.Services;

namespace WorkspaceService.Features;

public record GetWorkspaceInvitationsHistoryRequest(int WorkspaceId, int Page = 1, int PageSize = 10);

public class GetWorkspaceInvitationsHistoryValidator : AbstractValidator<GetWorkspaceInvitationsHistoryRequest>
{
    public GetWorkspaceInvitationsHistoryValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty()
            .WithMessage("Workspace ID is required.")
            .GreaterThan(0)
            .WithMessage("Workspace ID must be greater than 0.");
        
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");
    }
}


public class GetWorkspaceInvitationsHistoryHandler
{
    private readonly WorkspaceManager _workspaceManager;
    private readonly MSHttpClient _httpClient;
    
    public GetWorkspaceInvitationsHistoryHandler(WorkspaceManager workspaceManager, MSHttpClient httpClient)
    {
        _httpClient = httpClient;
        _workspaceManager = workspaceManager;
    }
    
    public async Task<ApiResult<PagedResult<WorkspaceInvitationHistoryDto>>> Handle(GetWorkspaceInvitationsHistoryRequest request, CancellationToken cancellationToken)
    {
        var workspace = await _workspaceManager.GetWorkspaceByIdAsync(request.WorkspaceId);

        if (workspace is null)
        {
            return new ApiResult<PagedResult<WorkspaceInvitationHistoryDto>>(null, false, "Workspace not found.");
        }

        var pagedInvitations = await _workspaceManager.GetWorkspaceInvitationsAsync(
            request.WorkspaceId,
            request.Page,
            request.PageSize,
            cancellationToken);

        var invitations = pagedInvitations.Items;

        var userIds = invitations
            .SelectMany(i => new[] { i.InvitedByUserId, i.AcceptedByUserId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        // If there are no relevant users, just map empty user names
        var userModels = new List<UserModel>();
        if (userIds.Any())
        {
            var url = MicroserviceEndpoints.UserService.GetUsersBulk();
            var userResponse = await _httpClient.PostAsync<object, IEnumerable<UserModel>>(url, new { userIds });

            if (!userResponse.Success)
            {
                return new ApiResult<PagedResult<WorkspaceInvitationHistoryDto>>(null, false, "Failed to fetch user details.");
            }

            userModels = userResponse.Data.ToList();
        }

        // 🟡 Map to DTOs
        var historyDtos = invitations.Select(invitation =>
        {
            var invitedByUser = userModels.FirstOrDefault(u => u.Id == invitation.InvitedByUserId);
            var acceptedByUser = userModels.FirstOrDefault(u => u.Id == invitation.AcceptedByUserId);

            return new WorkspaceInvitationHistoryDto
            {
                Email = invitation.Email,
                Status = invitation.Status,
                Date = invitation.CreatedOn,
                InvitedByName = invitedByUser?.Name ?? "(unknown)",
                AcceptedByName = acceptedByUser?.Name ?? "",
                AcceptedOn = invitation.AcceptedOn
            };
        }).ToList();

        return new ApiResult<PagedResult<WorkspaceInvitationHistoryDto>>(new PagedResult<WorkspaceInvitationHistoryDto>
        {
            Items = historyDtos,
            TotalCount = pagedInvitations.TotalCount
        });
    }
}


public class GetWorkspaceInvitationsHistoryEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/workspaces/invitations-history/{id}", 
            async (
                int id,
                int page, 
                int pageSize, 
                GetWorkspaceInvitationsHistoryHandler handler,
                GetWorkspaceInvitationsHistoryValidator validator,
                CancellationToken cancellationToken) =>
            {
                var request = new GetWorkspaceInvitationsHistoryRequest(id, page, pageSize);
                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                
                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage);
                    return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages));
                }

                var response = await handler.Handle(request, cancellationToken);

                return response.Success
                    ? Results.Ok(response)
                    : Results.NotFound(response);
            });
    }
}