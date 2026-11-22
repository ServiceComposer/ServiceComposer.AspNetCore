using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_using_minimal_apis_json_options
    {
        public enum SampleKind
        {
            First,
            Second
        }

        class TestHandler : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.SampleValue = "sample";
                vm.SampleKind = SampleKind.Second;
                return Task.CompletedTask;
            }
        }

        static async Task<JsonObject> Get(
            Action<Microsoft.AspNetCore.Http.Json.JsonOptions> configureJsonOptions,
            Action<ViewModelCompositionOptions> configureComposition = null,
            string acceptCasing = null)
        {
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestHandler>();
                        configureComposition?.Invoke(options);
                    });
                    services.AddRouting();
                    if (configureJsonOptions != null)
                    {
                        services.ConfigureHttpJsonOptions(configureJsonOptions);
                    }
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "/sample/1");
            if (acceptCasing != null)
            {
                request.Headers.Add("Accept-Casing", acceptCasing);
            }

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        }

        static void SnakeCaseAndStringEnums(Microsoft.AspNetCore.Http.Json.JsonOptions options)
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        [Fact]
        public async Task Defaults_to_camel_case()
        {
            var response = await Get(configureJsonOptions: null);

            Assert.Equal("sample", response["sampleValue"]!.GetValue<string>());
            Assert.Equal(1, response["sampleKind"]!.GetValue<int>());
        }

        [Fact]
        public async Task Applies_the_configured_options()
        {
            var response = await Get(SnakeCaseAndStringEnums);

            // The dynamic view model members follow the property naming policy
            Assert.Equal("sample", response["sample_value"]!.GetValue<string>());
            Assert.Equal("Second", response["sample_kind"]!.GetValue<string>());
        }

        [Fact]
        public async Task Applies_the_configured_dictionary_key_policy()
        {
            var response = await Get(options =>
            {
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.KebabCaseLower;
            });

            Assert.Equal("sample", response["sample-value"]!.GetValue<string>());
        }

        [Fact]
        public async Task Explicit_default_response_casing_replaces_only_the_naming_policy()
        {
            var response = await Get(SnakeCaseAndStringEnums,
                options => options.ResponseSerialization.DefaultResponseCasing = ResponseCasing.PascalCase);

            Assert.Equal("sample", response["SampleValue"]!.GetValue<string>());
            Assert.Equal("Second", response["SampleKind"]!.GetValue<string>());
        }

        [Fact]
        public async Task Accept_casing_header_replaces_only_the_naming_policy()
        {
            var response = await Get(SnakeCaseAndStringEnums, acceptCasing: "casing/camel");

            Assert.Equal("sample", response["sampleValue"]!.GetValue<string>());
            Assert.Equal("Second", response["sampleKind"]!.GetValue<string>());
        }

        [Fact]
        public async Task Custom_serializer_settings_replace_the_configured_options()
        {
            var response = await Get(SnakeCaseAndStringEnums,
                options => options.ResponseSerialization.UseCustomJsonSerializerSettings(_ => new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
                }));

            Assert.Equal("sample", response["sampleValue"]!.GetValue<string>());
            Assert.Equal(1, response["sampleKind"]!.GetValue<int>());
        }
    }
}
