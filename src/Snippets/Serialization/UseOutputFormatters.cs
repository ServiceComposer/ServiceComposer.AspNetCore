using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.Serialization
{
    public class UseOutputFormatters
    {
        // begin-snippet: use-output-formatters
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddViewModelComposition(options =>
            {
                options.ResponseSerialization.UseOutputFormatters = true;
            });
            services.AddControllers();
        }
        // end-snippet
    }

    public class UseNewtonsoftOutputFormatter
    {
        // begin-snippet: use-newtonsoft-output-formatters
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddViewModelComposition(options =>
            {
                options.ResponseSerialization.UseOutputFormatters = true;
            });
            services.AddControllers()
                .AddNewtonsoftJson();
        }
        // end-snippet
    }
}