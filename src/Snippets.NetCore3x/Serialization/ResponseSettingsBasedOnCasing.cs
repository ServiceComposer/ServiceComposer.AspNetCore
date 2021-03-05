using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Snippets.NetCore3x.Serialization
{
    public class ResponseSettingsBasedOnCasing
    {
        void Camel()
        {
            // begin-snippet: net-core-3x-camel-serialization-settings
            var settings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            // end-snippet
        }

        void Pascal()
        {
            // begin-snippet: net-core-3x-pascal-serialization-settings
            var settings = new JsonSerializerSettings();
            // end-snippet
        }
    }
}