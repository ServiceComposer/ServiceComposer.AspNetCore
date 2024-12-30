#nullable enable
using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ServiceComposer.AspNetCore;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public abstract class BindModelAttribute(Type type, BindingSource bindingSource) : Attribute
{
    public Type Type { get; } = type;
    public BindingSource BindingSource { get; } = bindingSource;
    public abstract string ModelName { get; }
}

/// <summary>
/// Binds a model from multiple sources. Each model property can specify the source
/// to use using the various FromBody, FromForm, FromRoute, etc., attributes  
/// </summary>
/// <typeparam name="T">The type of the model to bind to</typeparam>
public sealed class BindAttribute<T>()
    : BindModelAttribute(typeof(T), BindingSource.ModelBinding)
{
    public override string ModelName { get; } = "";
}

/// <summary>
/// Binds a model from the request body payload
/// </summary>
/// <typeparam name="T">The type of the model to bind to</typeparam>
public sealed class BindFromBodyAttribute<T>()
    : BindModelAttribute(typeof(T), BindingSource.Body)
{
    public override string ModelName { get; } = "";
}

/// <summary>
/// Binds a model from the request path
/// </summary>
/// <typeparam name="T">The type of the model to bind to</typeparam>
public sealed class BindFromRouteAttribute<T>(string routeValueKey)
    : BindModelAttribute(typeof(T), BindingSource.Path)
{
    public override string ModelName { get; } = routeValueKey;
}

// TODO Add tests for this binding source
public sealed class BindFromQueryAttribute<T>(string queryParameterName)
    : BindModelAttribute(typeof(T), BindingSource.Query)
{
    public override string ModelName { get; } = queryParameterName;
}

public sealed class BindFromFormAttribute<T>(string? formFieldName = null)
    : BindModelAttribute(typeof(T), BindingSource.Form)
{
    public override string ModelName { get; } = formFieldName ?? "";
}

// TODO Add tests for this binding source
public sealed class BindFromHeaderAttribute<T>(string headerName)
    : BindModelAttribute(typeof(T), BindingSource.Header)
{
    public override string ModelName { get; } = headerName;
}