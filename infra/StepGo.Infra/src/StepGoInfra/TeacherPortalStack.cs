using Amazon.CDK;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudFront.Origins;
using Amazon.CDK.AWS.S3;
using Constructs;

namespace StepGoInfra
{
    /// <summary>
    /// apps/teacher-portal (Blazor WASM standalone) — pure static hosting via S3 + CloudFront,
    /// no server component at all (design.md decision 1/11).
    /// </summary>
    public class TeacherPortalStack : Stack
    {
        internal TeacherPortalStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var siteBucket = new Bucket(this, "TeacherPortalSiteBucket", new BucketProps
            {
                RemovalPolicy = RemovalPolicy.DESTROY,
                AutoDeleteObjects = true,
                BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            });

            _ = new Distribution(this, "TeacherPortalDistribution", new DistributionProps
            {
                DefaultRootObject = "index.html",
                DefaultBehavior = new BehaviorOptions
                {
                    Origin = S3BucketOrigin.WithOriginAccessControl(siteBucket),
                    ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                },
                // Blazor WASM standalone is a client-side-routed SPA — unknown paths must fall
                // back to index.html rather than S3's own 403/404.
                ErrorResponses =
                [
                    new ErrorResponse { HttpStatus = 403, ResponseHttpStatus = 200, ResponsePagePath = "/index.html" },
                    new ErrorResponse { HttpStatus = 404, ResponseHttpStatus = 200, ResponsePagePath = "/index.html" },
                ],
            });
        }
    }
}
