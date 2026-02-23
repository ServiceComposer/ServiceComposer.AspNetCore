using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceComposer.AspNetCore.Testing;
using ServiceComposer.AspNetCore.Tests.CompositionHandlers.Generated;
using ServiceComposer.AspNetCore.Tests.Utils;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests.ScatterGather;

public class When_configuring_from_configuration
{
    static HttpClient BuildDownstreamClient(string routeTemplate, object[] responseItems)
    {
        return new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services => services.AddRouting(),
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapGet(routeTemplate, () => responseItems);
                });
            }
        ).CreateClient();
    }

    static IConfiguration BuildConfiguration(params (string template, (string key, string dest)[] gatherers)[] routes)
    {
        var dict = new Dictionary<string, string>();
        for (var ri = 0; ri < routes.Length; ri++)
        {
            dict[$"Routes:{ri}:Template"] = routes[ri].template;
            for (var gi = 0; gi < routes[ri].gatherers.Length; gi++)
            {
                dict[$"Routes:{ri}:Gatherers:{gi}:Key"] = routes[ri].gatherers[gi].key;
                dict[$"Routes:{ri}:Gatherers:{gi}:DestinationUrl"] = routes[ri].gatherers[gi].dest;
            }
        }
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    static IConfiguration BuildConfigurationWithType(params (string template, (string key, string type, string dest)[] gatherers)[] routes)
    {
        var dict = new Dictionary<string, string>();
        for (var ri = 0; ri < routes.Length; ri++)
        {
            dict[$"Routes:{ri}:Template"] = routes[ri].template;
            for (var gi = 0; gi < routes[ri].gatherers.Length; gi++)
            {
                dict[$"Routes:{ri}:Gatherers:{gi}:Key"] = routes[ri].gatherers[gi].key;
                dict[$"Routes:{ri}:Gatherers:{gi}:Type"] = routes[ri].gatherers[gi].type;
                if (routes[ri].gatherers[gi].dest != null)
                    dict[$"Routes:{ri}:Gatherers:{gi}:DestinationUrl"] = routes[ri].gatherers[gi].dest;
            }
        }
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    class StaticDataGatherer(string key) : IGatherer
    {
        public string Key { get; } = key;

        public Task<IEnumerable<object>> Gather(HttpContext context)
        {
            var data = (IEnumerable<object>)new[] { new { Value = "FromStaticData" } };
            return Task.FromResult(data);
        }
    }

    [Fact]
    public void Missing_AddScatterGather_throws_meaningful_exception()
    {
        var config = BuildConfiguration(
            (template: "/items", gatherers: [("Source", "/upstream/source")]));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddRouting();
                    // AddScatterGather() intentionally omitted
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder =>
                    {
                        builder.MapScatterGather(config.GetSection("Routes"));
                    });
                }
            ).CreateClient());

        Assert.Contains("AddScatterGather", ex.Message);
    }
    
    [Fact]
    public void Configuration_with_http_gather_should_create_gatherer()
    {
        var config = BuildConfigurationWithType(
            (template: "/items", gatherers: [("Source", "http", "/upstream/source")]));

        _ = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services =>
            {
                services.AddRouting();
                services.AddScatterGather();
            },
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapScatterGather(config.GetSection("Routes"));
                });
            }
        ).CreateClient();
    }
    
    [Fact]
    public void Configuration_with_http_gather_should_throw_if_empty_destination_url()
    {
        var config = BuildConfigurationWithType(
            (template: "/items", gatherers: [("Source", "http", "")]));

        Assert.Throws<ArgumentException>(() =>
        {
            _ = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddRouting();
                    services.AddScatterGather();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder =>
                    {
                        builder.MapScatterGather(config.GetSection("Routes"));
                    });
                }
            ).CreateClient(); 
        });
    }

    [Fact]
    public void Unknown_gatherer_type_throws_meaningful_exception()
    {
        var config = BuildConfigurationWithType(
            (template: "/items", gatherers: [("Source", "UnknownType", null)]));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddRouting();
                    services.AddScatterGather(); // no factory registered for "UnknownType"
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder =>
                    {
                        builder.MapScatterGather(config.GetSection("Routes"));
                    });
                }
            ).CreateClient());

        Assert.Contains("UnknownType", ex.Message);
        Assert.Contains("AddScatterGather", ex.Message);
    }

    [Fact]
    public void Duplicate_gatherer_factory_type_throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddRouting();
                    services.AddScatterGather(config =>
                    {
                        config.AddGathererFactory("MyType", (_, _) => null);
                        config.AddGathererFactory("MyType", (_, _) => null); // duplicate
                    });
                },
                configure: app => app.UseRouting()
            ).CreateClient());

        Assert.Contains("MyType", ex.Message);
    }

    [Fact]
    public async Task Custom_gatherer_type_can_be_configured()
    {
        // Arrange
        var config = BuildConfigurationWithType(
            (template: "/items", gatherers: [("StaticSource", "StaticData", null)]));

        var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services =>
            {
                services.AddRouting();
                services.AddControllers();
                services.AddScatterGather(config=> config.AddGathererFactory("StaticData", (section, _) => new StaticDataGatherer(section["Key"])));
            },
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapScatterGather(config.GetSection("Routes"));
                });
            }
        ).CreateClient();

        // Act
        var response = await client.GetAsync("/items");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var array = JsonNode.Parse(body)!.AsArray();
        Assert.Single(array);
        Assert.Equal("FromStaticData", array[0]!["Value"]!.GetValue<string>());
    }

    [Fact]
    public async Task Routes_defined_in_configuration_are_mapped()
    {
        // Arrange
        var downstreamClient = BuildDownstreamClient("/upstream/source", new[] { new { Value = "FromConfig" } });

        var config = BuildConfiguration(
            (template: "/items", gatherers: [("Source", "/upstream/source")]));

        var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services =>
            {
                services.AddRouting();
                services.AddControllers();
                services.AddScatterGather();
                services.Replace(new ServiceDescriptor(typeof(IHttpClientFactory),
                    new DelegateHttpClientFactory(_ => downstreamClient)));
            },
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapScatterGather(config.GetSection("Routes"));
                });
            }
        ).CreateClient();

        // Act
        var response = await client.GetAsync("/items");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var array = JsonNode.Parse(body)!.AsArray();
        Assert.Equal("FromConfig", array[0]!["value"]!.GetValue<string>());
    }

    [Fact]
    public async Task Programmatic_routes_coexist_with_configuration_routes()
    {
        // Arrange
        var configSourceClient = BuildDownstreamClient("/upstream/config-source", new[] { new { Value = "FromConfig" } });
        var programmaticSourceClient = BuildDownstreamClient("/upstream/programmatic-source", new[] { new { Value = "FromCode" } });

        var config = BuildConfiguration(
            (template: "/items/config", gatherers: [("ConfigSource", "/upstream/config-source")]));

        var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services =>
            {
                services.AddRouting();
                services.AddControllers();
                services.AddScatterGather();
                HttpClient ClientProvider(string name) => name switch
                {
                    "ConfigSource" => configSourceClient,
                    "ProgrammaticSource" => programmaticSourceClient,
                    _ => throw new NotSupportedException($"No HTTP client registered for '{name}'")
                };
                services.Replace(new ServiceDescriptor(typeof(IHttpClientFactory),
                    new DelegateHttpClientFactory(ClientProvider)));
            },
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    // Routes from external configuration
                    builder.MapScatterGather(config.GetSection("Routes"));

                    // Additional route defined purely in code
                    builder.MapScatterGather("/items/programmatic", new ScatterGatherOptions
                    {
                        Gatherers = new List<IGatherer>
                        {
                            new HttpGatherer("ProgrammaticSource", "/upstream/programmatic-source")
                        }
                    });
                });
            }
        ).CreateClient();

        // Act
        var configResponse = await client.GetAsync("/items/config");
        var programmaticResponse = await client.GetAsync("/items/programmatic");

        // Assert
        Assert.True(configResponse.IsSuccessStatusCode);
        var configBody = await configResponse.Content.ReadAsStringAsync();
        var configArray = JsonNode.Parse(configBody)!.AsArray();
        Assert.Equal("FromConfig", configArray[0]!["value"]!.GetValue<string>());

        Assert.True(programmaticResponse.IsSuccessStatusCode);
        var programmaticBody = await programmaticResponse.Content.ReadAsStringAsync();
        var programmaticArray = JsonNode.Parse(programmaticBody)!.AsArray();
        Assert.Equal("FromCode", programmaticArray[0]!["value"]!.GetValue<string>());
    }

    [Fact]
    public async Task Configuration_routes_can_be_customized()
    {
        // Arrange - two downstream services; the config only knows about the first one,
        // the second is injected via the customize callback.
        var configSourceClient = BuildDownstreamClient("/upstream/config-source", new[] { new { Value = "FromConfig" } });
        var extraSourceClient = BuildDownstreamClient("/upstream/extra-source", new[] { new { Value = "FromCustomization" } });

        var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: (_,services) =>
            {
                services.AddRouting();
                services.AddControllers();
                services.AddScatterGather();
                HttpClient ClientProvider(string name) => name switch
                {
                    "ConfigSource" => configSourceClient,
                    "ExtraSource" => extraSourceClient,
                    _ => throw new NotSupportedException($"No HTTP client registered for '{name}'")
                };
                services.Replace(new ServiceDescriptor(typeof(IHttpClientFactory),
                    new DelegateHttpClientFactory(ClientProvider)));
            },
            configure: (ctx, app) =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapScatterGather(
                        ctx.Configuration.GetSection("Routes"),
                        customize: (template, options) =>
                        {
                            // Add an extra gatherer that is not in the config file
                            options.Gatherers.Add(new HttpGatherer("ExtraSource", "/upstream/extra-source"));
                        });
                });
            }
        )
        {
            BuilderCustomization = webHostBuilder =>
            {
                webHostBuilder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    var config = BuildConfiguration(
                        (template: "/items", gatherers: [("ConfigSource", "/upstream/config-source")]));
                    configurationBuilder.AddConfiguration(config);
                });
            }
        }.CreateClient();

        // Act
        var response = await client.GetAsync("/items");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var array = JsonNode.Parse(body)!.AsArray();
        var values = new HashSet<string>(array.Select(n => n!["value"]!.GetValue<string>()));
        Assert.Contains("FromConfig", values);
        Assert.Contains("FromCustomization", values);
        Assert.Equal(2, array.Count);
    }
}
