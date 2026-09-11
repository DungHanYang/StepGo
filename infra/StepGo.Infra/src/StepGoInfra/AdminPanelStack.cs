using Amazon.CDK;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudFront.Origins;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.WAFv2;
using Constructs;

namespace StepGoInfra
{
    /// <summary>
    /// apps/admin-panel (Blazor WASM standalone) — S3 + CloudFront static hosting, plus an
    /// AWS WAF IP allow list (design.md decision 2: "管理者後台需獨立網域、IP 白名單...可用
    /// CloudFront 前的 AWS WAF IP set 落實"). The IP set starts empty — real allowed IPs are
    /// an operational concern set at deploy time, not a frontend-scaffold decision.
    ///
    /// NOTE: a WebACL associated with CloudFront must live in us-east-1 regardless of where
    /// the rest of the stack deploys; when this stack is actually deployed (not just
    /// synthesized), pass <c>Env = new Environment { Region = "us-east-1" }</c> in its props.
    /// </summary>
    public class AdminPanelStack : Stack
    {
        internal AdminPanelStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var siteBucket = new Bucket(this, "AdminPanelSiteBucket", new BucketProps
            {
                RemovalPolicy = RemovalPolicy.DESTROY,
                AutoDeleteObjects = true,
                BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            });

            var ipSet = new CfnIPSet(this, "AdminIpAllowList", new CfnIPSetProps
            {
                Name = "StepGoAdminIpAllowList",
                Scope = "CLOUDFRONT",
                IpAddressVersion = "IPV4",
                Addresses = [],
            });

            var webAcl = new CfnWebACL(this, "AdminWebAcl", new CfnWebACLProps
            {
                Scope = "CLOUDFRONT",
                DefaultAction = new CfnWebACL.DefaultActionProperty { Block = new CfnWebACL.BlockActionProperty() },
                VisibilityConfig = new CfnWebACL.VisibilityConfigProperty
                {
                    SampledRequestsEnabled = true,
                    CloudWatchMetricsEnabled = true,
                    MetricName = "StepGoAdminWebAcl",
                },
                Rules = new object[]
                {
                    new CfnWebACL.RuleProperty
                    {
                        Name = "AllowFromIpAllowList",
                        Priority = 0,
                        Action = new CfnWebACL.RuleActionProperty { Allow = new CfnWebACL.AllowActionProperty() },
                        Statement = new CfnWebACL.StatementProperty
                        {
                            IpSetReferenceStatement = new CfnWebACL.IPSetReferenceStatementProperty
                            {
                                Arn = ipSet.AttrArn,
                            },
                        },
                        VisibilityConfig = new CfnWebACL.VisibilityConfigProperty
                        {
                            SampledRequestsEnabled = true,
                            CloudWatchMetricsEnabled = true,
                            MetricName = "StepGoAdminIpAllowListRule",
                        },
                    },
                },
            });

            _ = new Distribution(this, "AdminPanelDistribution", new DistributionProps
            {
                DefaultRootObject = "index.html",
                DefaultBehavior = new BehaviorOptions
                {
                    Origin = S3BucketOrigin.WithOriginAccessControl(siteBucket),
                    ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                },
                ErrorResponses =
                [
                    new ErrorResponse { HttpStatus = 403, ResponseHttpStatus = 200, ResponsePagePath = "/index.html" },
                    new ErrorResponse { HttpStatus = 404, ResponseHttpStatus = 200, ResponsePagePath = "/index.html" },
                ],
                WebAclId = webAcl.AttrArn,
            });
        }
    }
}
