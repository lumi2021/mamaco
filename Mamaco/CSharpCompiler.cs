using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mamaco;

public class CSharpCompiler
{

    private List<(SyntaxTree, SyntaxTree, (int, int)[])> sources = [];
    private CSharpCompilation compilation = null!;
    
    public CSharpCompilation Compilation => compilation;
    
    public void Parse(string source)
    {
        var parseOptions = new CSharpParseOptions(
            LanguageVersion.Latest,
            DocumentationMode.Parse,
            SourceCodeKind.Regular,
            []
        );

        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var root = syntaxTree.GetRoot();
        
        var rewriter = new SyntaxPreprocessor();
        var newRoot = rewriter.Visit(root);
        var newTree = CSharpSyntaxTree.Create((CompilationUnitSyntax)newRoot);
        
        sources.Add((syntaxTree, newTree, rewriter.GetSpanShift()));
    }
    
    public void Compile()
    {

        var system_path = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "System.Private.CoreLib.dll");
        
        compilation = CSharpCompilation.Create(
            "Test",
            sources.Select(e => e.Item2),
            [MetadataReference.CreateFromFile(system_path)],
            new CSharpCompilationOptions(
                outputKind: OutputKind.ConsoleApplication,
                allowUnsafe: true)
        );
    }

    public ImmutableArray<Diagnostic> GetDiagnostics()
    {
        var processed = new List<Diagnostic>();
        var diagnostics = compilation.GetDiagnostics();

        foreach (var diagnostic in diagnostics)
        {
            var oldLocation = diagnostic.Location;
            var tree = oldLocation.SourceTree;

            var begin = oldLocation.SourceSpan.Start;
            var newBegin = oldLocation.SourceSpan.Start;
            var newEnd = oldLocation.SourceSpan.End;

            if (tree != null)
            {
                var treeData = sources.Find(e => e.Item2 == tree);
                var treeSpams = treeData.Item3;
                foreach (var (spam_start, spam_offset) in treeSpams)
                {
                    if (spam_start >= begin) continue;
                    newBegin -= spam_offset;
                    newEnd -= spam_offset;
                }

                if (newBegin < 0) newBegin = 0;
                if (newEnd < 0) newEnd = 0;

                var newloc = Location.Create(
                    treeData.Item1,
                    TextSpan.FromBounds(newBegin, newEnd));

                var d = Diagnostic.Create(
                    diagnostic.Descriptor,
                    newloc,
                    diagnostic.GetMessage(),
                    diagnostic.Properties,
                    diagnostic.Severity);

                processed.Add(d);
            }
            else processed.Add(diagnostic);
        }
        
        return [..processed];
    }
    
    private class SyntaxPreprocessor : CSharpSyntaxRewriter
    {
        private List<(int beguin, int offset)> spanShift = [];
        
        public (int beguin, int offset)[] GetSpanShift() => [.. spanShift];
        
        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.UnsafeKeyword)))
                return base.VisitClassDeclaration(node)!;

            var oldSpan = node.Modifiers.Span;
            
            var newModifiers = node.Modifiers.Add(SyntaxFactory.Token(
                SyntaxKind.UnsafeKeyword));
            node = node.WithModifiers(newModifiers);

            var newSpan = node.Modifiers.Span;

            var beggin = oldSpan.Start;
            var offset = newSpan.End - oldSpan.End;
            spanShift.Add((beggin, offset));

            return base.VisitClassDeclaration(node)!;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.UnsafeKeyword)))
                return base.VisitMethodDeclaration(node)!;

            var oldSpan = node.Modifiers.Span;
            
            var newModifiers = node.Modifiers.Add(
                SyntaxFactory.Token(SyntaxKind.UnsafeKeyword));
            node = node.WithModifiers(newModifiers);

            var newSpan = node.Modifiers.Span;

            var beggin = oldSpan.Start;
            var offset = newSpan.End - oldSpan.End;
            spanShift.Add((beggin, offset));

            return base.VisitMethodDeclaration(node)!;
        }
        
    }
}

