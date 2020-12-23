# Custom HTTP status codes in ASP.NET Core 3.x

The response status code can be set in requests handlers and it'll be honored by the composition pipeline. To set a custom response status code the following snippet can be used:

snippet: net-core-3x-sample-handler-with-custom-status-code

NOTE: Requests handlers are executed in parallel in a non-deterministic way, setting the response code in more than one handler can have unpredictable effects.
