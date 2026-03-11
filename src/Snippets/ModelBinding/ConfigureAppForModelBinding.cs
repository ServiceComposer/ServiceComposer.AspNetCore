using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.ModelBinding;

static class ModelBindingConfigSnippets
{
    static void ShowConfiguration()
    {
        var builder = WebApplication.CreateBuilder();

        // begin-snippet: model-binding-add-controllers
        builder.Services.AddViewModelComposition();
        builder.Services.AddControllers();
        // end-snippet
    }
}
