using EmailService.Persistence;
using FluentValidation;
using MeteorCloud.Shared.ApiResults;

namespace EmailService.Features;

public record MarkAsReadRequest(int NotificationId);

public class MarkAsReadValidator : AbstractValidator<MarkAsReadRequest>
{
    public MarkAsReadValidator()
    {
        RuleFor(x => x.NotificationId).NotEmpty().WithMessage("Notification ID is required.");
    }
}

public class MarkAsReadHandler
{
    private readonly NotificationRepository _notificationRepository;

    public MarkAsReadHandler(NotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<ApiResult<bool>> Handle(MarkAsReadRequest request, CancellationToken? cancellationToken = null)
    {
        var notification = await _notificationRepository.GetByIdAsync(request.NotificationId);
        if (notification == null)
        {
            return new ApiResult<bool>(false, false,"Notification not found.");
        }

        await _notificationRepository.MarkAsReadAsync(request.NotificationId);

        return new ApiResult<bool>(true);
    }
}

public static class MarkAsReadEndpoint
{
    public static void Register(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/notifications/{id}/read", async (
            int id,
            MarkAsReadHandler handler,
            MarkAsReadValidator validator,
            CancellationToken cancellationToken) =>
        {
            var request = new MarkAsReadRequest(id);

            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage);
                return Results.BadRequest(new ApiResult<IEnumerable<string>>(errorMessages));
            }

            var response = await handler.Handle(request, cancellationToken);
            return response.Success ? Results.Ok(response) : Results.NotFound(response);
        }).RequireAuthorization();
    }
}