using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using StepGo.Api.Shared.Json;
using StepGo.Identity.Infrastructure;
using StepGo.Shared.Application;
using StepGo.Shared.Domain;

namespace StepGo.Api.Shared.Routing;

public sealed record RouteContext(APIGatewayHttpApiV2ProxyRequest Request, IReadOnlyDictionary<string, string> PathParameters, ICurrentUserAccessor CurrentUser);

public delegate Task<APIGatewayHttpApiV2ProxyResponse> RouteHandler(RouteContext context, CancellationToken ct);

/// <summary>
/// Deliberately not a reflection-driven MVC-style router: routes are a flat list matched by exact
/// method + segment-by-segment path comparison (a literal segment or a "{name}" capture). This is the
/// AOT-safe alternative to Amazon.Lambda.AspNetCoreServer.Hosting's Minimal API endpoint resolution
/// (design.md decision 2).
/// </summary>
public sealed class MiniRouter
{
    private readonly List<(string Method, string[] Segments, RouteHandler Handler, bool RequiresAuth)> _routes = [];

    public MiniRouter MapGet(string path, RouteHandler handler, bool requiresAuth = true) => Map("GET", path, handler, requiresAuth);
    public MiniRouter MapPost(string path, RouteHandler handler, bool requiresAuth = true) => Map("POST", path, handler, requiresAuth);
    public MiniRouter MapPut(string path, RouteHandler handler, bool requiresAuth = true) => Map("PUT", path, handler, requiresAuth);

    /// <summary>Health check endpoints never require auth — load balancers / uptime checks don't carry a JWT.</summary>
    public MiniRouter MapHealthCheck(string path = "/health") => Map("GET", path, (_, _) => Task.FromResult(new APIGatewayHttpApiV2ProxyResponse { StatusCode = 200, Body = "OK" }), requiresAuth: false);

    private MiniRouter Map(string method, string path, RouteHandler handler, bool requiresAuth)
    {
        _routes.Add((method, path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries), handler, requiresAuth));
        return this;
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> DispatchAsync(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext lambdaContext, CancellationToken ct)
    {
        var method = request.RequestContext?.Http?.Method ?? "GET";
        var requestSegments = (request.RequestContext?.Http?.Path ?? "/").Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        foreach (var (routeMethod, routeSegments, handler, requiresAuth) in _routes)
        {
            if (!string.Equals(routeMethod, method, StringComparison.OrdinalIgnoreCase) || routeSegments.Length != requestSegments.Length)
            {
                continue;
            }

            var pathParameters = new Dictionary<string, string>();
            var matched = true;
            for (var i = 0; i < routeSegments.Length; i++)
            {
                if (routeSegments[i].StartsWith('{') && routeSegments[i].EndsWith('}'))
                {
                    pathParameters[routeSegments[i][1..^1]] = requestSegments[i];
                }
                else if (!string.Equals(routeSegments[i], requestSegments[i], StringComparison.Ordinal))
                {
                    matched = false;
                    break;
                }
            }

            if (!matched)
            {
                continue;
            }

            try
            {
                var currentUser = requiresAuth ? ResolveCurrentUser(request) : AnonymousCurrentUser.Instance;
                return await handler(new RouteContext(request, pathParameters, currentUser), ct);
            }
            catch (AuthorizationException ex)
            {
                return JsonResponses.Error(403, "forbidden", ex.Message);
            }
            catch (DomainException ex)
            {
                return JsonResponses.Error(400, ex.Code, ex.Message);
            }
            catch (Exception ex)
            {
                lambdaContext.Logger.LogError($"Unhandled error: {ex}");
                return JsonResponses.Error(500, "internal_error", "系統發生未預期的錯誤。");
            }
        }

        return JsonResponses.Error(404, "not_found", "找不到對應的路由。");
    }

    private static ICurrentUserAccessor ResolveCurrentUser(APIGatewayHttpApiV2ProxyRequest request)
    {
        var claims = request.RequestContext?.Authorizer?.Jwt?.Claims
            ?? throw new AuthorizationException("缺少有效的身分驗證 token。");

        return JwtClaimsCurrentUserAccessor.FromClaims((IReadOnlyDictionary<string, string>)claims);
    }

    private sealed class AnonymousCurrentUser : ICurrentUserAccessor
    {
        public static readonly AnonymousCurrentUser Instance = new();
        public Guid UserId => Guid.Empty;
        public Role Role => Role.Student;
    }
}
