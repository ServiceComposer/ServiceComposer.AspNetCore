﻿using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Endpoints.Tests
{
    public class Post_with_2_handlers
    {
        public static IEnumerable<object[]> Variants()
        {
            yield return new object[]
            {
                new TestVariant
                {
                    Description = "Read Json body",
                    CompositionOptions = options =>
                    {
                        options.RegisterCompositionHandler<TestStringHandler>();
                        options.RegisterCompositionHandler<TestIntegerHandler>();
                    },
                    ConfigureHttpClient = client => client.DefaultRequestHeaders.Add("Accept-Casing", "casing/pascal")
                }
            };
            yield return new object[]
            {
                new TestVariant
                {
                    Description = "Model binding",
                    CompositionOptions = options =>
                    {
                        options.RegisterCompositionHandler<TestIntegerHandler_USE_ModelBinding>();
                        options.RegisterCompositionHandler<TestStringHandler_USE_ModelBinding>();
                    },
                    ConfigureServices = services => services.AddControllers(),
                    ConfigureHttpClient = client => client.DefaultRequestHeaders.Add("Accept-Casing", "casing/pascal")
                }
            };
        }

        class TestIntegerHandler_USE_ModelBinding : ICompositionRequestsHandler
        {
            class IntegerModel
            {
                public int ANumber { get; set; }
            }

            [HttpPost("/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var model = await request.Bind<BodyRequest<IntegerModel>>();

                var vm = request.GetComposedResponseModel();
                vm.ANumber = model.Body.ANumber;
            }
        }

        class TestStringHandler_USE_ModelBinding : ICompositionRequestsHandler
        {
            class StringModel
            {
                public string AString { get; set; }
            }

            [HttpPost("/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var model = await request.Bind<BodyRequest<StringModel>>();

                var vm = request.GetComposedResponseModel();
                vm.AString = model.Body.AString;
            }
        }

        class TestIntegerHandler : ICompositionRequestsHandler
        {
            [HttpPost("/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                request.Body.Position = 0;
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                var content = JObject.Parse(body);

                var vm = request.GetComposedResponseModel();
                vm.ANumber = content?.SelectToken("ANumber")?.Value<int>();
            }
        }

        class TestStringHandler : ICompositionRequestsHandler
        {
            [HttpPost("/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                request.Body.Position = 0;
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true );
                var body = await reader.ReadToEndAsync();
                var content = JObject.Parse(body);

                var vm = request.GetComposedResponseModel();
                vm.AString = content?.SelectToken("AString")?.Value<string>();
            }
        }

        [Theory]
        [MemberData(nameof(Variants))]
        public async Task Returns_expected_response(TestVariant variant)
        {
            // Arrange
            var expectedString = "this is a string value";
            var expectedNumber = 32;

            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.EnableWriteSupport();

                        variant.CompositionOptions?.Invoke(options);
                    });
                    services.AddRouting();

                    variant.ConfigureServices?.Invoke(services);
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());

                    variant.Configure?.Invoke(app);
                }
            ).CreateClient();

            variant.ConfigureHttpClient?.Invoke(client);

            dynamic model = new ExpandoObject();
            model.AString = expectedString;
            model.ANumber = expectedNumber;

            var json = (string) JsonConvert.SerializeObject(model);
            var stringContent = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            stringContent.Headers.ContentLength = json.Length;

            // Act
            var response = await client.PostAsync("/sample/1", stringContent);

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JObject.Parse(responseString);

            Assert.Equal(expectedString, responseObj?.SelectToken("AString")?.Value<string>());
            Assert.Equal(expectedNumber, responseObj?.SelectToken("ANumber")?.Value<int>());
        }
    }
}