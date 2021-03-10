using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.NetCore3x.Serialization
{
    public class UseOutputFormatters
    {
        // begin-snippet: net-core-3x-use-output-formatters
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
}