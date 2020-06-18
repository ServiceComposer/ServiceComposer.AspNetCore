namespace ServiceComposer.AspNetCore
{
    public interface IViewModelCompositionOptionsCustomization
    {
        void Customize(ViewModelCompositionOptions options);
    }
}