# Composition over controllers

ServiceComposer can be used to enhance a MVC web application by adding compostion support to Controllers. ServiceComposer can be configured to use a technique called "Composition over controllers":

<!-- snippet: enable-composition-over-controllers -->
<a id='snippet-enable-composition-over-controllers'></a>
```cs
services.AddViewModelComposition(options =>
{
    options.EnableCompositionOverControllers();
});
```
<sup><a href='/src/Snippets/CompositionOverController.cs#L10-L15' title='Snippet source file'>snippet source</a> | <a href='#snippet-enable-composition-over-controllers' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Once composition over controllers is enabled, ServiceComposer will inject a MVC filter to intercept all controllers invocations. If a route matches a regular controller and a set of composition handlers ServiceComposer will invoke the matching handlers after the controller and before the view is rendered.

Composition over controllers can be used as a templating engine leveraging the excellent Razor engine. Optionally, it can be used to add ViewModel Composition support to MVC web application without introducing a separate composition gateway.
