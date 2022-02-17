#if NETCOREAPP3_1 || NET5_0_OR_GREATER
namespace ServiceComposer.AspNetCore
{
    public interface IEndpointScopedViewModelFactory : IViewModelFactory
    {
    }
}
#endif