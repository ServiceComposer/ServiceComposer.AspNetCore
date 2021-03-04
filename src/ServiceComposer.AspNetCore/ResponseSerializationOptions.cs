using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace ServiceComposer.AspNetCore
{
#if NETCOREAPP3_1 || NET5_0
    public class ResponseSerializationOptions
    {
        private readonly IServiceCollection services;

        public ResponseSerializationOptions(IServiceCollection services)
        {
            this.services = services;
        }

        public ResponseCasing DefaultResponseCasing { get; set; } = ResponseCasing.CamelCase;

        public void UseCustomJsonSerializerSettings(Func<HttpRequest, JsonSerializerSettings> jsonSerializerSettingsConfig)
        {
            services.AddSingleton(jsonSerializerSettingsConfig);
        }
    }

    public enum ResponseCasing
    {
        CamelCase = 0,
        PascalCase = 1
    }
#endif
}