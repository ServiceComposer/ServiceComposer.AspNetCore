using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ServiceComposer.AspNetCore.Generators
{
    [Generator]
    public class ControllerWrapperGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will gather the methods we need to process
            context.RegisterForSyntaxNotifications(() => new ControllerSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxContextReceiver is ControllerSyntaxReceiver receiver))
                return;

            foreach (var method in receiver.ControllerMethods)
            {
                var nameSpace = GetNamespace(method);
                var className = method.Identifier.Text + "Parameters";
                var parameters = method.ParameterList.Parameters;

                var source = GenerateWrapperClass(nameSpace, className, parameters);
                context.AddSource($"{className}.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        }

        string GenerateWrapperClass(string nameSpace, string className, SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            var builder = new StringBuilder();

            builder.AppendLine("using System;");
            builder.AppendLine("using System.Text.Json.Serialization;");
            builder.AppendLine();

            builder.AppendLine($"namespace {nameSpace}");
            builder.AppendLine("{");

            // Generate the wrapper class
            builder.AppendLine($"    public class {className}");
            builder.AppendLine("    {");

            // Generate properties for each parameter
            foreach (var param in parameters)
            {
                var paramType = param.Type?.ToString() ?? "object";
                var paramName = char.ToUpper(param.Identifier.Text[0]) + param.Identifier.Text.Substring(1);

                // Check for [FromBody] attribute
                var isFromBody = param.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Any(a => a.Name.ToString() == "FromBody");

                if (isFromBody)
                {
                    builder.AppendLine($"        [JsonPropertyName(\"{param.Identifier.Text}\")]");
                }

                builder.AppendLine($"        public {paramType} {paramName} {{ get; set; }}");
                builder.AppendLine();
            }

            // Generate a constructor
            builder.AppendLine($"        public {className}()");
            builder.AppendLine("        {");
            builder.AppendLine("        }");

            // Generate a constructor with parameters
            builder.AppendLine($"        public {className}(");
            builder.AppendLine(string.Join(",\n", parameters.Select(p =>
                $"            {p.Type} {p.Identifier.Text}")));
            builder.AppendLine("        )");
            builder.AppendLine("        {");

            foreach (var param in parameters)
            {
                var paramName = char.ToUpper(param.Identifier.Text[0]) + param.Identifier.Text.Substring(1);
                builder.AppendLine($"            {paramName} = {param.Identifier.Text};");
            }

            builder.AppendLine("        }");

            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }

        static string GetNamespace(MethodDeclarationSyntax methodSyntax)
        {
            var potentialNamespaceParent = methodSyntax.Parent;
            while (potentialNamespaceParent is not NamespaceDeclarationSyntax
                   ||
                   potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
            {
                potentialNamespaceParent = potentialNamespaceParent?.Parent;
            }

            var nameSpace = potentialNamespaceParent switch
            {
                NamespaceDeclarationSyntax namespaceDeclaration => namespaceDeclaration.Name.ToString(),
                FileScopedNamespaceDeclarationSyntax filerScopedNamespaceDeclaration => filerScopedNamespaceDeclaration.Name.ToString(),
                _ => string.Empty
            };

            return nameSpace;
        }
    }

    public class ControllerSyntaxReceiver : ISyntaxContextReceiver
    {
        public List<MethodDeclarationSyntax> ControllerMethods { get; } = [];

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is MethodDeclarationSyntax { AttributeLists.Count: > 0 } methodDeclaration)
            {
                var hasHttpPostAttribute = methodDeclaration.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Any(a => a.Name.ToString() == "HttpPost");

                if (hasHttpPostAttribute)
                {
                    ControllerMethods.Add(methodDeclaration);
                }
            }
        }
    }
}