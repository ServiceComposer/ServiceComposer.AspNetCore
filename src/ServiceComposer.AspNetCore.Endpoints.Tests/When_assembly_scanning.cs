using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Endpoints.Tests
{
    public class When_assembly_scanning
    {
        class SampleNeverInvokedHandler : ICompositionRequestsHandler
        {
            [HttpGet("/this-doesnt-exist")]
            public Task Handle(HttpRequest request)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void Should_not_fail_due_to_invalid_assemblies()
        {
            // Arrange
            var factory = new SelfContainedWebApplicationFactoryWithWebHost<When_assembly_scanning>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.TypesFilter = type =>
                        {
                            if (type.Assembly.FullName.Contains("TestClassLibraryWithHandlers"))
                            {
                                return true;
                            }

                            if (type.IsNestedTypeOf<When_assembly_scanning>())
                            {
                                return true;
                            }

                            return false;
                        };
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            );
            factory.CreateClient();
        }

        [Fact]
        public void Without_any_filter_should_return_assemblies()
        {
            //arrange
            var scanner = new AssemblyScanner();

            //act
            var assemblies = scanner.Scan();

            Assert.NotEmpty(assemblies);
        }

        [Fact]
        public void With_exclude_all_filter_should_return_no_assemblies()
        {
            //arrange
            var scanner = new AssemblyScanner();
            scanner.AddAssemblyFilter(assemblyFullPath => AssemblyScanner.FilterResults.Exclude);
            //act
            var assemblies = scanner.Scan();

            Assert.Empty(assemblies);
        }

        [Fact]
        public void With_include_only_current_assembly_filter_should_return_1_assembly()
        {
            //arrange
            var currentAssemblyName = "ServiceComposer.AspNetCore.Endpoints.Tests.dll";
            var scanner = new AssemblyScanner();
            scanner.AddAssemblyFilter(assemblyFullPath =>
            {
                return assemblyFullPath.EndsWith(currentAssemblyName)
                    ? AssemblyScanner.FilterResults.Include
                    : AssemblyScanner.FilterResults.Exclude;
            });
            //act
            var assemblies = scanner.Scan();

            Assert.Single(assemblies);
        }

        [Fact]
        public void Should_not_register_duplicated_handlers()
        {
            IServiceProvider container = null;
            // Arrange
            var factory = new SelfContainedWebApplicationFactoryWithWebHost<When_assembly_scanning>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.TypesFilter = type =>
                        {
                            if (type.Assembly.FullName.Contains("TestClassLibraryWithHandlers"))
                            {
                                return true;
                            }

                            if (type.IsNestedTypeOf<When_assembly_scanning>())
                            {
                                return true;
                            }

                            return false;
                        };
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    container= app.ApplicationServices;
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            );
            factory.CreateClient();

            var handler = container.GetServices<SampleNeverInvokedHandler>()
                .SingleOrDefault(svc => svc is SampleNeverInvokedHandler);

            Assert.NotNull(handler);
        }

        [Fact]
        public async Task Should_return_success_code()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_assembly_scanning>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.TypesFilter = type =>
                        {
                            if (type.Assembly.FullName.Contains("TestClassLibraryWithHandlers"))
                            {
                                return true;
                            }

                            if (type.IsNestedTypeOf<When_assembly_scanning>())
                            {
                                return true;
                            }

                            return false;
                        };
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            // Act
            var response = await client.GetAsync("/matching-handlers/1");

            // Assert
            response.EnsureSuccessStatusCode();
        }
    }
}
