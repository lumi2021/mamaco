using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mamaco;

public class CSharpCompilerUnit
{

    private readonly List<(SyntaxTree, SyntaxTree, (int, int)[])> _sources = [];
    private CSharpCompilation _compilation = null!;
    
    public CSharpCompilation Compilation => _compilation;
    
    public void Parse(string source, string path)
    {
        var parseOptions = new CSharpParseOptions(
            LanguageVersion.Latest,
            DocumentationMode.Parse,
            SourceCodeKind.Regular,
            []
        );

        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions, path);
        var root = syntaxTree.GetRoot();
        
        var rewriter = new SyntaxPreprocessor();
        var newRoot = rewriter.Visit(root);
        var newTree = CSharpSyntaxTree.Create((CompilationUnitSyntax)newRoot);
        
        _sources.Add((syntaxTree, newTree, rewriter.GetSpanShift()));
    }
    
    public void Compile()
    {
        _compilation = CSharpCompilation.Create(
            "Test",
            _sources.Select(e => e.Item2),
            [],
            new CSharpCompilationOptions(
                outputKind: OutputKind.ConsoleApplication,
                allowUnsafe: true,
                nullableContextOptions: NullableContextOptions.Annotations)
        );
        
        if (_compilation.GetDiagnostics().Any(e => e.Severity == DiagnosticSeverity.Error)) return;
        
        var rewriter = new SyntaxPostprocessor(this);
        for (var i = 0; i < _sources.Count; i++)
        {
            var source = _sources[i].Item2;
            
            var visited = (CSharpSyntaxNode)rewriter.Visit(source.GetRoot());
            var newTree = CSharpSyntaxTree.Create(visited, (CSharpParseOptions)source.Options);
            _compilation = _compilation.ReplaceSyntaxTree(source, newTree);
            _compilation.GetSemanticModel(newTree);

            var a = newTree.ToString();
            
            var sourceData = _sources[i];
            sourceData.Item2 = newTree;
            _sources[i] = sourceData;
        }
    }
    
    public void WipeSources() => _sources.Clear(); 
    
    public ImmutableArray<Diagnostic> GetDiagnostics()
    {
        var processed = new List<Diagnostic>();
        var diagnostics = _compilation.GetDiagnostics();

        foreach (var diagnostic in diagnostics)
        {
            var oldLocation = diagnostic.Location;
            var tree = oldLocation.SourceTree;

            var begin = oldLocation.SourceSpan.Start;
            var newBegin = oldLocation.SourceSpan.Start;
            var newEnd = oldLocation.SourceSpan.End;

            if (tree != null)
            {
                var treeData = _sources.Find(e => e.Item2 == tree);
                var treeSpams = treeData.Item3;
                foreach (var (spam_start, spam_offset) in treeSpams)
                {
                    if (spam_start >= begin) continue;
                    newBegin -= spam_offset;
                    newEnd -= spam_offset;
                }


                if (newBegin < 0) newBegin = 0;
                if (newEnd < 0) newEnd = 0;

                var newloc = Location.Create(treeData.Item1, TextSpan.FromBounds(newBegin, newEnd));

                var d = Diagnostic.Create(
                    new DiagnosticDescriptor(
                        diagnostic.Descriptor.Id,
                        diagnostic.Descriptor.Title,
                        diagnostic.GetMessage(),
                        diagnostic.Descriptor.Category,
                        diagnostic.Descriptor.DefaultSeverity,
                        diagnostic.Descriptor.IsEnabledByDefault),
                    
                    newloc,
                    [],
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
        private List<(int beguin, int offset)> _spanShift = [];
        public (int beguin, int offset)[] GetSpanShift() => [.. _spanShift];
        
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
            var offset =  oldSpan.End - newSpan.End;
            _spanShift.Add((beggin, offset));

            return base.VisitClassDeclaration(node)!;
        }

        public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
        {
            if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.UnsafeKeyword)))
                return base.VisitStructDeclaration(node)!;

            var oldSpan = node.Modifiers.Span;
            
            var newModifiers = node.Modifiers.Add(SyntaxFactory.Token(
                SyntaxKind.UnsafeKeyword));
            node = node.WithModifiers(newModifiers);

            var newSpan = node.Modifiers.Span;

            var beggin = oldSpan.Start;
            var offset =  oldSpan.End - newSpan.End;
            _spanShift.Add((beggin, offset));

            return base.VisitStructDeclaration(node)!;
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
            var offset = oldSpan.End - newSpan.End;
            _spanShift.Add((beggin, offset));

            return base.VisitMethodDeclaration(node)!;
        }

        public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.UnsafeKeyword)))
                return base.VisitConstructorDeclaration(node)!;

            var oldSpan = node.Modifiers.Span;
            
            var newModifiers = node.Modifiers.Add(
                SyntaxFactory.Token(SyntaxKind.UnsafeKeyword));
            node = node.WithModifiers(newModifiers);

            var newSpan = node.Modifiers.Span;

            var beggin = oldSpan.Start;
            var offset = oldSpan.End - newSpan.End;
            _spanShift.Add((beggin, offset));

            return base.VisitConstructorDeclaration(node)!;
        }
    }
    
    private class SyntaxPostprocessor(CSharpCompilerUnit parent) : CSharpSyntaxRewriter
    {

        private CSharpCompilerUnit _parent = parent;
        private CSharpCompilation Compilation => _parent._compilation;
    
        public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            if (node.Kind() == SyntaxKind.AddExpression)
            {
                var sema = Compilation.GetSemanticModel(node.SyntaxTree);
                var info = sema.GetTypeInfo(node).ConvertedType;
            
                if (info?.SpecialType == SpecialType.System_String)
                {
                    return SyntaxFactory.InvocationExpression(
                        
                        SyntaxFactory.ParseTypeName("global::System.String.Concat"),
                        
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList([
                                SyntaxFactory.Argument(node.Left),
                                SyntaxFactory.Argument(node.Right)
                            ])
                        )
                    );
                }
            }
        
            return base.VisitBinaryExpression(node);
        }
    }
}
