using ApprovalTests;
using ApprovalTests.Reporters;
using PublicApiGenerator;
using System.Runtime.CompilerServices;
using ApprovalTests.Namers;
using Xunit;

namespace ServiceComposer.AspNetCore.Endpoints.Tests.API
{
    public class APIApprovals
    {
        [Fact]
        [UseReporter(typeof(DiffReporter))]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Approve_API()
        {
            var publicApi = typeof(ICompositionContext).Assembly.GeneratePublicApi(new ApiGeneratorOptions
            {
                ExcludeAttributes = new[] { "System.Runtime.Versioning.TargetFrameworkAttribute", "System.Reflection.AssemblyMetadataAttribute" }
            });

            Approvals.Verify(publicApi);
        }
    }
}
