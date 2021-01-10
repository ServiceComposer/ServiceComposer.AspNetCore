using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.NetCore3x.DefaultCasing
{
    public class Startup
    {
        // begin-snippet: net-core-3x-default-casing
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddViewModelComposition(options =>
            {
                options.ResponseSerialization.DefaultResponseCasing = ResponseCasing.PascalCase;
            });
            // end-snippet
        }
    }
}