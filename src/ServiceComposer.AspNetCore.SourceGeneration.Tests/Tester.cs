using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NuGet.Frameworks;

namespace ServiceComposer.AspNetCore.SourceGeneration.Tests;

static class StringExtensions
{
    public static string NormalizeLineEndings(this string text)
    {
        return text.Replace("\r\n", "\n").Replace("\r", "\n");
    }
}

public class Tester
{
    static (Version Version, string Name) GetTargetFramework()
    {
        var targetFrameworkAttribute = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<TargetFrameworkAttribute>();
        Debug.Assert(targetFrameworkAttribute?.FrameworkName != null, "targetFrameworkAttribute?.FrameworkName != null");
        var framework = new FrameworkName(targetFrameworkAttribute.FrameworkName);
        return (framework.Version, targetFrameworkAttribute.FrameworkName);
    }
    
    static Compilation CreateCompilation(string source)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references:
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(CompositionHandlerAttribute).Assembly.Location)
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        
        var references = new List<MetadataReference>();
        
        var trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")?.ToString()?.Split(Path.PathSeparator);
        if (trustedPlatformAssemblies != null)
        {
            var frameworkReferences = trustedPlatformAssemblies
                .Where(r => Path.GetFileName(r).StartsWith("System.") || 
                            Path.GetFileName(r) == "mscorlib.dll" ||
                            Path.GetFileName(r) == "netstandard.dll")
                .Select(p => MetadataReference.CreateFromFile(p));
            references.AddRange(frameworkReferences);
        }
        
        // The ASP.NET Core shared framework is installed next to the .NET one the tests
        // run on, e.g. <dotnet root>/shared/Microsoft.NETCore.App/10.0.x and
        // <dotnet root>/shared/Microsoft.AspNetCore.App/10.0.x
        var netCoreAppPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var aspNetCorePath = Path.GetFullPath(Path.Combine(netCoreAppPath, "..", "..", "Microsoft.AspNetCore.App"));
        if (!Directory.Exists(aspNetCorePath))
        {
            throw new DirectoryNotFoundException($"Could not find ASP.NET Core shared framework directory: {aspNetCorePath}");
        }

        if (Directory.Exists(aspNetCorePath))
        {
            var targetFrameworkVersion = GetTargetFramework();
            var latestPatch = Directory.GetDirectories(aspNetCorePath)
                .Select(d => new Version(Path.GetFileName(d)))
                .Where(v => v.Major == targetFrameworkVersion.Version.Major)
                .Max();

            Debug.Assert(latestPatch != null, nameof(latestPatch) + " != null");
            var aspNetCoreAssemblies = Directory.GetFiles(
                Path.Combine(aspNetCorePath, latestPatch.ToString()),
                "*.dll"
            );
            
            foreach (var assembly in aspNetCoreAssemblies)
            {
                references.Add(MetadataReference.CreateFromFile(assembly));
            }
        }

        return compilation.AddReferences(references);
    }

    [Fact]
    public async Task TestSourceGenerator()
    {
        var testFilesPath = Path.Combine(Directory.GetCurrentDirectory(), "TestFiles.CompositionHandlers");
        var allTestFiles = Directory.GetFiles(testFilesPath, "*.cs", SearchOption.TopDirectoryOnly);

        var generator = new CompositionHandlerWrapperGenerator();
        foreach (var testFile in allTestFiles)
        {
            var inputCode = await File.ReadAllTextAsync(testFile);
            var compilation = CreateCompilation(inputCode);
            
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, 
                out _, 
                out var diagnostics);
            
            var runResult = driver.GetRunResult();
            Assert.Empty(diagnostics);
            
            var generatedSource = runResult.GeneratedTrees[0].ToString();

            var targetFramework = GetTargetFramework();

            var nugetFramework = NuGetFramework.Parse(targetFramework.Name);
            var shortFolderName = nugetFramework.GetShortFolderName();
            
            await Verify(generatedSource.NormalizeLineEndings())
                .UseDirectory("ApprovedFiles")
                .UseFileName($"{Path.GetFileName(testFile)}.{shortFolderName}");
        }
    }
}