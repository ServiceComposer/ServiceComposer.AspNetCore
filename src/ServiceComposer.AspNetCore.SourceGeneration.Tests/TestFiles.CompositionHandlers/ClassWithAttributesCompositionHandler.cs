using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServiceComposer.AspNetCore.SourceGeneration.Tests.TestFiles.CompositionHandlers;

[Authorize(Policy = "SomePolicy")]
[CompositionHandler]
class ClassWithAttributesCompositionHandler
{
    [AllowAnonymous]
    [HttpGet("/sample/{id}")]
    public Task Get(int id)
    {
        return Task.CompletedTask;
    }
}
