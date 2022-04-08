using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace TestClassLibraryWithHandlers;

public class FakeConfig : IConfiguration
{
    public IEnumerable<IConfigurationSection> GetChildren()
    {
        throw new System.NotImplementedException();
    }

    public IChangeToken GetReloadToken()
    {
        throw new System.NotImplementedException();
    }

    public IConfigurationSection GetSection(string key)
    {
        throw new System.NotImplementedException();
    }

    public string this[string key]
    {
        get => throw new System.NotImplementedException();
        set => throw new System.NotImplementedException();
    }
}