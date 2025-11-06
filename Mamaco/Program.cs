using System.Diagnostics;
using Mamaco;
using Microsoft.CodeAnalysis;
using Tq.Realizer.Builder;

var basedir = "../../../../test-code/";
var code = File.ReadAllText(Path.Combine(basedir, "main.cs"));

var compiler = new CSharpCompiler();
var compressor = new CSharpCompressor();
compiler.Parse(code);
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

var realizerProgram = new ProgramBuilder();

compressor.CompressModules(realizerProgram, compiler.Compilation.GlobalNamespace);
compressor.ProcessReferences();
compressor.ProcessFunctionBodies();


File.WriteAllText(Path.Combine(basedir, "program.txt"), realizerProgram.ToString());
Console.WriteLine("Build Finished Successfully!");

