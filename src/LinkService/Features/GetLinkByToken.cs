using FluentValidation;
using LinkService.Services;
using MeteorCloud.Communication;
using MeteorCloud.Shared.ApiResults;
using MeteorCloud.Shared.SharedDto.Users;

namespace LinkService.Features;

public record GetLinkByTokenRequest(Guid Token);

public record GetLinkByTokenResponse(string Name, Guid FileId, string FileName, long FileSize, DateTime CreatedAt, DateTime ExpiresAt, int AccessCount, string CreatedByUser, int OwnerId);

public class GetLinkByTokenValidator : AbstractValidator<GetLinkByTokenRequest>
{
    public GetLinkByTokenValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Token is required");
    }
}

public class GetLinkByTokenHandler
{
    private readonly FastLinkManager _fastLinkManager;
    private readonly MSHttpClient _httpClient;
    private readonly ILogger<GetLinkByTokenHandler> _logger;

    public GetLinkByTokenHandler(FastLinkManager fastLinkManager, MSHttpClient httpClient, ILogger<GetLinkByTokenHandler> logger)
    {
        _fastLinkManager = fastLinkManager;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiResult<GetLinkByTokenResponse>> Handle(GetLinkByTokenRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var link = await _fastLinkManager.GetByTokenAsync(request.Token);

        if (link is null)
        {
            return new ApiResult<GetLinkByTokenResponse>(null, false, "Link not found");
        }
        
        if (link.ExpiresAt < DateTime.UtcNow)
        {
            return new ApiResult<GetLinkByTokenResponse>(null, false, "Link expired");
        }

        var url = MicroserviceEndpoints.UserService.GetUserById(link.CreatedByUserId);
        var response = await _httpClient.GetAsync<UserResponse>(url);

        string userName;
        if (!response.Success || response.Data is null)
        {
            _logger.LogInformation("Failed to get user info for link {Token}", request.Token);
            userName = "Someone";
        }
        else
        {
            userName = response.Data.User.Name;
        }

        await _fastLinkManager.IncrementAccessCountAsync(link.Token);
        
        return new ApiResult<GetLinkByTokenResponse>(new GetLinkByTokenResponse(
            link.Name,
            link.FileId,
            link.FileName,
            link.FileSize,
            link.CreatedAt,
            link.ExpiresAt,
            link.AccessCount,
            userName,
            link.CreatedByUserId
        ));
        
    }
}

public class GetLinkByTokenEndpoint
{
    public static void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/links/{token}",
            async (string token,
                GetLinkByTokenHandler handler,
                GetLinkByTokenValidator validator,
                CancellationToken cancellationToken) =>
            {
                if (!Guid.TryParse(token, out var guid))
                {
                    return Results.BadRequest(new ApiResult<IEnumerable<string>>(null, false, "Invalid token format"));
                }
                
                var request = new GetLinkByTokenRequest(guid);
                
                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage);
                    return Results.BadRequest(new ApiResult<IEnumerable<string>>(errors));
                }
                
                var result = await handler.Handle(request, cancellationToken);
                return result.Success ? Results.Ok(result) : Results.NotFound(result);
            });
    }
}