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
    
    static bool IsManagedAssembly(string path)
    {
        try
        {
            AssemblyName.GetAssemblyName(path);
            return true;
        }
        catch (BadImageFormatException)
        {
            return false;
        }
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
            
            // On Windows, the shared framework also contains native libraries,
            // e.g. aspnetcorev2_inprocess.dll, that can't be referenced.
            foreach (var assembly in aspNetCoreAssemblies.Where(IsManagedAssembly))
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
    static string ReadComponentsProvidersTestFile(string fileName) =>
        File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "TestFiles.ComponentsProviders", fileName));

    static (GeneratorDriverRunResult RunResult, Compilation OutputCompilation, IReadOnlyList<Diagnostic> Diagnostics) RunComponentsProviderGenerator(string source)
    {
        // The wrapper generator runs too: the provider must reference the
        // wrappers it generates for contract-less composition handlers.
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CompositionHandlerWrapperGenerator(),
            new CompositionComponentsProviderGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(CreateCompilation(source),
            out var outputCompilation,
            out var diagnostics);

        return (driver.GetRunResult(), outputCompilation, diagnostics);
    }

    [Fact]
    public async Task TestComponentsProviderGenerator()
    {
        var (runResult, outputCompilation, diagnostics) = RunComponentsProviderGenerator(ReadComponentsProvidersTestFile("AllComponentKinds.cs"));

        Assert.Empty(diagnostics);
        Assert.Empty(outputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

        var generatedSource = runResult.GeneratedTrees
            .Single(t => t.FilePath.EndsWith("GeneratedCompositionComponentsProvider.g.cs"))
            .ToString();

        var nugetFramework = NuGetFramework.Parse(GetTargetFramework().Name);
        await Verify(generatedSource.NormalizeLineEndings())
            .UseDirectory("ApprovedFiles")
            .UseFileName($"ComponentsProviders.AllComponentKinds.cs.{nugetFramework.GetShortFolderName()}");
    }

    [Fact]
    public void No_components_provider_is_generated_for_assemblies_without_components()
    {
        var (runResult, _, diagnostics) = RunComponentsProviderGenerator(ReadComponentsProvidersTestFile("NoComponents.cs"));

        Assert.Empty(diagnostics);
        Assert.Empty(runResult.GeneratedTrees);
    }

    [Theory]
    [InlineData("Sales", "Sales")]
    [InlineData("Sales.ViewModelComposition", "Sales.ViewModelComposition")]
    [InlineData("My-Service.1Api", "My_Service._1Api")]
    [InlineData("Company.class", "Company.@class")]
    public void Provider_namespace_is_a_valid_identifier(string assemblyName, string expectedNamespace)
    {
        Assert.Equal(expectedNamespace, CompositionComponentsProviderGenerator.ToNamespace(assemblyName));
    }
}