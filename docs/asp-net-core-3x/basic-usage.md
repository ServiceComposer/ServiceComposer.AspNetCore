# Basic usage

Let's imagine a very basic e-commerce web page, where it's needed to display details about a selected product. These details are stored into two different services. The SalesService owns the product price, and the MarketingService owns the product name and description. ServiceComposer is designed to compose information coming from different services into one single composed view model that can be later displayed or consumed by downstream clients.

To start using ServiceComposer follow the outlined steps:

- Create, in an empty or existing solution, a .NET Core 3.x or later empty web application project named `CompositionGateway`.
- Add a package reference to the `ServiceComposer.AspNetCore` NuGet package and configure the `Startup` class like follows:

<!-- snippet: net-core-3x-sample-startup -->
<a id='snippet-net-core-3x-sample-startup'></a>
```cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddViewModelComposition();
    }

    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
    {
        app.UseRouting();
        app.UseEndpoints(builder => builder.MapCompositionHandlers());
    }
}
```
<sup><a href='/src/Snippets.NetCore3x/BasicUsage/Startup.cs#L8-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-net-core-3x-sample-startup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

> NOTE: To use a `Startup` class Generic Host support is required.

- Add a new .NET Core 3.x or later class library project, named `Sales.ViewModelComposition`.
- Add a package reference to the `ServiceComposer.AspNetCore` NuGet package.
- Add a new class to create a composition request handler.
- Define the class similar to the following:

<!-- snippet: net-core-3x-basic-usage-marketing-handler -->
<a id='snippet-net-core-3x-basic-usage-marketing-handler'></a>
```cs
public class MarketingProductInfo: ICompositionRequestsHandler
{
    [HttpGet("/product/{id}")]
    public Task Handle(HttpRequest request)
    {
        var vm = request.GetComposedResponseModel();

        //retrieve product details from the marketing database or service
        vm.ProductName = "Sample product";
        vm.ProductDescription = "This is a sample product";
        
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets.NetCore3x/BasicUsage/MarketingProductInfo.cs#L8-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-net-core-3x-basic-usage-marketing-handler' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

- Add another class library project, named `Marketing.ViewModelComposition`, and define a composition request handler like the following:

<!-- snippet: net-core-3x-basic-usage-sales-handler -->
<a id='snippet-net-core-3x-basic-usage-sales-handler'></a>
```cs
public class SalesProductInfo : ICompositionRequestsHandler
{
    [HttpGet("/product/{id}")]
    public Task Handle(HttpRequest request)
    {
        var vm = request.GetComposedResponseModel();

        //retrieve product details from the sales database or service
        vm.ProductId = request.HttpContext.GetRouteValue("id").ToString();
        vm.ProductPrice = 100;

        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets.NetCore3x/BasicUsage/SalesProductInfo.cs#L9-L24' title='Snippet source file'>snippet source</a> | <a href='#snippet-net-core-3x-basic-usage-sales-handler' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

- Make so that the web application project created at the beginning can load both class library assemblies, e.g. by adding a reference to the class library projects
- Build and run the web application project
- Using a browser or a tool like Postman issue an HTTP Get request to `<url-of-the-web-application>/product/1`

The HTTP response should be a json result containing the properties and values defined in the composition handler classes.

NOTE: ServiceComposer uses regular ASP.NET Core attribute routing to configure routes for which composition support is required.
