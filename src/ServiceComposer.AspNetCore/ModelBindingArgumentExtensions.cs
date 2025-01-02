#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ServiceComposer.AspNetCore;

public static class ModelBindingArgumentExtensions
{
    public static TArgument? Argument<TArgument>(this IList<ModelBindingArgument>? arguments)
    {
        var argumentValue = arguments?.Single().Value;
        if (argumentValue is TArgument argument)
        {
            return argument;
        }

        return default;
    }
    
    public static TArgument? Argument<TArgument>(this IList<ModelBindingArgument>? arguments, string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var argumentValue = arguments?.Single(a=>a.Name == name).Value;
        if (argumentValue is TArgument argument)
        {
            return argument;
        }

        return default;
    }
    
    public static TArgument? Argument<TArgument>(this IList<ModelBindingArgument>? arguments, BindingSource bindingSource)
    {
        ArgumentNullException.ThrowIfNull(bindingSource);

        var argumentValue = arguments?.Single(a=>a.BindingSource == bindingSource).Value;
        if (argumentValue is TArgument argument)
        {
            return argument;
        }

        return default;
    }
    
    public static TArgument? Argument<TArgument>(this IList<ModelBindingArgument>? arguments, string name, BindingSource bindingSource)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(bindingSource);
        
        var argumentValue = arguments?.Single(a=> a.Name == name && a.BindingSource == bindingSource).Value;
        if (argumentValue is TArgument argument)
        {
            return argument;
        }

        return default;
    }
}