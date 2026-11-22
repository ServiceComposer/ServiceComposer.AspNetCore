#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore;

static class ModelBindingProblem
{
    // MVC doesn't expose exception messages in problem details either
    const string DefaultErrorMessage = "The input was not valid.";

    internal static bool FailOnBindingErrors(HttpContext context)
    {
        return context.RequestServices.GetService<ViewModelCompositionOptions>()?.ModelBinding.FailOnBindingErrors ?? true;
    }

    // Several handlers can bind the same value, e.g. the request body: errors are merged
    internal static void AddErrors(ModelStateDictionary target, ModelStateDictionary source)
    {
        foreach (var (key, entry) in source)
        {
            foreach (var error in entry.Errors)
            {
                var message = GetErrorMessage(error);
                if (target.TryGetValue(key, out var existing) && existing.Errors.Any(e => GetErrorMessage(e) == message))
                {
                    continue;
                }

                target.TryAddModelError(key, message);
            }
        }
    }

    internal static async Task WriteAsync(HttpContext context, ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(kvp => kvp.Value is { Errors.Count: > 0 })
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(GetErrorMessage).ToArray(),
                StringComparer.Ordinal);

        var problem = new HttpValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
        };

        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        var problemDetailsService = context.RequestServices.GetService<IProblemDetailsService>();
        if (problemDetailsService != null
            && await problemDetailsService.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem }))
        {
            return;
        }

        await context.Response.WriteAsJsonAsync(problem, typeof(HttpValidationProblemDetails), options: null, contentType: "application/problem+json");
    }

    static string GetErrorMessage(ModelError error)
    {
        return string.IsNullOrEmpty(error.ErrorMessage) ? DefaultErrorMessage : error.ErrorMessage;
    }
}

// Lets endpoint filters return the binding errors response, e.g. on controller endpoints.
// Outer endpoint filters can read its status code, as for minimal APIs results.
sealed class ModelBindingProblemResult(ModelStateDictionary modelState) : IResult, IStatusCodeHttpResult
{
    public int? StatusCode => StatusCodes.Status400BadRequest;

    public Task ExecuteAsync(HttpContext httpContext) => ModelBindingProblem.WriteAsync(httpContext, modelState);
}
