using System;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore.Tests
{
    public class TestVariant
    {
        public string Description { get; set; }
        public Action<IServiceCollection> ConfigureServices { get; set; }
        public Action<IApplicationBuilder> Configure { get; set; }
        public Action<ViewModelCompositionOptions> CompositionOptions { get; set; }

        public Action<HttpClient> ConfigureHttpClient { get; set; }

        public override string ToString() => Description;
    }
}