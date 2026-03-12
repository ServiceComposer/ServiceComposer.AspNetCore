using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    /// <summary>
    /// Telemetry string values are part of the public API contract: users embed them in OTel
    /// pipelines, dashboards, and alert rules. This snapshot test ensures that any change to
    /// these values is an intentional, visible act that requires approving the updated snapshot.
    /// </summary>
    public class When_verifying_telemetry_contract_stability
    {
        [Fact]
        public Task Telemetry_constants_match_approved_values()
        {
            var constants = new SortedDictionary<string, string>();
            CollectConstants(typeof(CompositionTelemetry), constants);
            return Verifier.Verify(constants);
        }

        static void CollectConstants(Type type, SortedDictionary<string, string> result, string prefix = "")
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.IsLiteral && !field.IsInitOnly)
                {
                    var key = string.IsNullOrEmpty(prefix) ? field.Name : $"{prefix}.{field.Name}";
                    result[key] = (string)field.GetValue(null);
                }
            }

            foreach (var nested in type.GetNestedTypes(BindingFlags.Public))
            {
                var nestedPrefix = string.IsNullOrEmpty(prefix) ? nested.Name : $"{prefix}.{nested.Name}";
                CollectConstants(nested, result, nestedPrefix);
            }
        }
    }
}
