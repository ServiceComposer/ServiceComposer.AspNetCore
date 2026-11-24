using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ServiceComposer.AspNetCore.SourceGeneration.Tests.TestFiles.CompositionHandlers;

public class CartItem
{
    public int Id { get; set; }
}

[CompositionHandler]
class GenericParameterTypesCompositionHandler
{
    [HttpPost("/cart/{orderId}")]
    public Task Post(int? orderId, [FromBody] List<CartItem> items, [FromQuery] int[] ids, [FromServices] IDictionary<string, CartItem> lookup)
    {
        return Task.CompletedTask;
    }
}
