using Microsoft.AspNetCore.Mvc;

namespace HandlersSyntaxPlayground.CompositionHandlers;

public class MySampleCompositionHandler
{
    [HttpPost("/sample/{id}?val={v}")]
    public Task Post(int id, string v, [FromBody]BodyClass body)
    {
        return Task.CompletedTask;
    }
}

public class BodyClass
{
    public string S { get; set; }
}