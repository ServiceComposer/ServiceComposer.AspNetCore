using ApprovalTests;
using ApprovalTests.Reporters;
using PublicApiGenerator;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests.API
{
    public class APIApprovals
    {
        [Fact]
        [UseReporter(typeof(DiffReporter))]
        public void Approve_API()
        {
            var publicApi = ApiGenerator.GeneratePublicApi(typeof(IInterceptRoutes).Assembly);

            Approvals.Verify(publicApi);
        }
    }
}
