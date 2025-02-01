﻿using Microsoft.AspNetCore.Mvc;

namespace HandlersSyntaxPlayground.CompositionHandlers;

class MySampleCompositionHandler
{
    public class BodyClass
    {
        public string? S { get; set; }
    }
    
    [HttpPost("/sample/{id}")]
    public Task Post(int id, [FromQuery]string v, [FromBody]BodyClass body)
    {
        return Task.CompletedTask;
    }

    [ResponseCache(Duration = 10, Location = ResponseCacheLocation.Client), HttpPost("/sample/{v}")]
    public Task Post([FromRoute(Name = "v")]int id, int c)
    {
        return Task.CompletedTask;
    }
}