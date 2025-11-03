using Mamaco;
using Microsoft.CodeAnalysis;

string code = File.ReadAllText("../../../../test-code/main.cs");

var compiler = new CSharpCompiler();
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


var globalnamespace = compiler.Compilation.GlobalNamespace;

var symbol_tree = GenerateSymbolTree(globalnamespace);

Console.WriteLine("Build Finished Successfully!");
return;

SymbolTreeNode GenerateSymbolTree(ISymbol symbol)
{
    var totallyQualifiedName = symbol.Name;
    var cur = symbol.ContainingSymbol;
    while (cur is { IsImplicitlyDeclared: false })
    {
        if (!string.IsNullOrEmpty(cur.Name))
            totallyQualifiedName = cur.Name + "." + totallyQualifiedName;
        else
            totallyQualifiedName = cur + "." + totallyQualifiedName;    
        
        cur = cur.ContainingSymbol;
    }
    
    switch (symbol) {
        case INamespaceOrTypeSymbol @nmsp:
            
            List<SymbolTreeNode> symbols = [];
            foreach (var i in nmsp.GetMembers()) 
                symbols.Add(GenerateSymbolTree(i));

            return new SymbolTreeNode
            {
                global = totallyQualifiedName,
                symbol = symbol,
                children = symbols,
            };
            
        default:
            return new SymbolTreeNode
            {
                global = totallyQualifiedName,
                symbol = symbol,
                children = null,
            };
    }
}

struct SymbolTreeNode
{
    public string global;
    public ISymbol symbol;
    public List<SymbolTreeNode>? children;

    public readonly override string ToString() => global;
}
