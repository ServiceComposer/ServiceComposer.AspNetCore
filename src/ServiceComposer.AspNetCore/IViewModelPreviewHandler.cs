#if NETCOREAPP3_1 || NET5_0 

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore
{
    public interface IViewModelPreviewHandler
    {
        [Obsolete("Use Preview(HttpRequest request, dynamic viewModel). Will be removed in v2.")]
        Task Preview(dynamic viewModel);
        Task Preview(HttpRequest request, dynamic viewModel)
        {
            return Preview(viewModel);
        }
    }
}

#endif