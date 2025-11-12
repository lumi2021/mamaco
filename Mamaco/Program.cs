using System.Diagnostics;
using Mamaco;
using Microsoft.CodeAnalysis;
using Tq.Realizer.Builder;

var basedir = "../../../../test-code/";
var exeDir = AppContext.BaseDirectory;

List<string> sources = [];

sources.Add(File.ReadAllText(Path.Combine(basedir, "main.cs")));
var libsSrc = Directory.EnumerateFiles(Path.Combine(exeDir, "Libs"), "*.cs", SearchOption.AllDirectories);
sources.AddRange(libsSrc.Select(File.ReadAllText));

var compiler = new CSharpCompiler();
var compressor = new CSharpCompressor();

foreach (var i in sources) compiler.Parse(i);
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

compressor.CompressModules(realizerProgram, compiler.Compilation.GlobalNamespace, compiler.Compilation);
compressor.ProcessReferences();
compressor.ProccessBodies();


File.WriteAllText(Path.Combine(basedir, "program.txt"), realizerProgram.ToString());
Console.WriteLine("Build Finished Successfully!");

