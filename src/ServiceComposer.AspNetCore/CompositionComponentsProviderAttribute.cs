using System;

namespace ServiceComposer.AspNetCore;

/// <summary>
/// Identifies the <see cref="ICompositionComponentsProvider"/> that registers
/// the composition components defined in the assembly. The ServiceComposer
/// source generator adds it to every assembly for which it generates a provider.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public sealed class CompositionComponentsProviderAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="CompositionComponentsProviderAttribute"/>.
    /// </summary>
    /// <param name="providerType">The <see cref="ICompositionComponentsProvider"/> type.</param>
    public CompositionComponentsProviderAttribute(Type providerType)
    {
        ProviderType = providerType;
    }

    /// <summary>
    /// The <see cref="ICompositionComponentsProvider"/> type.
    /// </summary>
    public Type ProviderType { get; }
}
