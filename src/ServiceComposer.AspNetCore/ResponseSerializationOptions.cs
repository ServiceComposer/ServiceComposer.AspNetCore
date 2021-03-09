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

        /// <summary>
        /// Configures ServiceComposer to use the MVC defined output formatters.
        /// To use output formatters MVC service must be configured either by calling
        /// services.AddControllers(), or services.AddControllersAndViews(), or
        /// services.AddMvc(), or services.AddAddRazorPages().
        /// Default value if <c>false</c>.
        /// </summary>
        public bool UseOutputFormatters { get; set; }
    }

    public enum ResponseCasing
    {
        CamelCase = 0,
        PascalCase = 1
    }
#endif
}