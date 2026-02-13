using System;

namespace ServiceComposer.AspNetCore;

/// <summary>
/// Marks a class as a composition handler for contract-less composition.
/// Classes decorated with this attribute will be discovered by the source generator
/// and have wrapper classes generated for their HTTP-decorated methods.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class CompositionHandlerAttribute : Attribute
{
}
