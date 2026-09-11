using Amazon.CDK;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudFront.Origins;
using Amazon.CDK.AWS.S3;
using Constructs;

namespace StepGoInfra
{
    /// <summary>
    /// apps/marketing (Blazor Web App, Static SSR) — CloudFront in front of API Gateway + Lambda.
    /// The Lambda/API Gateway origin itself is wired up in task 8.3; this stack already defines
    /// the CloudFront cache policies (task 3.5, design.md decision 11: "marketing 的 SSR 頁面可在
    /// CloudFront 設短 TTL cache，例如課程列表 60 秒，降低 Lambda 呼叫次數並加速爬蟲抓取").
    /// A placeholder S3 origin stands in for the API Gateway origin until 8.3.
    /// </summary>
    public class MarketingStack : Stack
    {
        internal MarketingStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // Placeholder origin — replaced with the API Gateway HTTP origin in task 8.3.
            var originBucket = new Bucket(this, "MarketingOriginPlaceholder", new BucketProps
            {
                RemovalPolicy = RemovalPolicy.DESTROY,
                AutoDeleteObjects = true,
            });
            var origin = S3BucketOrigin.WithOriginAccessControl(originBucket);

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
