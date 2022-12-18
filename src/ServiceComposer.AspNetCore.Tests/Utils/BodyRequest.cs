using Microsoft.AspNetCore.Mvc;

namespace ServiceComposer.AspNetCore.Tests
{
    class BodyRequest<TBody>
    {
        [FromBody] public TBody Body { get; set; }
    }
}