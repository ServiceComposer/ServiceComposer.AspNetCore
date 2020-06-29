using System.Threading.Tasks;
using ServiceComposer.AspNetCore;

namespace TestClassLibraryWithHandlers
{
    public class TestPreviewHandler : IViewModelPreviewHandler
    {
        public Task Preview(dynamic viewModel)
        {
            return Task.CompletedTask;
        }
    }
}