using EmailService.Persistence;
using FluentValidation;
using MeteorCloud.Shared.ApiResults;
using Org.BouncyCastle.Crypto.Engines;

namespace EmailService.Features;

public record GetUnreadNotificationsRequest(int UserId, int skip = 0, int take = 10);

public record GetUnreadNotificationsResponse(IEnumerable<Notification> Notifications);

public class GetUnreadNotificationsValidator : AbstractValidator<GetUnreadNotificationsRequest>
{
    public GetUnreadNotificationsValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
    }
}

public class GetUnreadNotificationsHandler
{
    private readonly NotificationRepository _notificationRepository;

    public GetUnreadNotificationsHandler(NotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<ApiResult<IEnumerable<Notification>>> Handle(GetUnreadNotificationsRequest request, CancellationToken? cancellationToken = null)
    {
        var notifications = await _notificationRepository.GetRecentNotificationsAsync(request.UserId, request.skip, request.take, cancellationToken);
        return new ApiResult<IEnumerable<Notification>>(notifications);
    }
}


public static class GetUnreadNotificationsEndpoint
{
    public static void Register(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/notifications/recent", async (HttpContext context, GetUnreadNotificationsHandler handler, GetUnreadNotificationsValidator validator, CancellationToken cancellationToken, int skip = 0, int take = 10) =>
        {
            var userId = context.User?.Claims
                .FirstOrDefault(x => x.Type == "id")?.Value;
            
            var request = new GetUnreadNotificationsRequest(int.Parse(userId));
            
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
        })
        .RequireAuthorization();
    }
}