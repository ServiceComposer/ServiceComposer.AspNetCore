using System.Collections.Generic;
using System.Dynamic;
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

namespace ServiceComposer.AspNetCore.Tests;

public class When_using_model_binding
{
    class TestIntegerHandler : ICompositionRequestsHandler
    {
        class IntegerModel
        {
            public int ANumber { get; set; }
        }

        [HttpPost("/sample/{id}")]
        public async Task Handle(HttpRequest request)
        {
            var (model, _, modelState) = await request.TryBind<BodyRequest<IntegerModel>>();

            var vm = request.GetComposedResponseModel();
            vm.ANumber = model.Body.ANumber;
            vm.ANumberIsModelValid = modelState.IsValid;
        }
    }

    class TestStringHandler : ICompositionRequestsHandler
    {
        class StringModel
        {
            public string AString { get; set; }
        }

        [HttpPost("/sample/{id}")]
        public async Task Handle(HttpRequest request)
        {
            var (model, _, modelState) = await request.TryBind<BodyRequest<StringModel>>();

            var vm = request.GetComposedResponseModel();
            vm.AString = model.Body.AString;
            vm.AStringIsModelValid = modelState.IsValid;
        }
    }

    [Fact]
    public async Task Model_state_is_valid()
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
                    options.RegisterCompositionHandler<TestIntegerHandler>();
                    options.RegisterCompositionHandler<TestStringHandler>();
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
        Assert.True(responseObj?.SelectToken("AStringIsModelValid")?.Value<bool>());
        Assert.True(responseObj?.SelectToken("ANumberIsModelValid")?.Value<bool>());
    }
    
    class TestDictionaryHandler : ICompositionRequestsHandler
    {
        class DictionaryModel
        {
            public Dictionary<int, Model> Items { get; set; }
        }

        class Model
        {
            public string Value { get; set; }
        }

        [HttpPost("/sample/{id}")]
        public async Task Handle(HttpRequest request)
        {
            var (model, _, modelState) = await request.TryBind<BodyRequest<DictionaryModel>>();

            var vm = request.GetComposedResponseModel();
            vm.IsValid = modelState.IsValid;
            vm.ErrorCount = modelState.ErrorCount;
        }
    }
    
    [Fact]
    public async Task Model_state_report_error_count()
    {
        // Arrange
        var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services =>
            {
                services.AddViewModelComposition(options =>
                {
                    options.AssemblyScanner.Disable();
                    options.RegisterCompositionHandler<TestDictionaryHandler>();
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

        const string invalidJsonMissingClosingBracket = """
                            {
                                "1": {
                                    "Value":"this is the first value"
                                },
                                "2": {
                                    "Value":"this is the second value"
                                }
                            """;
        
        var stringContent = new StringContent(invalidJsonMissingClosingBracket, Encoding.UTF8, MediaTypeNames.Application.Json);
        stringContent.Headers.ContentLength = invalidJsonMissingClosingBracket.Length;

        // Act
        var response = await client.PostAsync("/sample/1", stringContent);

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        var responseString = await response.Content.ReadAsStringAsync();
        var responseObj = JObject.Parse(responseString);

        Assert.False(responseObj?.SelectToken("IsValid")?.Value<bool>());
        Assert.Equal(1, responseObj?.SelectToken("ErrorCount")?.Value<int>());
    }
}