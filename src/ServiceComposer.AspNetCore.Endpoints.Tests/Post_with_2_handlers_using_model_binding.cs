﻿using System.Net.Http;
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
    public class Post_with_2_handlers_using_model_binding
    {
        class ClientRequestModel
        {
            public string AString { get; set; }
            public int ANumber { get; set; }
        }

        class IntegerRequest
        {
            [FromBody] public IntegerModel Body { get; set; }
        }

        class StringRequest
        {
            [FromBody] public StringModel Body { get; set; }
        }

        class IntegerModel
        {
            public int ANumber { get; set; }
        }

        class StringModel
        {
            public string AString { get; set; }
        }

        class TestIntegerHandler : ICompositionRequestsHandler
        {
            [HttpPost("/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var model = await request.Bind<IntegerRequest>();

                var vm = request.GetComposedResponseModel();
                vm.ANumber = model.Body.ANumber;
            }
        }


        class TestStringHandler : ICompositionRequestsHandler
        {
            [HttpPost("/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var model = await request.Bind<StringRequest>();

                var vm = request.GetComposedResponseModel();
                vm.AString = model.Body.AString;
            }
        }

        [Fact]
        public async Task Returns_expected_response()
        {
            // Arrange
            var expectedString = "this is a string value";
            var expectedNumber = 32;

            var client = new SelfContainedWebApplicationFactoryWithWebHost<Post_with_2_handlers>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestStringHandler>();
                        options.RegisterCompositionHandler<TestIntegerHandler>();
                        options.EnableWriteSupport();
                    });
                    services.AddRouting();
                    services.AddControllers();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            client.DefaultRequestHeaders.Add("Accept-Casing", "casing/pascal");
            var json = JsonConvert.SerializeObject(new ClientRequestModel
            {
                AString = expectedString,
                ANumber = expectedNumber
            });
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