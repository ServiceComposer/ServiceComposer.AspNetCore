using PublicApiGenerator;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests.API
{
    public class APIApprovals
    {
        [Fact]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Task Approve_API()
        {
            var publicApi = typeof(ICompositionContext).Assembly.GeneratePublicApi(new ApiGeneratorOptions
            {
                ExcludeAttributes = new[] { "System.Runtime.Versioning.TargetFrameworkAttribute", "System.Reflection.AssemblyMetadataAttribute" }
            });

            return Verifier.Verify(publicApi);
        }
    }
}
