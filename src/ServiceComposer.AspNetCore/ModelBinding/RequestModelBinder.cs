using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace ServiceComposer.AspNetCore
{
    class RequestModelBinder
    {
        IModelBinderFactory modelBinderFactory;
        IModelMetadataProvider modelMetadataProvider;
        IOptions<MvcOptions> mvcOptions;

        public RequestModelBinder(IModelBinderFactory modelBinderFactory, IModelMetadataProvider modelMetadataProvider, IOptions<MvcOptions> mvcOptions)
        {
            this.modelBinderFactory = modelBinderFactory;
            this.modelMetadataProvider = modelMetadataProvider;
            this.mvcOptions = mvcOptions;
        }

        public async Task<(T Model, bool IsModelSet, ModelStateDictionary ModelState)> TryBind<T>(HttpRequest request)
        {
            //always rewind the stream; otherwise,
            //if multiple handlers concurrently bind
            //different models only the first one succeeds
            request.Body.Position = 0;

            var modelType = typeof(T);
            var modelMetadata = modelMetadataProvider.GetMetadataForType(modelType);
            var actionContext = new ActionContext(
                request.HttpContext,
                request.HttpContext.GetRouteData(),
                new ActionDescriptor(),
                new ModelStateDictionary());
            var valueProvider =
                await CompositeValueProvider.CreateAsync(actionContext, mvcOptions.Value.ValueProviderFactories);

            var bindingInfo = new BindingInfo()
            {
                BinderModelName = modelMetadata.BinderModelName,
                BinderType = modelMetadata.BinderType,
                BindingSource = modelMetadata.BindingSource,
                PropertyFilterProvider = modelMetadata.PropertyFilterProvider,
            };
            
            var modelBindingContext = DefaultModelBindingContext.CreateBindingContext(
                actionContext,
                valueProvider,
                modelMetadata,
                bindingInfo: bindingInfo,
                modelName: "");

            // TODO remove this
            // modelBindingContext.Model = new T();
            modelBindingContext.PropertyFilter = _ => true; // All props

            var factoryContext = new ModelBinderFactoryContext()
            {
                Metadata = modelMetadata,
                BindingInfo = bindingInfo,
                CacheToken = modelMetadata,
            };

            await modelBinderFactory
                .CreateBinder(factoryContext)
                .BindModelAsync(modelBindingContext);

            return ((T)modelBindingContext.Result.Model,
                modelBindingContext.Result.IsModelSet,
                modelBindingContext.ModelState);
        }

        internal async Task<(object Model, bool IsModelSet, ModelStateDictionary ModelState)> TryBind(
            Type modelType,
            HttpRequest request,
            string modelName,
            BindingSource bindingSource)
        {
            //always rewind the stream; otherwise,
            //if multiple handlers concurrently bind
            //different models only the first one succeeds
            request.Body.Position = 0;
            
            // TODO remove this
            // var reader = new StreamReader(request.Body);
            // var content = await reader.ReadToEndAsync();
            // request.Body.Position = 0;

            var modelMetadata = modelMetadataProvider.GetMetadataForType(modelType);
            var actionContext = new ActionContext(
                request.HttpContext,
                request.HttpContext.GetRouteData(),
                new ActionDescriptor(),
                new ModelStateDictionary());
            var valueProvider =
                await CompositeValueProvider.CreateAsync(actionContext, mvcOptions.Value.ValueProviderFactories);

            var bindingInfo = new BindingInfo()
            {
                BinderModelName = modelMetadata.BinderModelName,
                BinderType = modelMetadata.BinderType,
                BindingSource = bindingSource,
                PropertyFilterProvider = modelMetadata.PropertyFilterProvider,
            };
            
            var modelBindingContext = DefaultModelBindingContext.CreateBindingContext(
                actionContext,
                valueProvider,
                modelMetadata,
                bindingInfo: bindingInfo,
                modelName: modelName);

            // TODO remove this
            //modelBindingContext.Model = new T();
            modelBindingContext.PropertyFilter = _ => true; // All props
            modelBindingContext.BindingSource = bindingSource;

            var factoryContext = new ModelBinderFactoryContext()
            {
                Metadata = modelMetadata,
                BindingInfo = bindingInfo,
                CacheToken = modelMetadata,
            };

            await modelBinderFactory
                .CreateBinder(factoryContext)
                .BindModelAsync(modelBindingContext);

            return (modelBindingContext.Result.Model,
                modelBindingContext.Result.IsModelSet,
                modelBindingContext.ModelState);
        }
    }
}