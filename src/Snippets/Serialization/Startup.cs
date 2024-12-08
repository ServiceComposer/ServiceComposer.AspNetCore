using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
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
                options.ResponseSerialization.UseCustomJsonSerializerSettings(_ =>
                {
                    return new JsonSerializerOptions()
                    {
                        // customize options as needed
                    };
                });
            });
        }
        // end-snippet
    }
}