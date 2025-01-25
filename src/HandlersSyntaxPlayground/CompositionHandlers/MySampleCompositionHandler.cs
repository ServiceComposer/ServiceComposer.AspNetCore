using Microsoft.AspNetCore.Mvc;

namespace HandlersSyntaxPlayground.CompositionHandlers;

public class MySampleCompositionHandler
{
    [HttpPost("/sample/{id}")]
    public Task Post(int id, [FromQuery]string v, [FromBody]BodyClass body)
    {
        return Task.CompletedTask;
    }
    
    [HttpPost("/sample/{id}")]
    public Task Post(int id, [FromQuery(Name = "x")]int renamed, [FromBody]BodyClass body)
    {
        return Task.CompletedTask;
    }
    
    [HttpPost("/sample/{v}")]
    public Task Post([FromRoute(Name = "v")]int id, int c)
    {
        return Task.CompletedTask;
    }
}

public class BodyClass
{
    public string S { get; set; }
}