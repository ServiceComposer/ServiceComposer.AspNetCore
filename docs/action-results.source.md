# ASP.Net MVC Action results

MVC Action results support allow composition handlers to set custom response results for specific scenarios, like for example, handling bad requests or validation error thoat would nornmally require throwing an exception. Setting a custom action result is done by using the `SetActionResult()` `HttpRequest` extension method:

snippet: net-core-3x-action-results
