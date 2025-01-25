using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ServiceComposer.AspNetCore.SourceGeneration
{
    [Generator]
    public class CompositionHandlerWrapperGenerator : ISourceGenerator
    {
        const string ServiceComposerNamespace = "ServiceComposer.AspNetCore";
        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will gather the methods we need to process
            context.RegisterForSyntaxNotifications(() => new CompositionHandlerSyntaxReceiver());
        }

        string GetTypeFullname(SemanticModel semanticModel, TypeSyntax type)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(type);
            var symbol = symbolInfo.Symbol;

            var fullName = symbol!.ToDisplayString();
            return fullName;
        }

        string GetRouteTemplate(AttributeSyntax attribute)
        {
            string? routeTemplate = null;
            if (attribute.ArgumentList != null)
            {
                foreach (var argument in attribute.ArgumentList.Arguments)
                {
                    if (argument.NameEquals != null)
                    {
                        if (argument.NameEquals.Name.Identifier.Text == "template" &&
                            argument.Expression is LiteralExpressionSyntax literal)
                        {
                            routeTemplate = literal.Token.ValueText;
                            break;
                        }
                    }
                    else if (argument.Expression is LiteralExpressionSyntax literal)
                    {
                        routeTemplate = literal.Token.ValueText;
                        break;
                    }
                }
            }

            return routeTemplate ?? string.Empty;
        }
        
        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                if (!(context.SyntaxContextReceiver is CompositionHandlerSyntaxReceiver receiver))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "SG0002",
                                "Error",
                                "Invalid syntax receiver",
                                "Generator",
                                DiagnosticSeverity.Error,
                                true
                            ),
                            Location.None
                        )
                    );
                    return;
                }

                foreach (var method in receiver.CompositionHandlerMethods)
                {
                    var parameters = method.Method.ParameterList.Parameters;
                    var typeAndName = parameters.Select(p =>
                    {
                        var semanticModel = context.Compilation.GetSemanticModel(p.Type!.SyntaxTree);
                        return $"{GetTypeFullname(semanticModel, p.Type).Replace('.', '_')}_{p.Identifier.Text}";
                    });
                    
                    var generatedHandlerClassName = $"{method.ClassName}_{method.Method.Identifier.Text}_{string.Join("_", typeAndName)}";
                    var generatedNamespace = $"{method.Namespace.Replace('.', '_')}_Generated";
                    var userClassFullTypeName = $"{method.Namespace}.{method.ClassName}";
                    var userMethodRouteTemplate = GetRouteTemplate(method.HttpAttribute);

                    var source = GenerateWrapperClass(
                        context,
                        generatedNamespace,
                        generatedHandlerClassName,
                        userClassFullTypeName,
                        method.Method.Identifier.Text,
                        userMethodRouteTemplate,
                        parameters);
                    context.AddSource($"{generatedHandlerClassName}.g.cs", SourceText.From(source, Encoding.UTF8));
                }
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "SG0003",
                            "Error",
                            "Generator failed: {0}",
                            "Generator",
                            DiagnosticSeverity.Error,
                            true
                        ),
                        Location.None,
                        ex.ToString()
                    )
                );
            }
        }

        string GenerateWrapperClass(
            GeneratorExecutionContext context,
            string generatedClassNamespace,
            string generatedClassName,
            string userClassFullTypeName,
            string userMethodName,
            string userMethodRouteTemplate,
            SeparatedSyntaxList<ParameterSyntax> parameters
            )
        {
            var builder = new StringBuilder();

            builder.AppendLine();

            builder.AppendLine("#pragma warning disable SC0001");
            builder.AppendLine($"namespace {generatedClassNamespace}");
            builder.AppendLine("{");

            // Generate the wrapper class
            builder.AppendLine($"    public class {generatedClassName}({userClassFullTypeName} userHandler)");
            builder.AppendLine($"         : {ServiceComposerNamespace}.ICompositionRequestsHandler");
            builder.AppendLine("    {");
            
            var propertyNames = GenerateParametersInnerClass(context, builder, parameters);
            builder.AppendLine();

            //TODO copy here all the attributes declared on the user method
            //TODO we don't want to bind to a class with properties. Instead for each parameter we need to have a corresponding bind* attribute, and then use arguments. Otherwise, filters will get a list of arguments that is different from the one expressed by user code 
            builder.AppendLine($"        [{ServiceComposerNamespace}.Bind<Parameters>]");
            builder.AppendLine("        public System.Threading.Tasks.Task Handle(Microsoft.AspNetCore.Http.HttpRequest request)");
            builder.AppendLine("        {");
            builder.AppendLine($"            var ctx = {ServiceComposerNamespace}.HttpRequestExtensions.GetCompositionContext(request);");
            builder.AppendLine("            var arguments = ctx.GetArguments(this);");
            builder.AppendLine($"            var parameters = {ServiceComposerNamespace}.ModelBindingArgumentExtensions.Argument<Parameters>(arguments);");
            builder.AppendLine();            
            builder.AppendLine($"            return userHandler.{userMethodName}(");
            builder.AppendLine(string.Join(",\n", propertyNames.Select(propertyName => 
                $"                    parameters.{propertyName}")));
            builder.AppendLine($"            );");
            builder.AppendLine("        }");
            
            builder.AppendLine("    }");
            builder.AppendLine("}");
            builder.AppendLine("#pragma warning restore SC0001");

            var code = builder.ToString();

            return code;
        }

        List<string> GenerateParametersInnerClass(GeneratorExecutionContext context, StringBuilder builder,
            SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            builder.AppendLine("        public class Parameters");
            builder.AppendLine("        {");
            
            List<string> propertyNames = [];
            // Generate properties for each parameter
            foreach (var param in parameters)
            {
                var semanticModel = context.Compilation.GetSemanticModel(param.Type!.SyntaxTree);
                var paramTypeFullName = GetTypeFullname(semanticModel, param.Type);
                var propertyName = param.Identifier.Text; //char.ToUpper(param.Identifier.Text[0]) + param.Identifier.Text.Substring(1);
                propertyNames.Add(propertyName);

                // Check for [FromBody] attribute
                var isFromBody = param.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Any(a => a.Name.ToString() == "FromBody");

                if (isFromBody)
                {
//                    builder.AppendLine($"        [JsonPropertyName(\"{param.Identifier.Text}\")]");
                }

                builder.AppendLine($"            public {paramTypeFullName} {propertyName} {{ get; set; }}");
            }
            builder.AppendLine("        }");
            
            return propertyNames;
        }
    }
}