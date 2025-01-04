using Microsoft.AspNetCore.Mvc;

namespace HandlersSyntaxPlayground.CompositionHandlers;

public class MySampleCompositionHandler
{
    [HttpPost("/sample/{id}")]
    public Task Post(int id)
    {
        return Task.CompletedTask;
    }
}