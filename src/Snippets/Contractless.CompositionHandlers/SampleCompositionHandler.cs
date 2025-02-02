using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

#region contract-less-handler-sample
namespace Snippets.Contractless.CompositionHandlers;

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