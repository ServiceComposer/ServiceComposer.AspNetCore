#if NETCOREAPP3_1

using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore
{
    public interface IViewModelPreviewHandler
    {
        Task Preview(dynamic viewModel);
    }
}

#endif