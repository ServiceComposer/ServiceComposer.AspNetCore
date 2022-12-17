using System;
using System.Linq;
using System.Reflection;

namespace ServiceComposer.AspNetCore.Tests;

public static class IsNestedTypeOfExtension
{
    public static bool IsNestedTypeOf<T>(this Type type) where T : class
    {
        return type.IsNested
               && typeof(T).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public)
                   .Contains(type);
    }
}