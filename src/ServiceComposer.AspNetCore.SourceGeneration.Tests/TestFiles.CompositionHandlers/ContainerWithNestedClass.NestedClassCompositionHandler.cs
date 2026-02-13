using Microsoft.AspNetCore.Mvc;

namespace ServiceComposer.AspNetCore.SourceGeneration.Tests.TestFiles.CompositionHandlers;

public class ContainerWithNestedClass
{
    [CompositionHandler]
    public class NestedClassCompositionHandler
    {
        [ResponseCache(Duration = 10, Location = ResponseCacheLocation.Client), HttpPost("/sample/{v}")]
        public Task Post([FromRoute(Name = "v")] int id, int c)
        {
            return Task.CompletedTask;
        }
    }
}