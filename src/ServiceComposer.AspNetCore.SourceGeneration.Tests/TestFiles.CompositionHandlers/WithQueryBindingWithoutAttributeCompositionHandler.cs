using Microsoft.AspNetCore.Mvc;

namespace ServiceComposer.AspNetCore.SourceGeneration.Tests.TestFiles.CompositionHandlers;

class WithQueryBindingWithoutAttributeCompositionHandler
{
    [ResponseCache(Duration = 10, Location = ResponseCacheLocation.Client), HttpPost("/sample/{v}")]
    public Task Post([FromRoute(Name = "v")]int id, int c, [FromQuery]string v, [FromBody]WithQueryBindingWithoutAttributeBodyClass body)
    {
        return Task.CompletedTask;
    }
}

public class WithQueryBindingWithoutAttributeBodyClass
{
    public string? S { get; set; }
}