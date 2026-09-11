using Amazon.CDK;
using Constructs;

namespace StepGoInfra
{
    /// <summary>
    /// apps/marketing (Blazor Web App, Static SSR) — API Gateway + Lambda.
    /// Resource definitions land in task 8.3 per design.md decision 11.
    /// </summary>
    public class MarketingStack : Stack
    {
        internal MarketingStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
        }
    }
}
