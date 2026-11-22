#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace ServiceComposer.AspNetCore;

static class RouteTemplates
{
    // Combines the [Route] templates of a component class with the Http* templates of its
    // method, following the MVC rules for controllers: a method template starting with "/" or
    // "~/" is absolute, and isn't combined. Each class template gives one route per method template.
    internal static object[] CombineWithClassRoutes(object[] classAttributes, object[] methodAttributes)
    {
        // RouteAttribute requires a template
        var classTemplates = classAttributes.OfType<RouteAttribute>().Select(r => r.Template!).ToArray();
        if (classTemplates.Length == 0)
        {
            return methodAttributes;
        }

        return methodAttributes
            .SelectMany<object, object>(attribute => attribute is HttpMethodAttribute httpMethodAttribute && CanCombine(httpMethodAttribute)
                ? classTemplates.Select(classTemplate => WithTemplate(httpMethodAttribute, Combine(classTemplate, httpMethodAttribute.Template)))
                : [attribute])
            .ToArray();
    }

    // The template used to compose components into endpoints, without the leading "/" or "~/"
    internal static string? Normalize(string? template)
    {
        if (template == null)
        {
            return null;
        }

        return template.StartsWith("~/", StringComparison.Ordinal)
            ? template[2..]
            : template.TrimStart('/');
    }

    static string Combine(string classTemplate, string? methodTemplate)
    {
        if (string.IsNullOrEmpty(methodTemplate))
        {
            return classTemplate;
        }

        if (IsAbsolute(methodTemplate))
        {
            return methodTemplate;
        }

        return $"{classTemplate.TrimEnd('/')}/{methodTemplate}";
    }

    static bool IsAbsolute(string template) => template.StartsWith('/') || template.StartsWith("~/", StringComparison.Ordinal);

    // Only the built-in attributes, which composition endpoints are mapped from, can be recreated
    static bool CanCombine(HttpMethodAttribute attribute) => attribute is HttpGetAttribute or HttpPostAttribute
        or HttpPutAttribute or HttpPatchAttribute or HttpDeleteAttribute;

    static HttpMethodAttribute WithTemplate(HttpMethodAttribute attribute, string template)
    {
        HttpMethodAttribute combined = attribute switch
        {
            HttpGetAttribute => new HttpGetAttribute(template),
            HttpPostAttribute => new HttpPostAttribute(template),
            HttpPutAttribute => new HttpPutAttribute(template),
            HttpPatchAttribute => new HttpPatchAttribute(template),
            HttpDeleteAttribute => new HttpDeleteAttribute(template),
            _ => throw new ArgumentOutOfRangeException(nameof(attribute))
        };

        if (attribute.Name != null)
        {
            combined.Name = attribute.Name;
        }
        if (attribute is IRouteTemplateProvider { Order: { } order })
        {
            combined.Order = order;
        }

        return combined;
    }
}
