# ServiceComposer

ServiceComposer is a ViewModel Composition Gateway for ASP.NET Core.

Designing a UI when the back-end system consists of multiple autonomous services is challenging. ViewModel Composition solves this at the API gateway layer: each service contributes its own slice of data through an independent handler, all handlers run in parallel, and the gateway returns a single merged response to the caller.

## Getting started

Install the package in your ASP.NET Core gateway project:

```
dotnet add package ServiceComposer.AspNetCore
```

Configure the gateway in `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder();
builder.Services.AddRouting();
builder.Services.AddViewModelComposition();

var app = builder.Build();
app.MapCompositionHandlers();
app.Run();
```

Each service defines a composition handler that contributes its slice of data to the shared view model:

```csharp
// In Sales.ViewModelComposition
public class SalesProductInfo : ICompositionRequestsHandler
{
    [HttpGet("/product/{id}")]
    public Task Handle(HttpRequest request)
    {
        var vm = request.GetComposedResponseModel();
        vm.ProductId = request.HttpContext.GetRouteValue("id").ToString();
        vm.ProductPrice = 100;
        return Task.CompletedTask;
    }
}

// In Marketing.ViewModelComposition
public class MarketingProductInfo : ICompositionRequestsHandler
{
    [HttpGet("/product/{id}")]
    public Task Handle(HttpRequest request)
    {
        var vm = request.GetComposedResponseModel();
        vm.ProductName = "Sample product";
        vm.ProductDescription = "This is a sample product";
        return Task.CompletedTask;
    }
}
```

Both handlers target the same route and run in parallel. Neither knows about the other. A GET to `/product/1` returns a single merged JSON response:

```json
{
  "productId": "1",
  "productPrice": 100,
  "productName": "Sample product",
  "productDescription": "This is a sample product"
}
```

## Documentation

- [Getting started guide](https://github.com/ServiceComposer/ServiceComposer.AspNetCore/blob/master/docs/getting-started.md)
- [Full documentation](https://github.com/ServiceComposer/ServiceComposer.AspNetCore/blob/master/docs/README.md)
- [Source code](https://github.com/ServiceComposer/ServiceComposer.AspNetCore)
