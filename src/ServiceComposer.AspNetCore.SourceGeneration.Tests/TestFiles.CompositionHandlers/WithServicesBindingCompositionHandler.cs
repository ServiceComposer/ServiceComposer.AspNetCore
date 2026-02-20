using Microsoft.AspNetCore.Mvc;

namespace ServiceComposer.AspNetCore.SourceGeneration.Tests.TestFiles.CompositionHandlers;

public interface IMyService
{
    string GetValue();
}

[CompositionHandler]
class WithServicesBindingCompositionHandler
{
    [HttpGet("/sample/{id}")]
    public Task Get([FromRoute] int id, [FromServices] IMyService myService)
    {
        return Task.CompletedTask;
    }
}
