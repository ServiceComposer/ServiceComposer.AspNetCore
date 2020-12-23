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

snippet: net-core-3x-sample-handler

NOTE: ServiceComposer uses regular ASP.NET Core attribute routing to configure routes for which composition support is required.
