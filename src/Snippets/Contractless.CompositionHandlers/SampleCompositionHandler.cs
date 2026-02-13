using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace Snippets.Contractless.CompositionHandlers;

#region contract-less-handler-sample
[CompositionHandler]
class SampleCompositionHandler
{
    [HttpGet("/sample/{id}")]
    public Task SampleMethod(int id, [FromQuery(Name = "c")]string aValue, [FromBody]ComplexType ct)
    {
        return Task.CompletedTask;
    }
}
#endregion

class ComplexType;