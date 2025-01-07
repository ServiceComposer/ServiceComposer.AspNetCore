using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ServiceComposer.AspNetCore.SourceGeneration
{
    [Generator]
    public class CompositionHandlerWrapperGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will gather the methods we need to process
            context.RegisterForSyntaxNotifications(() => new CompositionHandlerSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxContextReceiver is CompositionHandlerSyntaxReceiver receiver))
            {
                return;
            }

            foreach (var method in receiver.CompositionHandlerMethods)
            {
                var className = $"{method.ClassName}_{method.Method.Identifier.Text}_Parameters";
                var parameters = method.Method.ParameterList.Parameters;

                var source = GenerateWrapperClass(method.Namespace, className, parameters);
                context.AddSource($"{className}.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        }

        string GenerateWrapperClass(string nameSpace, string className, SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            // We need to make sure we don't end in an infinite loop.
            // The generated classes will have the Http* attributes on them 
            
            var builder = new StringBuilder();

            builder.AppendLine("using System;");
            builder.AppendLine("using System.Text.Json.Serialization;");
            builder.AppendLine();

            builder.AppendLine($"namespace Generated.{nameSpace}");
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

            var code = builder.ToString();
            
            return code;
        }
    }

    public class CompositionHandlerSyntaxReceiver : ISyntaxContextReceiver
    {
        public List<(MethodDeclarationSyntax Method, string Namespace, string ClassName)> CompositionHandlerMethods { get; } = [];

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is not MethodDeclarationSyntax { AttributeLists.Count: > 0 } methodDeclaration
                ||
                methodDeclaration.SyntaxTree.FilePath.EndsWith(".g.cs"))
            {
                return;
            }
            
            var hasHttpAttribute = methodDeclaration.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(a => a.Name.ToString() == "HttpPost");

            if (hasHttpAttribute)
            {
                CompositionHandlerMethods.Add((methodDeclaration, GetNamespace(methodDeclaration), GetClassName(methodDeclaration)));
            }
        }

        static string GetClassName(MethodDeclarationSyntax methodSyntax)
        {
            var potentialClassParent = methodSyntax.Parent;
            while (potentialClassParent != null &&
                   !(potentialClassParent is ClassDeclarationSyntax))
            {
                potentialClassParent = potentialClassParent.Parent;
            }
            
            return (((ClassDeclarationSyntax)potentialClassParent!)!).Identifier.Text;
        }
        
        static string GetNamespace(MethodDeclarationSyntax methodSyntax)
        {
            var potentialNamespaceParent = methodSyntax.Parent;
            while (potentialNamespaceParent != null &&
                   !(potentialNamespaceParent is NamespaceDeclarationSyntax || potentialNamespaceParent is FileScopedNamespaceDeclarationSyntax))
            {
                potentialNamespaceParent = potentialNamespaceParent?.Parent;
            }

            var nameSpace = potentialNamespaceParent switch
            {
                NamespaceDeclarationSyntax namespaceDeclaration => namespaceDeclaration.Name.ToString(),
                FileScopedNamespaceDeclarationSyntax filerScopedNamespaceDeclaration => filerScopedNamespaceDeclaration.Name.ToString(),
                _ => "UndefinedNamespace"
            };

            return nameSpace;
        }
    }
}