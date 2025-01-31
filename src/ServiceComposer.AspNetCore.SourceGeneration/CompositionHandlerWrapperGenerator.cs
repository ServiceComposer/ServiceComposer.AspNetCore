using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ServiceComposer.AspNetCore.SourceGeneration;

[Generator]
public class CompositionHandlerWrapperGenerator : ISourceGenerator
{
    // const string ServiceComposerNamespace = "ServiceComposer.AspNetCore";

    public void Initialize(GeneratorInitializationContext context)
    {
        // Register a syntax receiver that will gather the methods we need to process
        context.RegisterForSyntaxNotifications(() => new CompositionHandlerSyntaxReceiver());
    }

    (string? typeName, string? nmespaceName) GetTypeAndNamespaceName(SemanticModel semanticModel, TypeSyntax type)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(type);
        var symbol = symbolInfo.Symbol;

        if (symbol is INamedTypeSymbol namedTypeSymbol)
        {
            return (GetClassNameIncludingParentClasses(namedTypeSymbol), namedTypeSymbol.ContainingNamespace.ToDisplayString());
        }
        
        return (null, null);
    }

    bool IsNestedClass(INamedTypeSymbol symbol)
    {
        bool isNestedClass = symbol is { TypeKind: TypeKind.Class, ContainingType: not null };
        return isNestedClass;
    }

    string GetClassNameIncludingParentClasses(INamedTypeSymbol symbol)
    {
        if (!IsNestedClass(symbol))
        {
            return symbol.Name;
        }

        List<string> hierarchy = [symbol.Name];
        var parent = symbol;
        while (parent.ContainingType != null)
        {
            hierarchy.Add(parent.ContainingType.Name);
            parent = parent.ContainingType;
        }
        
        hierarchy.Reverse();
        return string.Join(".", hierarchy);
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

    bool TryGetHttpAttribute(GeneratorExecutionContext context, MethodDeclarationSyntax method,
        AttributeSyntax[] attributes, out AttributeSyntax? attribute)
    {
        if (attributes.Length > 1)
        {
            var descriptor = new DiagnosticDescriptor(
                id: "SC001",
                title: "Configuration not supported",
                messageFormat:
                "The method {0} contains more than one Http* attribute. This version of ServiceComposer supports only one Http* per method.",
                category: "CodeGeneration",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true
            );

            var diagnostic = Diagnostic.Create(
                descriptor,
                method.GetLocation(),
                method.Identifier.Text
            );

            context.ReportDiagnostic(diagnostic);

            attribute = null;
            return false;
        }

        attribute = attributes[0];
        return true;
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
                var semanticModel = context.Compilation.GetSemanticModel(method.Method.SyntaxTree);
                
                var parameters = method.Method.ParameterList.Parameters;
                var typeAndName = parameters.Select(p =>
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(p.Type!);
                    var symbol = symbolInfo.Symbol;

                    var fullName = symbol!.ToDisplayString();
                    return $"{fullName.Replace('.', '_')}_{p.Identifier.Text}";
                });

                if (!TryGetHttpAttribute(context, method.Method, method.HttpAttributes, out var httpAttribute))
                {
                    return;
                }
                
                var flattenUserClassesHierarchy = string.Join("_", method.UserClassesHierarchy);
                var generatedHandlerClassName = $"{flattenUserClassesHierarchy}_{method.Method.Identifier.Text}_{string.Join("_", typeAndName)}".TrimEnd('_');
                var generatedNamespace = $"{method.Namespace}.Generated";
                
                var userClassName = string.Join(".", method.UserClassesHierarchy);
                var userClassFullTypeName = $"{method.Namespace}.{userClassName}";
                var userMethodRouteTemplate = GetRouteTemplate(httpAttribute!);

                var source = GenerateWrapperClass(
                    semanticModel,
                    generatedNamespace,
                    generatedHandlerClassName,
                    userClassFullTypeName,
                    method.Method,
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
        SemanticModel semanticModel,
        string generatedClassNamespace,
        string generatedClassName,
        string userClassFullTypeName,
        MethodDeclarationSyntax userMethod,
        string userMethodRouteTemplate,
        SeparatedSyntaxList<ParameterSyntax> parameters
    )
    {
        var builder = new StringBuilder();
        
        List<string> requiredNamespaces =
        [
            "System.ComponentModel",
            "ServiceComposer.AspNetCore",
            "System.Threading.Tasks",
            "Microsoft.AspNetCore.Http",
            "Microsoft.AspNetCore.Mvc.ModelBinding"
        ];

        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("{usingDirectivesPlaceholder}");

        builder.AppendLine("#pragma warning disable SC0001");
        builder.AppendLine($"namespace {generatedClassNamespace}");
        builder.AppendLine("{");

        builder.AppendLine("    [EditorBrowsable(EditorBrowsableState.Never)]");
        builder.AppendLine($"    class {generatedClassName}({userClassFullTypeName} userHandler)");
        builder.AppendLine($"         : ICompositionRequestsHandler");
        builder.AppendLine("    {");

        var userMethodAttributes = SerializeUserHandlerAttributeList(semanticModel, userMethod.AttributeLists, requiredNamespaces);
        foreach (var userMethodAttribute in userMethodAttributes)
        {
            builder.AppendLine($"        {userMethodAttribute}");
        }

        var boundParameters = AppendBindAttributes(semanticModel, builder, parameters, userMethodRouteTemplate, requiredNamespaces);
        builder.AppendLine("        public Task Handle(HttpRequest request)");
        builder.AppendLine("        {");
        builder.AppendLine($"            var ctx = HttpRequestExtensions.GetCompositionContext(request);");
        builder.AppendLine("            var arguments = ctx.GetArguments(this);");

        List<string> generatedArgs = [];
        for (var i = 0; i < boundParameters.Count; i++)
        {
            var boundParameter = boundParameters[i];
            var arg = $"p{i}_{boundParameter.parameterName}";
            generatedArgs.Add(arg);
            builder.AppendLine($"            var {arg} = ModelBindingArgumentExtensions.Argument<{boundParameter.parameterType}>(arguments, \"{boundParameter.parameterName}\", {boundParameter.bindingSource});");
        }

        builder.AppendLine();
        builder.AppendLine($"            return userHandler.{userMethod.Identifier.Text}({string.Join(", ", generatedArgs)});");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine("#pragma warning restore SC0001");

        var code = builder.ToString();

        var usingsBuilder = new StringBuilder();
        foreach (var rn in requiredNamespaces.Distinct().OrderBy(n => n))
        {
            usingsBuilder.AppendLine($"using {rn};");
        }

        var withUsings = code.Replace("{usingDirectivesPlaceholder}", usingsBuilder.ToString());
        return withUsings;
    }

    static bool IsSimpleType(ITypeSymbol? typeSymbol)
    {
        var simpleTypes = new[]
        {
            "System.Boolean",
            "System.Byte", "System.SByte",
            "System.Int16", "System.UInt16",
            "System.Int32", "System.UInt32",
            "System.Int64", "System.UInt64",
            "System.Single", "System.Double",
            "System.Decimal",
            "System.Char",
            "System.String",
            "System.DateTime",
            "System.DateTimeOffset",
            "System.Guid"
        };

        if (typeSymbol == null)
        {
            return false;
        }

        return typeSymbol.IsValueType
               || typeSymbol.TypeKind == TypeKind.Enum
               || simpleTypes.Contains(typeSymbol.ToDisplayString());
    }

    bool TryAppendBindingFromForm(SemanticModel semanticModel, StringBuilder builder,
        ParameterSyntax parameter, string _, List<string> requiredNamespaces,
        out (string parameterName, string parameterType, string bindingSource) boundParam)
    {
        var paramTypeFullName = GetTypeAndNamespaceName(semanticModel, parameter.Type!);
        requiredNamespaces.Add(paramTypeFullName.nmespaceName!);
        
        const string bindingSource = "BindingSource.Form";
        string[] attributeNames = ["FromForm", "FromFormAttribute"];

        var (attribute, nameArgument) = GetAttributeAndArgument(parameter, attributeNames, "Name");

        if (attribute is not null)
        {
            var paramName = nameArgument is null
                ? parameter.Identifier.Text
                : nameArgument.Expression.ToString().Trim('"');

            builder.AppendLine(
                $"        [BindFromForm<{paramTypeFullName.typeName}>(\"{paramName}\")]");
            boundParam = (paramName, paramTypeFullName.typeName!, bindingSource);
            return true;
        }

        boundParam = ("", "", "");
        return false;
    }

    bool TryAppendBindingFromBody(SemanticModel semanticModel, StringBuilder builder,
        ParameterSyntax parameter, string _, List<string> requiredNamespaces,
        out (string parameterName, string parameterType, string bindingSource) boundParam)
    {
        var typeSymbol = semanticModel.GetTypeInfo(parameter.Type!).Type;
        var isSimpleType = IsSimpleType(typeSymbol!);
        var paramTypeFullName = GetTypeAndNamespaceName(semanticModel, parameter.Type!);
        requiredNamespaces.Add(paramTypeFullName.nmespaceName!);

        const string bindingSource = "BindingSource.Body";
        string[] attributeNames = ["FromBody", "FromBodyAttribute"];

        // TODO can we somehow support the EmptyBodyBehavior?
        var (attribute, _) = GetAttributeAndArgument(parameter, attributeNames, "EmptyBodyBehavior");
        var addAttribute = attribute is not null || isSimpleType == false;

        if (addAttribute)
        {
            builder.AppendLine($"        [BindFromBody<{paramTypeFullName.typeName}>()]");
            boundParam = (parameter.Identifier.Text, paramTypeFullName.typeName!, bindingSource);
            return true;
        }

        boundParam = ("", "", "");
        return false;
    }

    bool TryAppendBindingFromQuery(SemanticModel semanticModel, StringBuilder builder,
        ParameterSyntax parameter, string _, List<string> requiredNamespaces,
        out (string parameterName, string parameterType, string bindingSource) boundParam)
    {
        var typeSymbol = semanticModel.GetTypeInfo(parameter.Type!).Type;
        var isSimpleType = IsSimpleType(typeSymbol!);
        var paramTypeFullName = GetTypeAndNamespaceName(semanticModel, parameter.Type!);
        requiredNamespaces.Add(paramTypeFullName.nmespaceName!);

        const string bindingSource = "BindingSource.Query";
        string[] attributeNames = ["FromQuery", "FromQueryAttribute"];

        var (attribute, nameArgument) = GetAttributeAndArgument(parameter, attributeNames, "Name");
        var addAttribute = attribute is not null || isSimpleType;
        var paramName = nameArgument is null
            ? parameter.Identifier.Text
            : nameArgument.Expression.ToString().Trim('"');

        if (addAttribute)
        {
            builder.AppendLine(
                $"        [BindFromQuery<{paramTypeFullName.typeName}>(\"{paramName}\")]");
            boundParam = (paramName, paramTypeFullName.typeName!, bindingSource);
            return true;
        }

        boundParam = ("", "", "");
        return false;
    }

    bool TryAppendBindingFromRoute(SemanticModel semanticModel, StringBuilder builder,
        ParameterSyntax parameter, string userMethodRouteTemplate, List<string> requiredNamespaces,
        out (string parameterName, string parameterType, string bindingSource) boundParam)
    {
        var paramTypeFullName = GetTypeAndNamespaceName(semanticModel, parameter.Type!);
        requiredNamespaces.Add(paramTypeFullName.nmespaceName!);

        const string bindingSource = "BindingSource.Path";
        string[] possibleMatches =
        [
            "{" + parameter.Identifier.Text + "}",
            "{" + parameter.Identifier.Text + ":"
        ];
        string[] attributeNames = ["FromRoute", "FromRouteAttribute"];

        var (_, nameArgument) = GetAttributeAndArgument(parameter, attributeNames, "Name");
        var appendAttribute = nameArgument is not null || possibleMatches.Any(userMethodRouteTemplate.Contains);
        var paramName = nameArgument is null
            ? parameter.Identifier.Text
            : nameArgument.Expression.ToString().Trim('"');

        if (appendAttribute)
        {
            builder.AppendLine(
                $"        [BindFromRoute<{paramTypeFullName.typeName}>(\"{paramName}\")]");
            boundParam = (paramName, paramTypeFullName.typeName!, bindingSource);
            return true;
        }

        boundParam = ("", "", "");
        return false;
    }

    static (AttributeSyntax? attribute, AttributeArgumentSyntax? argument) GetAttributeAndArgument(
        ParameterSyntax parameter, string[] attributeNamesToMatch, string argumentName)
    {
        var attribute = parameter.AttributeLists
            .SelectMany(al => al.Attributes)
            .SingleOrDefault(a => attributeNamesToMatch.Any(attributeName => a.Name.ToString() == attributeName));
        var argument = attribute?.ArgumentList?.Arguments
            .SingleOrDefault(arg => arg.NameEquals != null && arg.NameEquals.Name.Identifier.Text == argumentName);
        return (attribute, argument);
    }

    List<(string parameterName, string parameterType, string bindingSource)> AppendBindAttributes(
        SemanticModel semanticModel, StringBuilder builder,
        SeparatedSyntaxList<ParameterSyntax> parameters, string userMethodRouteTemplate, List<string> requiredNamespaces)
    {
        List<(string parameterName, string parameterType, string bindingSource)> boundParameters = [];
        foreach (var param in parameters)
        {
            // TODO all the TryAppendBinding must check if there are other valid attributes
            //   that are not relevant to them, otherwise the order here matters. For example,
            //   without that check the FromQuery must be las, otherwise if the argument type
            //   is a simple type it'll win even if there are other relevant attributes
            if (TryAppendBindingFromRoute(semanticModel, builder, param, userMethodRouteTemplate, requiredNamespaces,
                    out var fromRouteBoundParam))
            {
                boundParameters.Add(fromRouteBoundParam);
                continue;
            }

            if (TryAppendBindingFromBody(semanticModel, builder, param, userMethodRouteTemplate, requiredNamespaces,
                    out var fromBodyBoundParam))
            {
                boundParameters.Add(fromBodyBoundParam);
                continue;
            }

            if (TryAppendBindingFromForm(semanticModel, builder, param, userMethodRouteTemplate, requiredNamespaces,
                    out var fromFormBoundParam))
            {
                boundParameters.Add(fromFormBoundParam);
                continue;
            }

            if (TryAppendBindingFromQuery(semanticModel, builder, param, userMethodRouteTemplate, requiredNamespaces,
                    out var fromQueryBoundParam))
            {
                boundParameters.Add(fromQueryBoundParam);
                continue;
            }
        }

        return boundParameters;
    }

    List<string> SerializeUserHandlerAttributeList(SemanticModel semanticModel, SyntaxList<AttributeListSyntax> attributeLists, List<string> requiredNamespaces)
    {
        var serializedAttributes = new List<string>();

        foreach (var attributeList in attributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(attribute);
                var attributeType = symbolInfo.Symbol as IMethodSymbol;

                if (attributeType?.ContainingType == null) continue;

                var attributeName = attributeType.ContainingType.Name;
                var attributeNamespace = attributeType.ContainingNamespace.ToDisplayString();
                requiredNamespaces.Add(attributeNamespace);
                var arguments = SerializeAttributeArguments(attribute, semanticModel, requiredNamespaces);

                serializedAttributes.Add($"[{attributeName}{arguments}]");
            }
        }

        return serializedAttributes;
    }

    string SerializeAttributeArguments(AttributeSyntax attribute, SemanticModel semanticModel, List<string> requiredNamespaces)
    {
        if (attribute.ArgumentList == null) return string.Empty;

        var arguments = attribute.ArgumentList.Arguments
            .Select(arg => SerializeAttributeArgument(arg, semanticModel, requiredNamespaces))
            .ToList();

        return arguments.Any() ? $"({string.Join(", ", arguments)})" : string.Empty;
    }

    string SerializeAttributeArgument(AttributeArgumentSyntax argument, SemanticModel semanticModel, List<string> requiredNamespaces)
    {
        // Handle enum values (before constant values,
        // because enum values are also constant values)
        var typeInfo = semanticModel.GetTypeInfo(argument.Expression);
        if (typeInfo.Type?.TypeKind == TypeKind.Enum)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(argument.Expression);
            if (symbolInfo.Symbol is IFieldSymbol enumField)
            {
                var enumTypeName = enumField.ContainingType.Name;
                var enumTypeNamespace = enumField.ContainingNamespace.ToDisplayString();
                requiredNamespaces.Add(enumTypeNamespace);

                return argument.NameEquals != null
                    ? $"{argument.NameEquals.Name} = {enumTypeName}.{enumField.Name}"
                    : $"{enumTypeName}.{enumField.Name}";
            }
        }
        
        var constantValue = semanticModel.GetConstantValue(argument.Expression);
        if (constantValue.HasValue)
        {
            return argument.NameEquals != null
                ? $"{argument.NameEquals.Name} = {FormatConstantValue(constantValue.Value!)}"
                : FormatConstantValue(constantValue.Value!);
        }

        return argument.ToString();
    }

    string FormatConstantValue(object value)
    {
        if (value == null) return "null";
        if (value is string stringValue) return $"\"{stringValue}\"";
        if (value is bool boolValue) return boolValue ? "true" : "false";
        return value.ToString();
    }
}