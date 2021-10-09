# ASP.Net MVC Action results

MVC Action results support allow composition handlers to set custom response results for specific scenarios, like for example, handling bad requests or validation error thoat would nornmally require throwing an exception. Setting a custom action result is done by using the `SetActionResult()` `HttpRequest` extension method:

snippet: net-core-3x-action-results

Using MVC action results require enabling output formatters support:

snippet: net-core-3x-action-results-required-config

Note: ServiceComposer supports only one action result per request. If two or more composition handlers try to set action results, only the frst one will succeed and subsequent requests will be ignored.
