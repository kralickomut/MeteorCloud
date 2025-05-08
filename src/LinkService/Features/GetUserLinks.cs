using FluentValidation;
using LinkService.Persistence.Entities;
using LinkService.Services;
using MeteorCloud.Shared.ApiResults;

namespace LinkService.Features;

public record GetUserLinksRequest(int UserId, int Page = 1, int PageSize = 10);

public class GetUserLinksValidator : AbstractValidator<GetUserLinksRequest>
{
    public GetUserLinksValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage("User ID must be greater than 0");
    }
}

public class GetUserLinksHandler
{
    private readonly FastLinkManager _fastLinkManager;

    public GetUserLinksHandler(FastLinkManager fastLinkManager)
    {
        _fastLinkManager = fastLinkManager;
    }

    public async Task<ApiResult<PagedResult<FastLink>>> Handle(GetUserLinksRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var links = await _fastLinkManager.GetByUserIdAsync(request.UserId, request.Page, request.PageSize);

        return new ApiResult<PagedResult<FastLink>>(links);
    }
}


public static class GetUserLinksEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/links/user/{userId:int}",
            async (int userId, int page, int pageSize,
                GetUserLinksHandler handler,
                GetUserLinksValidator validator,
                CancellationToken cancellationToken) =>
            {
                var request = new GetUserLinksRequest(userId, page, pageSize);

                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage);
                    return Results.BadRequest(new ApiResult<IEnumerable<string>>(errors));
                }

                var result = await handler.Handle(request, cancellationToken);
                return Results.Ok(result);
            });
    }
}

