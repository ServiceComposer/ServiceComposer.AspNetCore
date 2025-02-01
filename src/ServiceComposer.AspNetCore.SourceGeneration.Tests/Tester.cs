using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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
    static Version GetTargetFrameworkVersion()
    {
        var targetFrameworkAttribute = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<TargetFrameworkAttribute>();
        var framework = new FrameworkName(targetFrameworkAttribute?.FrameworkName);
        return framework.Version;   
    }
    
    static Compilation CreateCompilation(string source)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references:
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        
        // Add basic runtime references
        var references = new List<MetadataReference>();
        
        // Add core framework reference
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
        
        string aspNetCorePath;
        if (OperatingSystem.IsWindows())
        {
            aspNetCorePath = Path.Combine(
                Environment.GetEnvironmentVariable("ProgramFiles") ?? @"C:\Program Files",
                "dotnet",
                "shared",
                "Microsoft.AspNetCore.App"
            );
        }
        else if (OperatingSystem.IsMacOS())
        {
            aspNetCorePath = "/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App";
            
            // Check for homebrew installation if default location doesn't exist
            if (!Directory.Exists(aspNetCorePath))
            {
                aspNetCorePath = "/opt/homebrew/share/dotnet/shared/Microsoft.AspNetCore.App";
            }
        }
        else if (OperatingSystem.IsLinux())
        {
            // Check common Linux installation paths
            var possiblePaths = new[]
            {
                "/usr/share/dotnet/shared/Microsoft.AspNetCore.App",
                "/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App",
                "/opt/dotnet/shared/Microsoft.AspNetCore.App"
            };
            
            aspNetCorePath = possiblePaths.FirstOrDefault(Directory.Exists) ?? 
                             throw new DirectoryNotFoundException("Could not find ASP.NET Core shared framework directory");
        }
        else
        {
            throw new PlatformNotSupportedException("Current operating system is not supported");
        }
        
        if (Directory.Exists(aspNetCorePath))
        {
            var targetFrameworkVersion = GetTargetFrameworkVersion();
            var latestPatch = Directory.GetDirectories(aspNetCorePath)
                .Select(d => new Version(Path.GetFileName(d)))
                .Where(v => v.Major == targetFrameworkVersion.Major)
                .Max();
            
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
            
            await Verify(generatedSource.NormalizeLineEndings())
                .UseDirectory("ApprovedFiles")
                .UseFileName(Path.GetFileName(testFile));
        }
    }
}