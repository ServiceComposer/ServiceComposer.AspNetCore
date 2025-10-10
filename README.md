<img src="assets/ServiceComposer.png" width="100" />

# ServiceComposer

**A ViewModel Composition Gateway for Microservices**

Designing UIs for back-end systems with dozens (or more) of microservices presents unique challenges. While we achieve separation and autonomy on the back end, the front-end needs to reunite these distributed components cohesively. ServiceComposer prevents spaghetti code and inefficient web request patterns by enabling clean ViewModel composition.

## 🎯 Overview

ServiceComposer solves the problem of composing data from multiple autonomous services into unified view models, allowing downstream clients to consume data without being coupled to the distributed nature of your architecture.

## 📚 Contents

- [Technical Introduction](#technical-introduction)
- [Quick Start](#quick-start)
- [Documentation](#documentation)
- [Philosophy & Service Boundaries](#philosophy--service-boundaries)

## 🚀 Quick Start

Let's build an e-commerce product page where data comes from two different services:
- **Sales Service**: owns product pricing
- **Marketing Service**: owns product name and description

### Step 1: Create Gateway Project

Create a .NET 8+ empty web application:

```bash
dotnet new web -n CompositionGateway
cd CompositionGateway
```

Add the ServiceComposer NuGet package:

```bash
dotnet add package ServiceComposer.AspNetCore
```

Configure the startup:

```csharp
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

### Step 2: Create Sales Service Composition Handlers

Create a class library project:

```bash
dotnet new classlib -n Sales.ViewModelComposition
cd Sales.ViewModelComposition
dotnet add package ServiceComposer.AspNetCore
```

Add a composition handler:

```csharp
public class SalesProductInfo : ICompositionRequestsHandler
{
    [HttpGet("/product/{id}")]
    public Task Handle(HttpRequest request)
    {
        var vm = request.GetComposedResponseModel();
        var productId = request.HttpContext.GetRouteValue("id").ToString();
        
        // Retrieve product details from Sales service
        vm.ProductId = productId;
        vm.ProductPrice = 100; // Example data
        
        return Task.CompletedTask;
    }
}
```

### Step 3: Create Marketing Service Composition Handlers

Create another class library:

```bash
dotnet new classlib -n Marketing.ViewModelComposition
cd Marketing.ViewModelComposition
dotnet add package ServiceComposer.AspNetCore
```

Add a composition handler:

```csharp
public class MarketingProductInfo : ICompositionRequestsHandler
{
    [HttpGet("/product/{id}")]
    public Task Handle(HttpRequest request)
    {
        var vm = request.GetComposedResponseModel();
        
        // Retrieve product details from Marketing service
        vm.ProductName = "Sample Product";
        vm.ProductDescription = "This is a sample product description";
        
        return Task.CompletedTask;
    }
}
```

### Step 4: Run and Test

Reference the composition projects from your gateway project, then run:

```bash
dotnet run
```

Test with HTTP GET request to: `/product/1`

**Expected Response:**
```json
{
    "productId": "1",
    "productPrice": 100,
    "productName": "Sample Product",
    "productDescription": "This is a sample product description"
}
```

## 💡 Key Features

- **Dynamic View Models**: Uses C# `dynamic` objects for zero coupling between handlers
- **Strongly Typed Support**: Optional strongly typed view models with compiler checks
- **ASP.NET Core Integration**: Leverages standard ASP.NET Core routing and attributes
- **Microservices-Friendly**: Perfect for distributed systems with autonomous services

## 📖 Documentation

### Supported Platforms
- ASP.NET Core on .NET 8+ ([Documentation](docs/))

### View Model Types
ServiceComposer supports both dynamic and strongly typed view models. Dynamic view models provide maximum decoupling, while strongly typed models offer compiler safety. See the [view model factory documentation](docs/view-model-factory.md) for details.

## 🎯 Philosophy & Service Boundaries

### The Service Boundary Challenge

Service boundaries are THE critical aspect of SOA systems. Poor boundary definition leads to distributed monoliths or complete system failure. The transition from user mental models to service boundary architecture raises critical questions:

- How do we display unified data when domain entities are split across services?
- How do systems make decisions when required data spans multiple services?

### The Solution: ViewModel Composition

Traditional approaches often lead to:
- **Rich events** that share too much data
- **Cache duplication** in systems like Elasticsearch
- **Distributed monoliths** where data ownership is lost

ViewModel Composition addresses these concerns by bringing proper separation of concerns to the front-end, maintaining the autonomy you designed at the back-end level.

### Learn More

For deeper insights into Composition Gateway philosophy:
- [ViewModel Composition article series](https://milestone.topics.it/)
- [YouTube technical introduction](https://youtube.com/watch?v=your-video-id)

## 🤝 Contributing

We welcome contributions! Please see our [contributing guidelines](CONTRIBUTING.md) for details.

## 📄 License

This project is licensed under the [MIT License](LICENSE).

