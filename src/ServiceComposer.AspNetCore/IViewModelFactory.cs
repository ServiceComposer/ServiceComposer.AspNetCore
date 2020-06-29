using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore
{
    public interface IViewModelFactory
    {
        IViewModel CreateViewModel(HttpRequest request);
    }
}