using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace ServiceComposer.AspNetCore.SourceGeneration.Tests.TestFiles.ComponentsProviders;

public class RequestsHandler : ICompositionRequestsHandler
{
    [HttpGet("/sample/{id}")]
    public Task Handle(HttpRequest request) => Task.CompletedTask;
}

class InternalRequestsHandler : ICompositionRequestsHandler
{
    [HttpGet("/sample/{id}")]
    public Task Handle(HttpRequest request) => Task.CompletedTask;
}

class EventsSubscriber : ICompositionEventsSubscriber
{
    [HttpGet("/sample/{id}")]
    public void Subscribe(ICompositionEventsPublisher publisher) { }
}

class SampleEvent;

class EventsHandler : ICompositionEventsHandler<SampleEvent>
{
    public Task Handle(SampleEvent @event, HttpRequest request) => Task.CompletedTask;
}

class PreviewHandler : IViewModelPreviewHandler
{
    public Task Preview(HttpRequest request) => Task.CompletedTask;
}

class GlobalViewModelFactory : IViewModelFactory
{
    public object CreateViewModel(HttpContext httpContext, ICompositionContext compositionContext) => new object();
}

class EndpointScopedViewModelFactory : IEndpointScopedViewModelFactory
{
    [HttpGet("/sample/{id}")]
    public object CreateViewModel(HttpContext httpContext, ICompositionContext compositionContext) => new object();
}

class RequestFilter : ICompositionRequestFilter<RequestsHandler>, ICompositionRequestFilter<InternalRequestsHandler>
{
    public ValueTask<object> InvokeAsync(CompositionRequestFilterContext context, CompositionRequestFilterDelegate next) => next(context);
}

// Filters applied as attributes are not registered in the container.
class RequestFilterAttribute : CompositionRequestFilterAttribute
{
    public override ValueTask<object> InvokeAsync(CompositionRequestFilterContext context, CompositionRequestFilterDelegate next) => next(context);
}

class OptionsCustomization : IViewModelCompositionOptionsCustomization
{
    public void Customize(ViewModelCompositionOptions options) { }
}

[CompositionHandler]
class ContractlessHandler
{
    [HttpGet("/sample/{id}")]
    public Task Get(int id) => Task.CompletedTask;
}

public class Container
{
    public class NestedRequestsHandler : ICompositionRequestsHandler
    {
        [HttpGet("/sample/{id}")]
        public Task Handle(HttpRequest request) => Task.CompletedTask;
    }

    // Not accessible at the assembly level: skipped.
    private class PrivateNestedRequestsHandler : ICompositionRequestsHandler
    {
        [HttpGet("/sample/{id}")]
        public Task Handle(HttpRequest request) => Task.CompletedTask;
    }
}

// Abstract and open generic types are skipped.
abstract class AbstractRequestsHandler : ICompositionRequestsHandler
{
    public abstract Task Handle(HttpRequest request);
}

class GenericRequestsHandler<T> : ICompositionRequestsHandler
{
    [HttpGet("/sample/{id}")]
    public Task Handle(HttpRequest request) => Task.CompletedTask;
}

// Customizations without an accessible parameterless constructor are skipped.
class OptionsCustomizationWithoutParameterlessConstructor(int value) : IViewModelCompositionOptionsCustomization
{
    public void Customize(ViewModelCompositionOptions options) { _ = value; }
}

class NotAComponent;
