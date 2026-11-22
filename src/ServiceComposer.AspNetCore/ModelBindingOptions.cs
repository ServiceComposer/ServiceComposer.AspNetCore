namespace ServiceComposer.AspNetCore
{
    public class ModelBindingOptions
    {
        /// <summary>
        /// When <c>true</c>, the default, a request whose declaratively bound models have binding
        /// errors, such as a value that can't be converted, malformed JSON, or a missing required
        /// body, gets a <c>400 Bad Request</c> with a validation problem details body, before any
        /// composition handler or endpoint filter runs. When <c>false</c>, the composition handlers
        /// run with the partially bound models.
        /// </summary>
        public bool FailOnBindingErrors { get; set; } = true;
    }
}
