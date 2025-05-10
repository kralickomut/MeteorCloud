using FluentValidation;
using LinkService.Services;
using MeteorCloud.Shared.ApiResults;

namespace LinkService.Features;

public record RefreshLinkRequest(Guid Token, int Hours);

public class RefreshLinkValidator : AbstractValidator<RefreshLinkRequest>
{
    public RefreshLinkValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Token is required");

        RuleFor(x => x.Hours)
            .GreaterThan(0)
            .WithMessage("Hours must be greater than 0");
    }
}


public class RefreshLinkHandler
{
    private readonly FastLinkManager _fastLinkManager;
    
    public RefreshLinkHandler(FastLinkManager fastLinkManager)
    {
        _fastLinkManager = fastLinkManager;
    }
    
    public async Task<ApiResult<bool>> Handle(RefreshLinkRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var link = await _fastLinkManager.GetByTokenAsync(request.Token);

        if (link is null)
        {
            return new ApiResult<bool>(false, false, "Link not found");
        }

        link.ExpiresAt = DateTime.UtcNow.AddHours(request.Hours);
        
        var success = await _fastLinkManager.UpdateExpirationAsync(request.Token, DateTime.UtcNow.AddHours(request.Hours));
        
        if (!success)
        {
            return new ApiResult<bool>(false, false, "Failed to update link expiration");
        }

        return new ApiResult<bool>(true);
    }
}


public class RefreshLinkEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/links/refresh",
            async (RefreshLinkRequest request,
                RefreshLinkHandler handler,
                RefreshLinkValidator validator,
                CancellationToken cancellationToken) =>
            {
                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage);
                    return Results.BadRequest(new ApiResult<IEnumerable<string>>(errors));
                }

                var result = await handler.Handle(request, cancellationToken);
                return Results.Ok(result);
            }).RequireAuthorization();
    }
}