using System.Linq;
using MELT;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class ResponseSerializationOptions_validate_configuration
    {
        [Fact]
        public void Logs_warning_when_using_invalid_config()
        {
            var expectedLogMessage = $"ResponseSerialization {nameof(ResponseSerializationOptions.UseOutputFormatters)} is set to true, " +
                                     $"and it's also configured to use either a custom response casing or custom json serializer settings. " +
                                     $"When using output formatters, custom settings are ignored.";

            var loggerFactory = TestLoggerFactory.Create();
            var logger = loggerFactory.CreateLogger<ResponseSerializationOptions>();

            var options = new ResponseSerializationOptions(new ServiceCollection())
            {
                UseOutputFormatters = true,
                DefaultResponseCasing = ResponseCasing.PascalCase
            };
            options.UseCustomJsonSerializerSettings(request => new JsonSerializerSettings());
            options.ValidateConfiguration(logger);

            var log = Assert.Single(loggerFactory.Sink.LogEntries);
            Assert.Equal(expectedLogMessage, log.Message);
            Assert.Equal(LogLevel.Warning, log.LogLevel);
        }

        [Fact]
        public void Logs_warning_when_using_invalid_config_with_casing_and_formatters()
        {
            var expectedLogMessage = $"ResponseSerialization {nameof(ResponseSerializationOptions.UseOutputFormatters)} is set to true, " +
                                     $"and it's also configured to use either a custom response casing or custom json serializer settings. " +
                                     $"When using output formatters, custom settings are ignored.";

            var loggerFactory = TestLoggerFactory.Create();
            var logger = loggerFactory.CreateLogger<ResponseSerializationOptions>();

            var options = new ResponseSerializationOptions(new ServiceCollection())
            {
                UseOutputFormatters = true,
                DefaultResponseCasing = ResponseCasing.PascalCase
            };
            options.ValidateConfiguration(logger);

            var log = Assert.Single(loggerFactory.Sink.LogEntries);
            Assert.Equal(expectedLogMessage, log.Message);
            Assert.Equal(LogLevel.Warning, log.LogLevel);
        }

        [Fact]
        public void Logs_warning_when_using_invalid_config_with_custom_settings_and_formatters()
        {
            var expectedLogMessage = $"ResponseSerialization {nameof(ResponseSerializationOptions.UseOutputFormatters)} is set to true, " +
                                     $"and it's also configured to use either a custom response casing or custom json serializer settings. " +
                                     $"When using output formatters, custom settings are ignored.";

            var loggerFactory = TestLoggerFactory.Create();
            var logger = loggerFactory.CreateLogger<ResponseSerializationOptions>();

            var options = new ResponseSerializationOptions(new ServiceCollection())
            {
                UseOutputFormatters = true
            };
            options.UseCustomJsonSerializerSettings(request => new JsonSerializerSettings());
            options.ValidateConfiguration(logger);

            var log = Assert.Single(loggerFactory.Sink.LogEntries);
            Assert.Equal(expectedLogMessage, log.Message);
            Assert.Equal(LogLevel.Warning, log.LogLevel);
        }

        [Fact]
        public void Logs_nothing_when_using_valid_custom_config()
        {
            var loggerFactory = TestLoggerFactory.Create();
            var logger = loggerFactory.CreateLogger<ResponseSerializationOptions>();

            var options = new ResponseSerializationOptions(new ServiceCollection())
            {
                DefaultResponseCasing = ResponseCasing.PascalCase
            };
            options.ValidateConfiguration(logger);

            Assert.Empty(loggerFactory.Sink.LogEntries.ToList());
        }

        [Fact]
        public void Logs_nothing_when_using_valid_config_with_formatters()
        {
            var loggerFactory = TestLoggerFactory.Create();
            var logger = loggerFactory.CreateLogger<ResponseSerializationOptions>();

            var options = new ResponseSerializationOptions(new ServiceCollection())
            {
                UseOutputFormatters = true
            };
            options.ValidateConfiguration(logger);

            Assert.Empty(loggerFactory.Sink.LogEntries.ToList());
        }
    }
}