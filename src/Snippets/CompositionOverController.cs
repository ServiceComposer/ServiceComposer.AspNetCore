using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets
{
    public class CompositionOverControllers
    {
        public CompositionOverControllers(IServiceCollection services)
        {
            // begin-snippet: enable-composition-over-controllers
            services.AddViewModelComposition(options =>
            {
                options.EnableCompositionOverControllers();
            });
            // end-snippet
        }
    }
}
