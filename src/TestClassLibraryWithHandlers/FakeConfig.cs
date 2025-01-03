using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace TestClassLibraryWithHandlers;

public class FakeConfig : IConfiguration
{
    public IEnumerable<IConfigurationSection> GetChildren()
    {
        yield return null;
    }

    public IChangeToken GetReloadToken() => null;

    public IConfigurationSection GetSection(string key) => null;

    public string this[string key]
    {
        get => null;
        set { /* NOP */ }
    }
}