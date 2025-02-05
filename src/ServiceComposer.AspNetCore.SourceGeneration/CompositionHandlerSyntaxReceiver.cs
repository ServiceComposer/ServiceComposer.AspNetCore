using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ServiceComposer.AspNetCore.SourceGeneration;

public class CompositionHandlerSyntaxReceiver : ISyntaxContextReceiver
{
    readonly HashSet<string> supportedAttributes = [
        "HttpGet", "HttpPost", "HttpPatch", "HttpPut", "HttpDelete",
        "HttpGetAttribute", "HttpPostAttribute", "HttpPatchAttribute", "HttpPutAttribute", "HttpDeleteAttribute"
    ];
    public List<(MethodDeclarationSyntax Method, AttributeSyntax[] HttpAttributes, string Namespace, List<string> UserClassesHierarchy)> CompositionHandlerMethods { get; } = [];

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (
            // We're looking for methods with attributes
            context.Node is not MethodDeclarationSyntax { AttributeLists.Count: > 0 } methodDeclaration
            ||
            // We don't want to run against generated files. We could end up in an infinite loop
            methodDeclaration.SyntaxTree.FilePath.EndsWith(".g.cs"))
        {
            return;
        }
            
        var httpAttributes = methodDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .Where(attributeSyntax => supportedAttributes.Contains(attributeSyntax.Name.ToString()))
            .ToArray();

        var userClassNamespace = GetNamespace(methodDeclaration);
        var userClassesHierarchy = GetUserClassesHierarchy(methodDeclaration);

        // TODO How are conventions shared with ServiceComposer?
        //   somehow this conventions must be shared with ServiceComposer
        //   that uses them to register user types in the IoC container
        var namespaceMatchesConventions = userClassNamespace != null ? userClassNamespace == "CompositionHandlers" || userClassNamespace.EndsWith(".CompositionHandlers") : false;
        var classNameMatchesConventions = userClassesHierarchy.Last().EndsWith("CompositionHandler");
        var isTaskReturnType = methodDeclaration.ReturnType.ToString() == "Task";
        var isMethodPublic = methodDeclaration.Modifiers.Any(m=>m.Text != "private");

        if (isMethodPublic && httpAttributes.Any() && namespaceMatchesConventions && classNameMatchesConventions && isTaskReturnType)
        {
            CompositionHandlerMethods.Add((methodDeclaration, httpAttributes, userClassNamespace!, userClassesHierarchy));
        }
    }

    static List<string> GetUserClassesHierarchy(MethodDeclarationSyntax methodSyntax)
    {
        var result = new List<string>();
        var potentialClassParent = methodSyntax.Parent;
        while (potentialClassParent != null)
        {
            if (potentialClassParent is ClassDeclarationSyntax classDeclarationSyntax)
            {
                result.Add(classDeclarationSyntax.Identifier.Text);
            }
            
            potentialClassParent = potentialClassParent.Parent;
        }

        result.Reverse();
        
        return result;
    }

    // TODO nested namespaces are not supported
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