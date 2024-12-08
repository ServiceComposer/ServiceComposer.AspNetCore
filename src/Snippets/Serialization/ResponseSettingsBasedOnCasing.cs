using System.Text.Json;

namespace Snippets.Serialization
{
    public class ResponseSettingsBasedOnCasing
    {
        void Camel()
        {
            // begin-snippet: camel-serialization-settings
            var settings = new JsonSerializerOptions()
            {
                // System.Text.Json requires both properties to be
                // set to properly format serialized responses
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
            };
            // end-snippet
        }

        void Pascal()
        {
            // begin-snippet: pascal-serialization-settings
            var settings = new JsonSerializerOptions();
            // end-snippet
        }
    }
}