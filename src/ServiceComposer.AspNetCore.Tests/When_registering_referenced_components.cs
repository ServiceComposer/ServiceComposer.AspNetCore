using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_registering_referenced_components
    {
        [Fact]
        public void The_providers_of_this_and_referenced_assemblies_are_registered()
        {
            var options = new ViewModelCompositionOptions(new ServiceCollection());

            options.RegisterReferencedComponents();

            var providerTypes = options.ComponentsProviders.Select(provider => provider.GetType()).ToList();
            Assert.Contains(typeof(TestClassLibraryWithHandlers.GeneratedCompositionComponentsProvider), providerTypes);
            Assert.Contains(typeof(GeneratedCompositionComponentsProvider), providerTypes);
        }
    }
}
