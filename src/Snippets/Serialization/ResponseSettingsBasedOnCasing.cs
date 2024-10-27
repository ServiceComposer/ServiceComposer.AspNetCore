using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Snippets.Serialization
{
    public class ResponseSettingsBasedOnCasing
    {
        void Camel()
        {
            // begin-snippet: camel-serialization-settings
            var settings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            // end-snippet
        }

        void Pascal()
        {
            // begin-snippet: pascal-serialization-settings
            var settings = new JsonSerializerSettings();
            // end-snippet
        }
    }
}