using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ServiceComposer.AspNetCore;

namespace Snippets.Serialization
{
    public class Startup
    {
        // begin-snippet: custom-serialization-settings
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddViewModelComposition(options =>
            {
                options.ResponseSerialization.UseCustomJsonSerializerSettings(request =>
                {
                    return new JsonSerializerSettings();
                });
            });
        }
        // end-snippet
    }
}