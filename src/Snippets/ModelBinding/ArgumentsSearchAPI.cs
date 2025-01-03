using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ServiceComposer.AspNetCore;

namespace Snippets.ModelBinding;

public class ArgumentsSearchAPI
{
    void Snippet(HttpRequest request)
    {
#pragma warning disable SC0001
        // begin-snippet: arguments-search-api
        var ctx = request.GetCompositionContext();
        var arguments = ctx.GetArguments(GetType());
        var findValueByType = arguments.Argument<BodyModel>();
        var findValueByTypeAndName = arguments.Argument<int>(name: "id");
        var findValueByTypeAndSource = arguments.Argument<int>(bindingSource: BindingSource.Header);
        var findValueByTypeSourceAndName = arguments.Argument<string>(name: "user", bindingSource: BindingSource.Query);
        // end-snippet
#pragma warning restore SC0001
    }
}