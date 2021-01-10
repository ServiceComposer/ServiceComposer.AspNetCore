namespace ServiceComposer.AspNetCore
{
#if NETCOREAPP3_1 || NET5_0
    public class ResponseSerializationOptions
    {
        public ResponseCasing DefaultResponseCasing { get; set; } = ResponseCasing.CamelCase;
    }

    public enum ResponseCasing
    {
        CamelCase = 0,
        PascalCase = 1
    }
#endif
}