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
    static Compilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references:
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
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