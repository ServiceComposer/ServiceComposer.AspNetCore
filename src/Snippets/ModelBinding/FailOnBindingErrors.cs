using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.ModelBinding;

static class FailOnBindingErrorsSnippets
{
    static void ShowConfiguration()
    {
        var builder = WebApplication.CreateBuilder();

        // begin-snippet: disable-fail-on-binding-errors
        builder.Services.AddViewModelComposition(options =>
        {
            options.ModelBinding.FailOnBindingErrors = false;
        });
        // end-snippet
    }
}
