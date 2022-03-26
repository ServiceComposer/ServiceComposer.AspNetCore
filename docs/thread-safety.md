# Threading and shared resources

ServiceComposer handles incoming requests asynchronously. Each handler is executed in a separate `Task`. Dependencies injected into handlers via DI are created by the root `ServiceProvider`. They are shared across more than one handler, depending on the lifecycle.

Given that handlers are executed in parallel when resources are shared by more than one handler, they must be thread-safe.
