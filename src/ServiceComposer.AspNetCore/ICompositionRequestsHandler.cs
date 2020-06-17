using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore
{
    public interface ICompositionRequestsHandler
    {
        Task Handle(HttpRequest request);
    }
}