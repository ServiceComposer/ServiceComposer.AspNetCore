using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.ViewModelPreview;

// begin-snippet: view-model-preview-handler
public class RequestInfoPreviewHandler : IViewModelPreviewHandler
{
    public Task Preview(HttpRequest request)
    {
        var vm = request.GetComposedResponseModel();
        vm.RequestId = request.GetCompositionContext().RequestId;

        return Task.CompletedTask;
    }
}
// end-snippet

static class ViewModelPreviewRegistration
{
    static void ShowConfiguration()
    {
        var builder = WebApplication.CreateBuilder();

        // begin-snippet: view-model-preview-handler-registration
        builder.Services.AddViewModelComposition(options =>
        {
            options.AssemblyScanner.Disable();
        });
        builder.Services.AddTransient<IViewModelPreviewHandler, RequestInfoPreviewHandler>();
        // end-snippet
    }
}
