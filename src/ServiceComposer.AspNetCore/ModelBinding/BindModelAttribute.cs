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
}

public sealed class BindFromBodyAttribute<T>()
    : ModelAttribute(typeof(T), BindingSource.Body)
{
    public override string ModelName { get; } = "";
};

public sealed class BindFromRouteAttribute<T>(string routeValueKey)
    : ModelAttribute(typeof(T), BindingSource.Path)
{
    public override string ModelName { get; } = routeValueKey;
}

// TODO Add tests for this binding source
public sealed class BindFromQueryAttribute<T>(string queryParameterName)
    : ModelAttribute(typeof(T), BindingSource.Query)
{
    public override string ModelName { get; } = queryParameterName;
}

public sealed class BindFromFormAttribute<T>(string? formFieldName = null)
    : ModelAttribute(typeof(T), BindingSource.Form)
{
    public override string ModelName { get; } = formFieldName ?? "";
}

// TODO Add tests for this binding source
public sealed class BindFromHeaderAttribute<T>(string headerName)
    : ModelAttribute(typeof(T), BindingSource.Header)
{
    public override string ModelName { get; } = headerName;
}