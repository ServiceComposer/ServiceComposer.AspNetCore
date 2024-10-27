using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.DefaultCasing
{
    public class Startup
    {
        // begin-snippet: default-casing
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddViewModelComposition(options =>
            {
                options.ResponseSerialization.DefaultResponseCasing = ResponseCasing.PascalCase;
            });
        }
        // end-snippet
    }
}