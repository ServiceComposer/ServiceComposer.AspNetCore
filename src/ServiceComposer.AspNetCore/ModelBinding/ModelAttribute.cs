#nullable enable
using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ServiceComposer.AspNetCore;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public abstract class ModelAttribute(Type type, BindingSource bindingSource) : Attribute
{
    public Type Type { get; } = type;
    public BindingSource BindingSource { get; } = bindingSource;
    public abstract string ModelName { get; }
    //public int Order { get; set; }
}

public sealed class BindModelFromBodyAttribute<T>()
    : ModelAttribute(typeof(T), BindingSource.Body)
{
    public override string ModelName { get; } = "model";
};

public sealed class BindModelFromRouteAttribute<T>(string routeValueKey)
    : ModelAttribute(typeof(T), BindingSource.Path)
{
    public override string ModelName { get; } = routeValueKey;
}