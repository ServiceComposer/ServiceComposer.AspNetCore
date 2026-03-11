using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.DefaultCasing;

static class DefaultCasingSnippets
{
    static void ShowConfiguration()
    {
        var builder = WebApplication.CreateBuilder();

        // begin-snippet: default-casing
        builder.Services.AddRouting();
        builder.Services.AddViewModelComposition(options =>
        {
            options.ResponseSerialization.DefaultResponseCasing = ResponseCasing.PascalCase;
        });
        // end-snippet
    }
}
