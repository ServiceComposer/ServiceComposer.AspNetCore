#nullable enable
using System;

namespace ServiceComposer.AspNetCore;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ModelAttribute<T>(ModelBindingSource bindingSource = ModelBindingSource.ModelBinding) : Attribute
{
    public ModelBindingSource Source { get; set; } = bindingSource;
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