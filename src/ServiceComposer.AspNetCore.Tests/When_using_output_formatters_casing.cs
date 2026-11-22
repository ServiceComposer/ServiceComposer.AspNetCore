using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    // Verifies the casing configurations documented in output-formatters-serialization.md
    public class When_using_output_formatters_casing
    {
        public class TypedViewModel
        {
            public string SampleValue { get; set; }
        }

        class Handler : ICompositionRequestsHandler
        {
            [HttpGet("/dynamic/{id}")]
            [HttpGet("/typed/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.SampleValue = "sample";
                return Task.CompletedTask;
            }
        }

        class TypedFactory : IEndpointScopedViewModelFactory
        {
            [HttpGet("/typed/{id}")]
            public object CreateViewModel(HttpContext httpContext, ICompositionContext compositionContext) => new TypedViewModel();
        }

        static async Task<JsonObject> Get(Action<IServiceCollection> addMvc, string path, string acceptCasing = null)
        {
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<Handler>();
                        options.RegisterEndpointScopedViewModelFactory<TypedFactory>();
                        options.ResponseSerialization.UseOutputFormatters = true;
                    });
                    services.AddRouting();
                    addMvc(services);
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, path);
            if (acceptCasing != null)
            {
                request.Headers.Add("Accept-Casing", acceptCasing);
            }

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        }

        [Theory]
        [InlineData("/typed/1", "sampleValue")]
        [InlineData("/dynamic/1", "SampleValue")]
        public async Task System_Text_Json_camel_cases_only_typed_view_models_by_default(string path, string expectedName)
        {
            var response = await Get(services => services.AddControllers(), path);

            Assert.True(response.ContainsKey(expectedName), response.ToJsonString());
        }

        [Theory]
        [InlineData("/typed/1")]
        [InlineData("/dynamic/1")]
        public async Task System_Text_Json_camel_cases_dynamic_view_models_with_a_dictionary_key_policy(string path)
        {
            var response = await Get(services => services.AddControllers()
                .AddJsonOptions(options => options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase), path);

            Assert.True(response.ContainsKey("sampleValue"), response.ToJsonString());
        }

        [Theory]
        [InlineData("/typed/1")]
        [InlineData("/dynamic/1")]
        public async Task System_Text_Json_pascal_cases_without_a_naming_policy(string path)
        {
            var response = await Get(services => services.AddControllers()
                .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null), path);

            Assert.True(response.ContainsKey("SampleValue"), response.ToJsonString());
        }

        [Theory]
        [InlineData("/typed/1", "sampleValue")]
        [InlineData("/dynamic/1", "SampleValue")]
        public async Task Newtonsoft_camel_cases_only_typed_view_models_by_default(string path, string expectedName)
        {
            var response = await Get(services => services.AddControllers().AddNewtonsoftJson(), path);

            Assert.True(response.ContainsKey(expectedName), response.ToJsonString());
        }

        [Theory]
        [InlineData("/typed/1")]
        [InlineData("/dynamic/1")]
        public async Task Newtonsoft_camel_cases_dynamic_view_models_processing_dictionary_keys(string path)
        {
            var response = await Get(services => services.AddControllers().AddNewtonsoftJson(options =>
                options.SerializerSettings.ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = true }
                }), path);

            Assert.True(response.ContainsKey("sampleValue"), response.ToJsonString());
        }

        [Theory]
        [InlineData("/typed/1")]
        [InlineData("/dynamic/1")]
        public async Task Newtonsoft_pascal_cases_with_the_default_contract_resolver(string path)
        {
            var response = await Get(services => services.AddControllers().AddNewtonsoftJson(options =>
                options.SerializerSettings.ContractResolver = new DefaultContractResolver()), path);

            Assert.True(response.ContainsKey("SampleValue"), response.ToJsonString());
        }

        [Fact]
        public async Task The_accept_casing_header_is_ignored()
        {
            var response = await Get(services => services.AddControllers(), "/typed/1", acceptCasing: "casing/pascal");

            Assert.True(response.ContainsKey("sampleValue"), response.ToJsonString());
        }
    }
}
