using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServiceComposer.AspNetCore.SourceGeneration.Tests.TestFiles.CompositionHandlers;

[CompositionHandler]
class AHandlerWithANestedClassCompositionHandler
{
    public class BodyClass
    {
        public string? S { get; set; }
    }
    
    [HttpPost("/sample/{id}"), Authorize]
    public Task Post(int id, [FromQuery(Name = "x")]int renamed, [FromBody]BodyClass body)
    {
        return Task.CompletedTask;
    }
}