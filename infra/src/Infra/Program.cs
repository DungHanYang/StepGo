using Amazon.CDK;

namespace Infra
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            new BackendStack(app, "BackendStack", new StackProps
            {
                // Env is intentionally left unset for this scaffold — see design.md Open Questions on
                // the AWS deployment/trust-boundary decision that hasn't been made yet. A single
                // synthesized template can still be produced and reviewed without a target account.
            });
            app.Synth();
        }
    }
}
