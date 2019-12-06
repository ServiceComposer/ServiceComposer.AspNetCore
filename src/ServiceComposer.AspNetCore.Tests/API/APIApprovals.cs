using ApprovalTests;
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
        public void Approve_API()
        {
            var publicApi = typeof(IInterceptRoutes).Assembly.GeneratePublicApi();

            Approvals.Verify(publicApi);
        }
    }
}
