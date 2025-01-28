using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HandlersSyntaxPlayground.CompositionHandlers;

public class ContainerWithNestedClass
{
    public class NestedClassCompositionHandler
    {
        [ResponseCache(Duration = 10, Location = ResponseCacheLocation.Client), HttpPost("/sample/{v}")]
        public Task Post([FromRoute(Name = "v")] int id, int c)
        {
            return Task.CompletedTask;
        }
    }
}