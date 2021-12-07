using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.NetCore3x.ModelBinding
{
    public class ConfigureAppForModelBinding
    {
        // begin-snippet: model-binding-add-controllers
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddViewModelComposition();
            services.AddControllers();
        }
        // end-snippet
    }
}