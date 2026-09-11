using Amazon.CDK;
using Constructs;

namespace StepGoInfra
{
    /// <summary>
    /// apps/admin-panel (Blazor WASM standalone) — S3 + CloudFront static hosting,
    /// plus an AWS WAF IP allow list per design.md decision 2/11.
    /// Resource definitions land in task 8.3.
    /// </summary>
    public class AdminPanelStack : Stack
    {
        internal AdminPanelStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
        }
    }
}
