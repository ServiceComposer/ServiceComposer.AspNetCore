using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using TestClassLibraryWithHandlers;
using Xunit;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace ServiceComposer.AspNetCore.Tests
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

                            if (type == typeof(CustomizationsThatAccessTheConfiguration))
                            {
                                return false;
                            }

                            if (type.IsNestedTypeOf<When_using_assembly_scanner>())
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

        static bool _invokedCustomizations = false;
        class EmptyCustomizations : IViewModelCompositionOptionsCustomization
        {
            public void Customize(ViewModelCompositionOptions options)
            {
                _invokedCustomizations = true;
            }
        }
        
        
        static IConfiguration _customizationsThatAccessTheConfigurationConfig;
        class CustomizationsThatAccessTheConfiguration : IViewModelCompositionOptionsCustomization
        {
            public void Customize(ViewModelCompositionOptions options)
            {
                _customizationsThatAccessTheConfigurationConfig = options.Configuration;
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

                            if (type == typeof(CustomizationsThatAccessTheConfiguration))
                            {
                                return false;
                            }

                            if (type.IsNestedTypeOf<When_using_assembly_scanner>())
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
            Assert.True(_invokedCustomizations);
        }
        
        [Fact]
        public void Options_customization_throws_if_configuration_is_not_available()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                _ = new SelfContainedWebApplicationFactoryWithWebHost<When_using_assembly_scanner>
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

                                if (type == typeof(EmptyCustomizations))
                                {
                                    return false;
                                }

                                if (type.IsNestedTypeOf<When_using_assembly_scanner>())
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
            });
        }
        
        [Fact]
        public void Options_customization_can_access_configuration_if_set()
        {
            // Arrange
            var config = new FakeConfig();
            var factory = new SelfContainedWebApplicationFactoryWithWebHost<When_using_assembly_scanner>
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

                            if (type == typeof(EmptyCustomizations))
                            {
                                return false;
                            }

                            if (type.IsNestedTypeOf<When_using_assembly_scanner>())
                            {
                                return true;
                            }

                            return false;
                        };
                    }, config);
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            );

            // Act
            var client = factory.CreateClient();
            
            // Assert
            Assert.Same(config, _customizationsThatAccessTheConfigurationConfig);
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

                            if (type == typeof(CustomizationsThatAccessTheConfiguration))
                            {
                                return false;
                            }

                            if (type.IsNestedTypeOf<When_using_assembly_scanner>())
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

        class TestEndpointScopedViewModelFactory : IEndpointScopedViewModelFactory
        {
            [HttpGet("/use-endpoint-scoped-factory/{id}")]
            public object CreateViewModel(HttpContext httpContext, ICompositionContext compositionContext)
            {
                return new TestModel();
            }
        }
        
        [Fact]
        public void Endpoint_scoped_factories_are_registered_automatically()
        {
            // Arrange
            TestEndpointScopedViewModelFactory expectedInstance = null;
            
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

                            if (type == typeof(CustomizationsThatAccessTheConfiguration))
                            {
                                return false;
                            }

                            if (type.IsNestedTypeOf<When_using_assembly_scanner>())
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

                    expectedInstance = app.ApplicationServices.GetService<TestEndpointScopedViewModelFactory>();
                }
            ).CreateClient();

            // Assert
            Assert.NotNull(expectedInstance);
        }
        
        [Fact]
        public async Task Endpoint_scoped_factories_is_used()
        {
            // Arrange
            var expectedValue = 1;
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_using_assembly_scanner>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.ResponseSerialization.DefaultResponseCasing = ResponseCasing.PascalCase;
                        options.TypesFilter = type =>
                        {
                            if (type.Assembly.FullName.Contains("TestClassLibraryWithHandlers"))
                            {
                                return true;
                            }

                            if (type == typeof(CustomizationsThatAccessTheConfiguration))
                            {
                                return false;
                            }

                            if (type.IsNestedTypeOf<When_using_assembly_scanner>())
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
            var response = await client.GetAsync($"/use-endpoint-scoped-factory/{expectedValue}");

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<TestModel>(responseString);
            
            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(expectedValue, responseObj.Value);
        }
    }
}