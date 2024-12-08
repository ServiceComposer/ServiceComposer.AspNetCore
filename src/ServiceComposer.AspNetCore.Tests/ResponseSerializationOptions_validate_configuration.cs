using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MELT;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VerifyXunit;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class ResponseSerializationOptions_validate_configuration
    {
        [Fact]
        public async Task Logs_warning_when_using_invalid_config()
        {
            var loggerFactory = TestLoggerFactory.Create();
            var logger = loggerFactory.CreateLogger<ResponseSerializationOptions>();

            var options = new ResponseSerializationOptions(new ServiceCollection())
            {
                UseOutputFormatters = true,
                DefaultResponseCasing = ResponseCasing.PascalCase
            };
            options.UseCustomJsonSerializerSettings(request => new JsonSerializerOptions());
            options.ValidateConfiguration(logger);

            var log = Assert.Single(loggerFactory.Sink.LogEntries);
            Assert.Equal(LogLevel.Warning, log.LogLevel);
            
            await Verifier.Verify(log.Message);
        }

        [Fact]
        public async Task Logs_warning_when_using_invalid_config_with_casing_and_formatters()
        {
            var loggerFactory = TestLoggerFactory.Create();
            var logger = loggerFactory.CreateLogger<ResponseSerializationOptions>();

            var options = new ResponseSerializationOptions(new ServiceCollection())
            {
                UseOutputFormatters = true,
                DefaultResponseCasing = ResponseCasing.PascalCase
            };
            options.ValidateConfiguration(logger);

            var log = Assert.Single(loggerFactory.Sink.LogEntries);
            Assert.Equal(LogLevel.Warning, log.LogLevel);
            
            await Verifier.Verify(log.Message);
        }

        [Fact]
        public async Task Logs_warning_when_using_invalid_config_with_custom_settings_and_formatters()
        {
            var loggerFactory = TestLoggerFactory.Create();
            var logger = loggerFactory.CreateLogger<ResponseSerializationOptions>();

            var options = new ResponseSerializationOptions(new ServiceCollection())
            {
                UseOutputFormatters = true
            };
            options.UseCustomJsonSerializerSettings(request => new JsonSerializerOptions());
            options.ValidateConfiguration(logger);

            var log = Assert.Single(loggerFactory.Sink.LogEntries);
            Assert.Equal(LogLevel.Warning, log.LogLevel);
            
            await Verifier.Verify(log.Message);
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