using StepGo.Api.Shared.Composition;
using StepGo.Api.Shared.Json;
using StepGo.Api.Shared.Routing;
using StepGo.Notifications.Application;
using StepGo.Contracts.Json;
using StepGo.Contracts.Notifications;
using StepGo.Shared.Domain;

namespace StepGo.Api.Notifications;

public static class NotificationsRoutes
{
    public static MiniRouter Map(MiniRouter router, CompositionRoot root, StepGoJsonContext contractsJson) => router
        .MapHealthCheck("/notifications/health")
        .MapPost("/notification-templates/{key}", async (ctx, ct) =>
        {
            var request = JsonResponses.Deserialize(ctx.Request.Body, contractsJson.UpdateNotificationTemplateRequestDto)
                ?? throw new DomainException("invalid_request", "請求內容不正確。");
            var key = ctx.PathParameters["key"];

            var handler = new ManageNotificationTemplateHandler(root.NotificationTemplateRepository, ctx.CurrentUser, root.Clock);
            var template = await handler.HandleAsync(new UpdateNotificationTemplateCommand(key, request.BodyTemplate), ct);
            return JsonResponses.Ok(new NotificationTemplateDto(template.Id, template.BodyTemplate, template.UpdatedBy, template.UpdatedAt), contractsJson.NotificationTemplateDto);
        })
        .MapGet("/users/{userId}/notifications", async (ctx, ct) =>
        {
            var userId = Guid.Parse(ctx.PathParameters["userId"]);
            StepGo.Identity.Application.RowLevelAccessGuard.GuardOwnsStudentResource(ctx.CurrentUser, userId);

            var handler = new ListInAppNotificationsHandler(root.NotificationRecordRepository);
            var records = await handler.HandleAsync(userId, ct);

            return JsonResponses.Ok(
                new StepGo.Contracts.Common.PagedResultDto<NotificationRecordDto>(
                    [.. records.Select(r => new NotificationRecordDto(r.Id, r.UserId, r.TemplateKey, r.RenderedText, (NotificationChannelDto)r.DispatchedChannels, r.SentAt, r.IsRead))],
                    null),
                contractsJson.PagedResultDtoNotificationRecordDto);
        })
        .MapPost("/account/notification-settings", (ctx, ct) =>
        {
            var request = JsonResponses.Deserialize(ctx.Request.Body, contractsJson.UpdateAccountNotificationSettingsRequestDto)
                ?? throw new DomainException("invalid_request", "請求內容不正確。");

            new UpdateAccountNotificationSettingsHandler().Handle(new UpdateAccountNotificationSettingsCommand(request.DisabledChannels));
            return Task.FromResult(JsonResponses.NoContent());
        });
}
