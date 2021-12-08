using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using PublicApiGenerator;
using System.Runtime.CompilerServices;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests.API
{
    public class APIApprovals
    {
        [Fact]
        [UseReporter(typeof(DiffReporter))]
        [MethodImpl(MethodImplOptions.NoInlining)]
#if NETCOREAPP3_1 || NET5_0
        [UseApprovalSubdirectory("NET")]
#endif
#if NETCOREAPP2_1
        [UseApprovalSubdirectory("NETCOREAPP2_1")]
#endif
        public void Approve_API()
        {
            var publicApi = typeof(IInterceptRoutes).Assembly.GeneratePublicApi(new ApiGeneratorOptions
            {
                ExcludeAttributes = new[] { "System.Runtime.Versioning.TargetFrameworkAttribute", "System.Reflection.AssemblyMetadataAttribute" }
            });

            Approvals.Verify(publicApi);
        }
    }
}
