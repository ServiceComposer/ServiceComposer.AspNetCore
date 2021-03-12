using Microsoft.AspNetCore.Mvc;

namespace ServiceComposer.AspNetCore.Endpoints.Tests
{
    class BodyRequest<TBody>
    {
        [FromBody] public TBody Body { get; set; }
    }
}