#if NETCOREAPP3_1 || NET5_0
namespace ServiceComposer.AspNetCore
{
    public interface IEndpointScopedViewModelFactory : IViewModelFactory
    {
    }
}
#endif