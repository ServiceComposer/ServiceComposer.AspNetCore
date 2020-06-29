#if NETCOREAPP3_1

using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore
{
    public interface IViewModelVisitor
    {
        Task Visit(dynamic viewModel);
    }
}

#endif