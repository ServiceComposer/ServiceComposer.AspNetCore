using System;
using System.Net.Http;

namespace ServiceComposer.AspNetCore.Tests.Utils;

public class DelegateHttpClientFactory : IHttpClientFactory
{
    private readonly Func<string, HttpClient> _httpClientProvider;

    public DelegateHttpClientFactory(Func<string, HttpClient> httpClientProvider)
    {
        _httpClientProvider = httpClientProvider;
    }

    public HttpClient CreateClient(string name)
    {
        return _httpClientProvider(name);
    }
}