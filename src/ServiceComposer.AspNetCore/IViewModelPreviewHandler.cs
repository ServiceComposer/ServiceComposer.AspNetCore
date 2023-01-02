using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore
{
    public interface IViewModelPreviewHandler
    {
        [Obsolete("Use Preview(HttpRequest request). Will be treated as an error starting v2 and removed in v3.", error: true)]
        Task Preview(dynamic viewModel)
        {
            throw new NotSupportedException();
        }

        [Obsolete("Use Preview(HttpRequest request). Will be treated as an error starting v2 and removed in v3.", error: true)]
        Task Preview(HttpRequest request, dynamic viewModel)
        {
            throw new NotSupportedException();
        }

        Task Preview(HttpRequest request);
    }
}