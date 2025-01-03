using Microsoft.AspNetCore.Mvc;

namespace Snippets.ModelBinding;

// begin-snippet: model-binding-request
class RequestModel
{
    [FromRoute(Name = "id")] public int Id { get; set; }
    [FromBody] public BodyModel Body { get; set; }
}
// end-snippet