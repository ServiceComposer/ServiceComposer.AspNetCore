# Customizing ViewModel Composition options from dependent assemblies

Assemblies containing types participating in the composition process can customize the current application `ViewModelCompositionOptions` by defining a type that implements the `IViewModelCompositionOptionsCustomization` interface. At runtime, when the application starts, types implementing the `IViewModelCompositionOptionsCustomization` will be instantiated and the `Customize` method will be invoked.

> Note: Types implementing `IViewModelCompositionOptionsCustomization` are not managed by IoC container. Dependency injection is not available.

`ViewModelCompositionOptions` offers the ability to access the application `IConfiguration` instance. By default, the `ViewModelCompositionOptions.Configuration` property is null. If accessed it throws an `ArgumentException`. To enable configuration support, pass the `IConfiguration` instance when configuring ServiceComposer via the  `AddViewModelComposition` extension method.
