using System;
using System.ComponentModel;

namespace ServiceComposer.AspNetCore
{
    /// <summary>
    /// Marks a composition handler generated for a contract-less composition handler, pointing to the user's class
    /// and method. Applied by the source generator.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class GeneratedCompositionHandlerAttribute(Type userType, string methodName) : Attribute
    {
        public Type UserType { get; } = userType;
        public string MethodName { get; } = methodName;
    }
}
