using Amazon.CDK;
using Constructs;

namespace StepGoInfra
{
    /// <summary>
    /// apps/teacher-portal (Blazor WASM standalone) — S3 + CloudFront static hosting.
    /// Resource definitions land in task 8.3 per design.md decision 11.
    /// </summary>
    public class TeacherPortalStack : Stack
    {
        internal TeacherPortalStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
        }
    }
}
