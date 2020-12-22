using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using TestClassLibraryWithHandlers;
using Xunit;

namespace ServiceComposer.AspNetCore.Endpoints.Tests
{
    public class When_using_assembly_scanner
    {
        [Fact]
        public async Task Matching_handlers_are_found()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_using_assembly_scanner>
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

                            if (type.IsNested && typeof(When_using_assembly_scanner).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public)
                                .Contains(type))
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
            var response = await client.GetAsync("/empty-response/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
        }

        private static bool _invoked = false;
        class Customizations : IViewModelCompositionOptionsCustomization
        {
            public void Customize(ViewModelCompositionOptions options)
            {
                _invoked = true;
            }
        }

        [Fact]
        public async Task Options_customization_are_invoked()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_using_assembly_scanner>
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

                            if (type.IsNested && typeof(When_using_assembly_scanner).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public)
                                .Contains(type))
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
            var response = await client.GetAsync("/empty-response/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(_invoked);
        }

        [Fact]
        public void ViewModel_preview_handlers_are_registered_automatically()
        {
            IEnumerable<IViewModelPreviewHandler> expectedPreviewHandlers = null;

            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_using_assembly_scanner>
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

                            if (type.IsNested && typeof(When_using_assembly_scanner).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public)
                                .Contains(type))
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

                    expectedPreviewHandlers = app.ApplicationServices.GetServices<IViewModelPreviewHandler>();
                }
            ).CreateClient();

            // Assert
            Assert.NotNull(expectedPreviewHandlers);
            Assert.True(expectedPreviewHandlers.Single().GetType() == typeof(TestPreviewHandler));
        }
    }
}