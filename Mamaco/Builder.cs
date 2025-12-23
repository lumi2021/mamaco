using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Tq.Realizer.Core.Program;
using Tq.Realizer.Core.Program.Builder;
using Tq.Realizer;
using Tq.Realizer.Core.Builder;

namespace Mamaco;

public static class Builder
{
    [DoesNotReturn]
    public static void Build(string[] args)
    {
        Directory.SetCurrentDirectory("../../../../test-code");
        var baseDir = Directory.GetCurrentDirectory();
        var exeDir = AppContext.BaseDirectory;

        ClearLocal();
        
        List<(string src, string path)> sources = [];
        
        ListSources(baseDir, sources);
        ListSources(Path.Combine(exeDir, "Libs"), sources);

        Console.WriteLine($"{sources.Count} sources found");

        var compiler = new CSharpCompilerUnit();
        var compressor = new CSharpCompressorUnit();

        //compiler.AddImplicitGlobalNamespaces([
        //    "System",
        //]);
        
        foreach (var (i1, i2) in sources) compiler.Parse(i1, i2);
        compiler.Compile();

        var diagnostics = compiler.GetDiagnostics();

        foreach (var diag in diagnostics)
        {
            var location = diag.Location;
            var lineSpan = location.GetLineSpan().StartLinePosition;
            var file = (location.SourceTree?.FilePath) ?? "<no file>";
            var severity = diag.Severity;
            
            Console.WriteLine($"{severity} ({diag.Id}) {file} ({lineSpan}): {diag.GetMessage()}");
        }
 
        if (diagnostics.Any(e => e.Severity == DiagnosticSeverity.Error))
        {
            Console.WriteLine("\nBuild Failed!");
            Environment.Exit(1);
        }
        compiler.WipeSources();
        
        var realizerProgram = new RealizerProgram("placeholder");
        
        compressor.CompressModules(realizerProgram, compiler.Compilation.GlobalNamespace, compiler.Compilation);
        compressor.ProcessReferences();
        compressor.ProccessBodies();

        
        LoadModules();
        var realizerProcessor = new RealizerProcessor
        {
            Verbose = true,
            DebugDumpPath = Path.Combine(baseDir, ".abs-cache/debug/"),
        };

        var target = RealizerModules.Find(args[0]);
        realizerProcessor.SelectProgram(realizerProgram);
        realizerProcessor.SelectConfiguration(target.LanguageOutput);
        
        realizerProcessor.Start();
        realizerProgram = realizerProcessor.Compile();

        target.CompilerInvoke(realizerProgram, target.LanguageOutput);

        
        Console.WriteLine("Build Finished Successfully!");
    }

    private static void ListSources(string path, List<(string, string)> srcList)
    {
        var fullPath = Path.GetFullPath(path);
        if (!Directory.Exists(fullPath)) throw new DirectoryNotFoundException($"Directory {fullPath} does not exist");
        
        var libsSrc = Directory.EnumerateFiles(fullPath, "*.cs", SearchOption.AllDirectories);
        srcList.AddRange(libsSrc.Select( e => (File.ReadAllText(e), e)));
    }

    
    private static void LoadModules()
    {
        RealizerModules.ManualLoad(Path.Combine(AppContext.BaseDirectory, "Tq.Module.LLVM.dll"));
        RealizerModules.ManualLoad(Path.Combine(AppContext.BaseDirectory, "Tq.Module.CLR.dll"));
    }

    private static void ClearLocal()
    {
        string[] toCreate = [
        ".abs-out",
        ".abs-cache",
        ".abs-cache/debug",
        ];

        foreach (var i in toCreate)
        {
            if (Directory.Exists(i)) Directory.Delete(i, true);
            Directory.CreateDirectory(i);
        }
    }
}
