using FluentValidation;
using MeteorCloud.Shared.ApiResults;
using WorkspaceService.Persistence.Entities;
using WorkspaceService.Services;

namespace WorkspaceService.Features;

public record GetWorkspaceInvitationByTokenRequest(string Token);

public class GetWorkspaceInvitationByTokenValidator : AbstractValidator<GetWorkspaceInvitationByTokenRequest>
{
    public GetWorkspaceInvitationByTokenValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Token is required.")
            .Must(IsGuid)
            .WithMessage("Token must be a valid GUID.");
    }
    
    
    private bool IsGuid(string token)
    {
        return Guid.TryParse(token, out _);
    }
}

public class GetWorkspaceInvitationByTokenHandler
{
    private readonly WorkspaceManager _workspaceService;

    public GetWorkspaceInvitationByTokenHandler(WorkspaceManager workspaceService)
    {
        _workspaceService = workspaceService;
    }

    public async Task<ApiResult<WorkspaceInvitation>> Handle(GetWorkspaceInvitationByTokenRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var invitation = await _workspaceService.GetInvitationByTokenAsync(Guid.Parse(request.Token), cancellationToken);
        return invitation != null
            ? new ApiResult<WorkspaceInvitation>(invitation)
            : new ApiResult<WorkspaceInvitation>(null, false, "Invitation not found");
    }
}


public class GetWorkspaceInvitationByTokenEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/workspace/invitations/{token}",
            async (string token, GetWorkspaceInvitationByTokenValidator validator, GetWorkspaceInvitationByTokenHandler handler, CancellationToken cancellationToken) =>
            {
                var request = new GetWorkspaceInvitationByTokenRequest(token);
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
            }).WithName("GetWorkspaceInvitationById");
    }
}