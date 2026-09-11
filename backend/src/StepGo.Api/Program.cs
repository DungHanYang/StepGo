using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using StepGo.Api.Shared.Composition;
using StepGo.Api.Shared.Json;
using StepGo.Api.Shared.Routing;
using StepGo.Contracts.Json;

var root = new CompositionRoot();
var contractsJson = new StepGoJsonContext();

// All 8 capabilities share one Lambda + one HTTP API integration: each Routes.Map call registers its
// endpoints on the same MiniRouter instance, so there is a single cold start and a single deployable
// for the whole backend API surface instead of one Lambda per capability.
var router = new MiniRouter();
StepGo.Api.Courses.CoursesRoutes.Map(router, root, contractsJson);
StepGo.Api.Governance.GovernanceRoutes.Map(router, root, contractsJson);
StepGo.Api.Identity.IdentityRoutes.Map(router, root, contractsJson);
StepGo.Api.Notifications.NotificationsRoutes.Map(router, root, contractsJson);
StepGo.Api.Orders.OrdersRoutes.Map(router, root, contractsJson);
StepGo.Api.Payouts.PayoutsRoutes.Map(router, root, contractsJson);
StepGo.Api.RefundTickets.RefundTicketsRoutes.Map(router, root, contractsJson);

await LambdaBootstrapBuilder.Create<APIGatewayHttpApiV2ProxyRequest, APIGatewayHttpApiV2ProxyResponse>(
    (request, context) => router.DispatchAsync(request, context, CancellationToken.None),
    new SourceGeneratorLambdaJsonSerializer<ApiEventJsonContext>())
    .Build()
    .RunAsync();
