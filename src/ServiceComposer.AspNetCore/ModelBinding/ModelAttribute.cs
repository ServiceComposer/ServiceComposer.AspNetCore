#nullable enable
using System;

namespace ServiceComposer.AspNetCore;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ModelAttribute(Type type, ModelBindingSource bindingSource = ModelBindingSource.ModelBinding) : Attribute
{
    public ModelBindingSource Source { get; } = bindingSource;
    public Type Type { get; } = type;
    //public int Order { get; set; }
}

public enum ModelBindingSource
{
    ModelBinding = 0,
    Query,
    Route,
    Body,
    Form,
    Headers
}