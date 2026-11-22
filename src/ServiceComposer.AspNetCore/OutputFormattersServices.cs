#nullable enable
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore;

static class OutputFormattersServices
{
    // Output formatters write responses through an MVC ObjectResult, whose executor
    // only exists when MVC services are registered. Without the check, the first
    // request fails instead of the application at startup.
    internal static void EnsureRegistered(IServiceProvider serviceProvider, string setting)
    {
        var isService = serviceProvider.GetService<IServiceProviderIsService>();
        if (isService == null || isService.IsService(typeof(IActionResultExecutor<ObjectResult>)))
        {
            return;
        }

        throw new InvalidOperationException(
            $"{setting} is set to true, but the MVC services output formatters need aren't registered. " +
            "Call services.AddControllers(), services.AddControllersWithViews(), services.AddMvc(), " +
            "or services.AddRazorPages(), or set it to false.");
    }
}
