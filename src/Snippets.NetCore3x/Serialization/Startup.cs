using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ServiceComposer.AspNetCore;

namespace Snippets.NetCore3x.Serialization
{
    public class Startup
    {
        // begin-snippet: net-core-3x-custom-serialization-settings
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