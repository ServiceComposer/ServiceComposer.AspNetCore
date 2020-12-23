# Basic usage

To start using ServiceComposer follow the outlined steps:

- Create, in an empty or existing solution, a .NET Core 3.x or later empty web application project.
- Add a package reference to the `ServiceComposer.AspNetCore` NuGet package and configure the `Startup` class like follows:

snippet: net-core-3x-sample-startup

> NOTE: To use a `Startup` class Generic Host support is required.

- Add a new .NET Core 3.x or later class library project.
- Add a package reference to the `ServiceComposer.AspNetCore` NuGet package.
- Add a new class to create a composition request handler.
- Define the class similar to the following:

snippet: net-core-3x-basic-usage-marketing-handler

- Add another class library project and define a composition request handler like the following:

snippet: net-core-3x-basic-usage-sales-handler

- Make so that the web application project created at the beginning can load both class library assembly, e.g. by adding a reference to the class library projects
- Build and run the web application project
- Using a browser or a tool like Postman issue an HTTP Get request to `url-of-the-web-application/product/1`

The HTTP response should be a json result containing the properties and values defined in the composition handler classes.

NOTE: ServiceComposer uses regular ASP.NET Core attribute routing to configure routes for which composition support is required.
