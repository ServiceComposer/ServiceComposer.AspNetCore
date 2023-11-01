using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_raising_event_using_DynamicViewModel
    {
        class Logger : ILogger<DynamicViewModel>
        {
            public LogLevel Level { get; set; }
            public string Message { get; set; }
            
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                Level = logLevel;
                Message = state.ToString();
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                throw new NotImplementedException();
            }
        }
        
        [Fact]
        public async Task Should_log_warning()
        {
            // Arrange
            var logger = new Logger();
            dynamic sut = new DynamicViewModel(logger);

            Task Function() => sut.RaiseEvent(new object());
            await Assert.ThrowsAsync<NotSupportedException>(Function);

            Assert.Equal(LogLevel.Error, logger.Level);
            Assert.Equal("dynamic.RaiseEvent is obsolete. It'll be treated as an error starting v2 and removed in v3. Use HttpRequest.GetCompositionContext() to raise events.", logger.Message);
        }
    }
}
