using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore
{
    public interface IViewModelPreviewHandler
    {
        Task Preview(HttpRequest request);
    }
}