using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServiceComposer.AspNetCore.SourceGeneration.Tests.TestFiles.CompositionHandlers;

[CompositionHandler]
class ClassWithInternalMethodCompositionHandler
{
    [Authorize]
    [HttpPost("/sample/{id}")]
    internal Task Post(int id, [FromForm(Name = "sampleC")]int c, [FromForm(Name = "sampleX")]string x)
    {
        return Task.CompletedTask;
    }
}