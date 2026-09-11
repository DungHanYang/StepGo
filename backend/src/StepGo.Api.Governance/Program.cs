using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using StepGo.Api.Shared.Composition;
using StepGo.Api.Shared.Json;
using StepGo.Api.Shared.Routing;
using StepGo.Application.Governance;
using StepGo.Contracts.Courses;
using StepGo.Contracts.Governance;
using StepGo.Contracts.Json;
using StepGo.Domain.Courses;
using StepGo.Domain.Governance;
using StepGo.Domain.SharedKernel;

var root = new CompositionRoot();
var contractsJson = new StepGoJsonContext();

var router = new MiniRouter()
    .MapHealthCheck("/governance/health")
    .MapPost("/governance/fee-settings", async (ctx, ct) =>
    {
        var request = JsonResponses.Deserialize(ctx.Request.Body, contractsJson.ProposeFeeSettingRequestDto)
            ?? throw new DomainException("invalid_request", "請求內容不正確。");

        var handler = new ProposeFeeSettingHandler(root.PlatformFeeSettingRepository, root.ChangeLogRepository, ctx.CurrentUser, root.Clock);
        var setting = await handler.HandleAsync(new ProposeFeeSettingCommand(
            request.PlatformServiceFeeRate, Money.FromWholeDollars(request.CrossBankTransferFee),
            [.. request.RefundFloor.Select(t => new RefundTier(t.DaysBeforeCourseStart, t.RefundPercentage))], request.EffectiveDate), ct);

        return JsonResponses.Created(ToDto(setting), contractsJson.PlatformFeeSettingDto);
    })
    .MapGet("/governance/change-log/{category}", async (ctx, ct) =>
    {
        var category = Enum.Parse<ChangeLogCategory>(ctx.PathParameters["category"], ignoreCase: true);
        var handler = new ListChangeLogHandler(root.ChangeLogRepository);
        var entries = await handler.HandleAsync(category, ct);

        return JsonResponses.Ok(
            new StepGo.Contracts.Common.PagedResultDto<ChangeLogEntryDto>(
                [.. entries.Select(e => new ChangeLogEntryDto(e.Id, e.Category.ToString(), e.Description, e.OperatorId, e.OccurredAt))], null),
            contractsJson.PagedResultDtoChangeLogEntryDto);
    })
    .MapPost("/governance/terms", async (ctx, ct) =>
    {
        var request = JsonResponses.Deserialize(ctx.Request.Body, contractsJson.PublishTermsVersionRequestDto)
            ?? throw new DomainException("invalid_request", "請求內容不正確。");

        var handler = new PublishTermsVersionHandler(root.TermsVersionRepository, ctx.CurrentUser, root.Clock);
        var terms = await handler.HandleAsync(new PublishTermsVersionCommand(request.Content), ct);
        return JsonResponses.Created(new TermsVersionDto(terms.Id, terms.VersionNumber, terms.Content, terms.PublishedBy, terms.PublishedAt), contractsJson.TermsVersionDto);
    })
    .MapPost("/teachers/{teacherId}/terms-consent", async (ctx, ct) =>
    {
        var request = JsonResponses.Deserialize(ctx.Request.Body, contractsJson.RecordTeacherConsentRequestDto)
            ?? throw new DomainException("invalid_request", "請求內容不正確。");
        var teacherId = Guid.Parse(ctx.PathParameters["teacherId"]);

        var handler = new RecordTeacherConsentHandler(root.TermsVersionRepository, root.TeacherConsentRepository, root.Clock);
        await handler.HandleAsync(new RecordTeacherConsentCommand(teacherId, request.TermsVersionId), ct);
        return JsonResponses.NoContent();
    });

await LambdaBootstrapBuilder.Create<APIGatewayHttpApiV2ProxyRequest, APIGatewayHttpApiV2ProxyResponse>(
    (request, context) => router.DispatchAsync(request, context, CancellationToken.None),
    new SourceGeneratorLambdaJsonSerializer<ApiEventJsonContext>())
    .Build()
    .RunAsync();

static PlatformFeeSettingDto ToDto(PlatformFeeSetting setting) => new(
    setting.Id, setting.PlatformServiceFeeRate, setting.CrossBankTransferFee.Cents,
    [.. setting.RefundFloor.Tiers.Select(t => new RefundTierDto(t.DaysBeforeCourseStart, t.RefundPercentage))],
    setting.EffectiveDate, setting.ProposedBy, setting.ProposedAt);
