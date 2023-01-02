using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ServiceComposer.AspNetCore;

namespace TestClassLibraryWithHandlers
{
    public class TestPreviewHandler : IViewModelPreviewHandler
    {
        public Task Preview(HttpRequest request)
        {
            return Task.CompletedTask;
        }
    }
}