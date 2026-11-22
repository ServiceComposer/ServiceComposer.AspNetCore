using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ServiceComposer.AspNetCore.SourceGeneration.Tests.TestFiles.CompositionHandlers;

public static class Markers
{
    public class AuditedAttribute : Attribute { }

    public class TaggedAttribute<T> : Attribute { }

    public enum Severity { Low, High }

    public class SeverityLevelAttribute(Severity severity) : Attribute
    {
        public Severity Severity { get; } = severity;
    }
}

[Markers.Audited]
[Markers.Tagged<int>]
[CompositionHandler]
class NestedAndGenericAttributesCompositionHandler
{
    [Markers.SeverityLevel(Markers.Severity.High)]
    [HttpGet("/sample/{id}")]
    public Task Get(int id)
    {
        return Task.CompletedTask;
    }
}
