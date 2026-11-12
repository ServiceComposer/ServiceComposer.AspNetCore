using System;
using System.Runtime.CompilerServices;

namespace ServiceComposer.AspNetCore.Tests.Utils
{
    static class DisableConfigurationReload
    {
        // Every test host is built with Host.CreateDefaultBuilder, which loads
        // appsettings.json with reload on change enabled and, on Linux, creates
        // an inotify instance per host. Test hosts are not disposed, so running
        // the suite can exceed the default limit of 128 inotify instances per
        // user and fail with an IOException. No test relies on configuration
        // reload, so disable it for the whole test process.
        [ModuleInitializer]
        internal static void Initialize()
        {
            Environment.SetEnvironmentVariable("DOTNET_hostBuilder__reloadConfigOnChange", "false");
        }
    }
}
