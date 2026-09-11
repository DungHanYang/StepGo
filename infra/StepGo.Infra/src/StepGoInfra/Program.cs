using Amazon.CDK;

namespace StepGoInfra
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();

            // Four independent deploy targets per design.md decision 2/11:
            // marketing (Lambda + API Gateway) and three static WASM portals (S3 + CloudFront).
            new MarketingStack(app, "StepGoMarketingStack");
            new TeacherPortalStack(app, "StepGoTeacherPortalStack");
            new StudentPortalStack(app, "StepGoStudentPortalStack");
            new AdminPanelStack(app, "StepGoAdminPanelStack");

            app.Synth();
        }
    }
}
