using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace TestClassLibraryWithHandlers.CompositionHandlers;

public class TestContractLessCompositionHandler(IHttpContextAccessor contextAccessor)
{
    [HttpGet("/contract-less-handler/{id}")]
    public Task MyMethod(int id)
    {
        var model = contextAccessor.HttpContext.Request.GetComposedResponseModel();
        model.Value = id;
        
        return Task.CompletedTask;
    }
}