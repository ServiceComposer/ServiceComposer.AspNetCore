using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore
{
    public interface ICompositionErrorsHandler
    {
        Task OnRequestError( HttpRequest request, Exception ex);
    }
}