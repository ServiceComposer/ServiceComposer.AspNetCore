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

/// <summary>
/// Binds a model from the request query string.
/// </summary>
/// <param name="queryParameterName">The query string parameter name</param>
/// <typeparam name="T"></typeparam>
public sealed class BindFromQueryAttribute<T>(string queryParameterName)
    : BindModelAttribute(typeof(T), BindingSource.Query)
{
    public override string ModelName { get; } = queryParameterName;
}

/// <summary>
/// Binds a model from the form fields collection
/// </summary>
/// <param name="formFieldName">The optional form field name;
/// When omitted it's expected the bound type is <c>IFormCollection</c></param>
/// <typeparam name="T"></typeparam>
public sealed class BindFromFormAttribute<T>(string? formFieldName = null)
    : BindModelAttribute(typeof(T), BindingSource.Form)
{
    public override string ModelName { get; } = formFieldName ?? "";
}

/// <summary>
/// Binds a model from the dependency injection services container
/// </summary>
/// <param name="parameterName">The parameter name used to identify this service argument</param>
/// <typeparam name="T">The type of the service to resolve</typeparam>
public sealed class BindFromServicesAttribute<T>(string parameterName)
    : BindModelAttribute(typeof(T), BindingSource.Services)
{
    public override string ModelName { get; } = parameterName;
}