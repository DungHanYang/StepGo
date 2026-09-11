using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudFront.Origins;
using Constructs;
using LambdaFunction = Amazon.CDK.AWS.Lambda.Function;
using LambdaFunctionProps = Amazon.CDK.AWS.Lambda.FunctionProps;
using LambdaRuntime = Amazon.CDK.AWS.Lambda.Runtime;
using LambdaCode = Amazon.CDK.AWS.Lambda.Code;

namespace StepGoInfra
{
    /// <summary>
    /// apps/marketing (Blazor Web App, Static SSR) — CloudFront in front of API Gateway + Lambda
    /// (design.md decision 1/11).
    ///
    /// NOTE: <see cref="Code.FromInline"/> below is a placeholder handler, not the real
    /// published StepGo.Marketing app. Deploying the actual Blazor Web App to Lambda needs the
    /// app itself updated to host via <c>Amazon.Lambda.AspNetCoreServer.Hosting</c> and a
    /// publish step producing the real deployment bundle — both out of scope for this frontend
    /// change; this stack's resource topology (CloudFront -> API Gateway -> Lambda, with the
    /// cache policies from task 3.5) is what's being established here.
    /// </summary>
    public class MarketingStack : Stack
    {
        internal MarketingStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var placeholderHandler = new LambdaFunction(this, "MarketingSsrFunction", new LambdaFunctionProps
            {
                Runtime = LambdaRuntime.NODEJS_22_X,
                Handler = "index.handler",
                Code = LambdaCode.FromInline(
                    "exports.handler = async () => ({ statusCode: 200, body: 'placeholder — real StepGo.Marketing Lambda bundle deploys here (see MarketingStack.cs)' });"),
                Timeout = Duration.Seconds(30),
                MemorySize = 512,
            });

            var httpApi = new HttpApi(this, "MarketingHttpApi", new HttpApiProps
            {
                DefaultIntegration = new HttpLambdaIntegration("MarketingSsrIntegration", placeholderHandler),
            });

            var origin = new HttpOrigin(Fn.Select(2, Fn.Split("/", httpApi.ApiEndpoint)));

            // Default: most marketing pages (home, about, terms, for-teachers/-students)
            // change rarely — cache for a few minutes at the edge.
            var defaultCachePolicy = new CachePolicy(this, "MarketingDefaultCachePolicy", new CachePolicyProps
            {
                CachePolicyName = "StepGoMarketingDefault",
                DefaultTtl = Duration.Minutes(5),
                MinTtl = Duration.Seconds(0),
                MaxTtl = Duration.Hours(1),
            });

            // Course list/detail change as teachers create/update courses — short TTL so
            // edge caching still cuts Lambda invocations without serving stale listings.
            var courseListingCachePolicy = new CachePolicy(this, "MarketingCourseListingCachePolicy", new CachePolicyProps
            {
                CachePolicyName = "StepGoMarketingCourseListing",
                DefaultTtl = Duration.Seconds(60),
                MinTtl = Duration.Seconds(0),
                MaxTtl = Duration.Minutes(5),
            });

            _ = new Distribution(this, "MarketingDistribution", new DistributionProps
            {
                DefaultBehavior = new BehaviorOptions
                {
                    Origin = origin,
                    CachePolicy = defaultCachePolicy,
                    ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                    AllowedMethods = AllowedMethods.ALLOW_ALL,
                },
                AdditionalBehaviors = new System.Collections.Generic.Dictionary<string, IBehaviorOptions>
                {
                    ["/courses*"] = new BehaviorOptions
                    {
                        Origin = origin,
                        CachePolicy = courseListingCachePolicy,
                        ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                    },
                },
            });
        }
    }
}
