using Amazon.CDK;
using Constructs;

namespace StepGoInfra
{
    /// <summary>
    /// apps/student-portal (Blazor WASM standalone) — S3 + CloudFront static hosting.
    /// Resource definitions land in task 8.3 per design.md decision 11.
    /// </summary>
    public class StudentPortalStack : Stack
    {
        internal StudentPortalStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
        }
    }
}
