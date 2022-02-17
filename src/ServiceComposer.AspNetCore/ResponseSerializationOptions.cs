using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ServiceComposer.AspNetCore
{
#if NETCOREAPP3_1 || NET5_0_OR_GREATER
    public class ResponseSerializationOptions
    {
        IServiceCollection services;
        private bool usingCustomJsonSerializerSettings;
        const ResponseCasing defaultCasing = ResponseCasing.CamelCase;

        internal void ValidateConfiguration(ILogger<ResponseSerializationOptions> logger)
        {
            if (UseOutputFormatters && (DefaultResponseCasing != defaultCasing || usingCustomJsonSerializerSettings))
            {
                logger.LogWarning($"ResponseSerialization {nameof(UseOutputFormatters)} is set to true, and it's also configured to use " +
                                  "either a custom response casing or custom json serializer settings. When using output formatters, custom " +
                                  "settings are ignored.");
            }
        }

        public ResponseSerializationOptions(IServiceCollection services)
        {
            this.services = services;
        }

        public ResponseCasing DefaultResponseCasing { get; set; } = defaultCasing;

        public void UseCustomJsonSerializerSettings(Func<HttpRequest, JsonSerializerSettings> jsonSerializerSettingsConfig)
        {
            usingCustomJsonSerializerSettings = true;
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