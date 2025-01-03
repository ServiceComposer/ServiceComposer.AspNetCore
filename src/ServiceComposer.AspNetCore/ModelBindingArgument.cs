#nullable enable
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ServiceComposer.AspNetCore;

public class ModelBindingArgument(string name, object? value, BindingSource bindingSource)
{
    public string Name { get; } = name;
    public object? Value { get; } = value;
    public BindingSource BindingSource { get; } = bindingSource;
}