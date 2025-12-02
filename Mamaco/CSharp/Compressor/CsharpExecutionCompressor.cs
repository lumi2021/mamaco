using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tq.Realizeer.Core.Program.Builder;
using Tq.Realizeer.Core.Program.Member;
using Tq.Realizer.Core.Builder.Execution.Omega;
using Tq.Realizer.Core.Builder.Language.Omega;
using Tq.Realizer.Core.Builder.References;
using Tq.Realizer.Core.Intermediate.Values;
using static Tq.Realizer.Core.Builder.Language.Omega.OmegaInstructions;

namespace Mamaco;

public partial class CSharpCompressorUnit
{
    
    public void ProccessBodies()
    {
        foreach (var (symbol, builder) in _symbolsMap_2)
        {
        
            SyntaxNode? body;
            
            switch (symbol)
            {
                case INamespaceOrTypeSymbol: continue;
                
                case IMethodSymbol { MethodKind: MethodKind.Constructor } methodSymbol:
                {
                    var node = symbol.DeclaringSyntaxReferences.ToArray().FirstOrDefault();
                    var ctorDeclSyntax = node?.GetSyntax() as ConstructorDeclarationSyntax;
                    body = (SyntaxNode?)ctorDeclSyntax?.Body ?? ctorDeclSyntax?.ExpressionBody!.Expression;
                    if (node == null) continue;
                    
                    var func = (RealizerFunction)builder;
                    var cell = func.AddOmegaCodeCell("entry");
                    Dictionary<ISymbol, int> localsMap = [];
        
                    for (var i = 0; i < methodSymbol.Parameters.Length; i++)
                        localsMap.Add(methodSymbol.Parameters[i], -i - 1);

                    var instancet = TypeOf(methodSymbol.ReceiverType!);
                    var instancep = func.AddParameter(".instance", instancet, 0);
                    
                    var s = node.GetSyntax();
                    switch (s)
                    {
                        case ClassDeclarationSyntax @classDec:
                        {
                            // TODO
                            var a = classDec.BaseList;
                        } break;

                        case ConstructorDeclarationSyntax @constructorDec:
                        {
                            if (constructorDec.Initializer != null)
                            {
                                var baseRef = (RealizerFunction)SymbolsMap(RefOf(constructorDec.Initializer));
                                
                                List<IOmegaExpression> args = [];
                                var baseargs = constructorDec.Initializer.ArgumentList.Arguments;
                                
                                args.Add(new Argument(instancep));
                                args.AddRange(baseargs.Select((t, i) => ParseExpression(
                                    t.Expression, ref cell, [],
                                    expectedType: baseRef.Parameters[i].Type)));

                                cell.Writer.Call(null, new Member(baseRef), [.. args]);
                            }
                        } break;
                        
                        default: throw new UnreachableException();
                    }

                    if (body != null)
                    {
                        if (body is BlockSyntax @b) ParseBlock(b, ref cell, localsMap);
                        else
                        {
                            var x = ParseExpression((ExpressionSyntax)body, ref cell, localsMap);
                            if (x != null!) cell.Writer.Ret(x);
                        }
                    
                    }
                    
                    if (!cell.IsFinished()) cell.Writer.Ret();
                } break;
                
                case IMethodSymbol methodSymbol:
                {
                    var node = symbol.DeclaringSyntaxReferences.ToArray().FirstOrDefault();
                    var methodDeclSyntax = node?.GetSyntax() as MethodDeclarationSyntax;
                    body = (SyntaxNode?)methodDeclSyntax?.Body ?? methodDeclSyntax?.ExpressionBody!.Expression;
                    if (node == null) continue;
                    
                    var func = (RealizerFunction)builder;
                    Dictionary<ISymbol, int> localsMap = [];
        
                    for (var i = 0; i < methodSymbol.Parameters.Length; i++)
                        localsMap.Add(methodSymbol.Parameters[i], -i - 1);
                    
                    if (body != null)
                    {
                        var cell = func.AddOmegaCodeCell("entry");
                        
                        if (body is BlockSyntax @b) ParseBlock(b, ref cell, localsMap);
                        else
                        {
                            var x = ParseExpression((ExpressionSyntax)body, ref cell, localsMap);
                            if (x != null) cell.Writer.Ret(x);
                        }
                    
                        if (!cell.IsFinished()) cell.Writer.Ret();
                    }
                } break;
        
                case IFieldSymbol fieldSymbol:
                {
                    var node = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                    body = (node as VariableDeclaratorSyntax)?.Initializer?.Value;
                    if (body == null) continue;
                    
                    ((RealizerField)builder).Initializer = ParseConstantValue(body, ((RealizerField)builder).Type);
                } break;
                
                case IPropertySymbol propertySymbol:
                {
                    if (_backingField.TryGetValue(propertySymbol, out var field))
                    {
                        var pbuilder = (RealizerProperty)builder;
                        var getter = pbuilder.Getter;
                        if (getter != null) {
                            var block = getter.AddOmegaCodeCell("entry");
                            if (pbuilder.Static) block.Writer.Ret(new Member(field));
                            else block.Writer
                                .Ret(new Access(new Self(), new Member(field)));
                        }
                        
                        var setter = pbuilder.Setter;
                        if (setter != null) {
                            var block = setter!.AddOmegaCodeCell("entry");
                            if (pbuilder.Static)
                            {
                                block.Writer
                                    .Assignment(
                                        new Member(field),
                                        new Argument(setter.Parameters[0]))
                                    .Ret();
                            }
                            else
                            {
                                block.Writer
                                    .Assignment(
                                        new Access(new Self(), new Member(field)),
                                        new Argument(setter.Parameters[0]))
                                    .Ret();
                            }
                        }
                    }
                    else
                    {
                        var node = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                        body = (node as PropertyDeclarationSyntax)?.Initializer?.Value;
                        if (body == null) continue;
                    
                        ((RealizerProperty)builder).Initializer = ParseConstantValue(body);
                    }
                } break;
                
                //AccessorDeclarationSyntax accessorDecl => accessorDecl.Body,
                //LocalFunctionStatementSyntax localFuncDecl => localFuncDecl.Body,
                
                default: throw new UnreachableException();
            };
        }
    }
    
    private void ParseBlock(BlockSyntax node, ref OmegaCodeCell cell,  Dictionary<ISymbol, int> localsMap)
    {
        foreach (var s in node.Statements)
            ParseStatement(s, ref cell, localsMap);
    }
    private void ParseStatement(StatementSyntax node, ref OmegaCodeCell cell, Dictionary<ISymbol, int> localsMap)
    {
        switch (node)
        {
            case LocalDeclarationStatementSyntax localDeclaration:
            {
                var csVarType = RefOf(localDeclaration.Declaration.Type);
                foreach (var variable in localDeclaration.Declaration.Variables)
                {
                    var csVarVal = variable.Initializer?.Value;
                    var csVarInt = localsMap.Count;
                    var csVarSymbol = (ILocalSymbol)RefOf(variable);
                    
                    localsMap.Add(csVarSymbol, csVarInt);
                    
                    if (csVarVal == null) continue;
                    
                    cell.Writer.Assignment(
                        new Register(TypeOf(csVarSymbol.Type), (ushort)csVarInt),
                        ParseExpression(csVarVal, ref cell, localsMap));
                }
            } return;
            
            case ReturnStatementSyntax ret:
                cell.Writer.Ret(ret.Expression == null ? null 
                    : ParseExpression(ret.Expression, ref cell, localsMap));
                return;
            
            case ExpressionStatementSyntax exp:
                ParseExpression(exp.Expression, ref cell, localsMap);
                return;
            
            default: throw new UnreachableException();
        }
    }

    private IOmegaExpression ParseExpression(
        ExpressionSyntax node,
        ref OmegaCodeCell cell,
        Dictionary<ISymbol, int> localsMap,
        
        TypeReference? expectedType = null,
        bool instanceRelative = false)
    {
        switch (node)
        {
            case AssignmentExpressionSyntax:
                // Technically this is a fake expression
                ParseExpression_Assingment(node, ref cell, localsMap);
                return null!;
            
            case ThrowExpressionSyntax @throw:
                // Fake expression again
                cell.Writer.Throw(null!);
                return null!;
            
            case InvocationExpressionSyntax @invocation:
            {
                var exp = invocation.Expression;
                var args = invocation.ArgumentList.Arguments;
        
                var callee = ParseExpression(exp, ref cell, localsMap);
                var fRetType = callee.Type;
                
                List<IOmegaExpression> argsList = [];
                foreach (var i in args)
                    argsList.Add(ParseExpression(i.Expression, ref cell, localsMap));

                if (fRetType == null)
                {
                    cell.Writer.Call(null, (IOmegaCallable)callee, [.. argsList]);
                    return null!;
                }

                return new Call(fRetType, (IOmegaCallable)callee, [.. argsList]);
            }
        
            case LiteralExpressionSyntax @lit:
            {
                var v = ParseConstantValue(lit, expectedType);
                return new Constant(v);
            }
        
            case ImplicitObjectCreationExpressionSyntax @iobj:
            case ObjectCreationExpressionSyntax @obj:
                throw new NotImplementedException();
        
            case MemberAccessExpressionSyntax memberAccess:
            {
                var left = memberAccess.Expression;
                var right = memberAccess.Name;

                if (left is IdentifierNameSyntax)
                    return ParseExpression(right, ref cell, localsMap);
                
                return new Access(
                    ParseExpression(left, ref cell, localsMap),
                    ParseExpression(right, ref cell, localsMap, instanceRelative: true)
                );
            }
            
            case SimpleNameSyntax simpleName:
            {
                var memberSymbol = RefOf(simpleName);

                IOmegaExpression? accessLeft = null;
                IOmegaExpression? accessRight = null;
                
                if (!instanceRelative && memberSymbol
                                          is IMethodSymbol
                                          or IPropertySymbol
                                          or IFieldSymbol
                                          or IEventSymbol
                                      && !memberSymbol.IsStatic) accessLeft = new Self();

                accessRight = memberSymbol switch
                {
                    IMethodSymbol @method => new Member(SymbolsMap(method)),
                    IFieldSymbol @field => new Member(SymbolsMap(field)),
                    IPropertySymbol @prop => new Member(SymbolsMap(prop)),
                    
                    IParameterSymbol @arg => new Argument(SymbolsMap(arg)),
                    
                    _ => throw new UnreachableException()
                };

                return accessLeft == null ? accessRight : new Access(accessLeft, accessRight);
            }
            
            case ThisExpressionSyntax:
                return new Self();

            case BinaryExpressionSyntax @bin:
            {
                
                var r = (IMethodSymbol)RefOf(bin);
                var t = TypeOf(TypeOf(bin));
                
                var expl = ParseExpression(bin.Left, ref cell, localsMap, expectedType: t);
                var expr = ParseExpression(bin.Right, ref cell, localsMap, expectedType: t);
                
                if (r.MethodKind != MethodKind.BuiltinOperator)
                    return new Call(t, new Member(SymbolsMap(r)), [expl, expr]);
                 
                return bin.Kind() switch
                {
                    SyntaxKind.AddExpression => new Add(t, expl, expr),
                    
                    _ => throw new UnreachableException()
                };
            }
            
            default: throw new UnreachableException();
        }
    }

    
    private void ParseExpression_Assingment(ExpressionSyntax node, ref OmegaCodeCell cell, Dictionary<ISymbol, int> localsMap)
    {
        switch (node)
        {
            case AssignmentExpressionSyntax assig:
            {
                
                var target = assig.Left;
                var val = assig.Right;
                
                var r = (IMethodSymbol)RefOf(assig);
                var t = TypeOf(TypeOf(assig));
                
                var expl = (IOmegaAssignable)ParseExpression(target, ref cell, localsMap);
                var expr = ParseExpression(val, ref cell, localsMap, expectedType: t);
                
                if (assig.Kind() == SyntaxKind.SimpleAssignmentExpression)
                {
                    cell.Writer.Assignment(expl, expr);
                    return;
                }
                
                
                if (r.MethodKind != MethodKind.BuiltinOperator)
                {
                    cell.Writer.Assignment(expl, new Call(t, new Member(SymbolsMap(r)), [expl, expr]));
                    return;
                }
                
                switch (assig.Kind())
                {
                    case SyntaxKind.AddAssignmentExpression:
                        cell.Writer.Assignment(expl, new Add(t, expl, expr)); break;
                    
                    case SyntaxKind.MultiplyAssignmentExpression:
                        cell.Writer.Assignment(expl, new Mul(t, expl, expr)); break;

                    default: throw new UnreachableException();
                };
            } break;
            
            default: throw new UnreachableException();
        }
    }

    private RealizerConstantValue ParseConstantValue(object? value, TypeReference? type = null)
    {
        switch (type)
        {
            case not null when value is LiteralExpressionSyntax:
            case null: break;
            
            case IntegerTypeReference @i:
                return value switch
                {
                    sbyte @v => new IntegerConstantValue(i.Bits, v),
                    byte @v => new IntegerConstantValue(i.Bits, v),
                    short @v => new IntegerConstantValue(i.Bits, v),
                    ushort @v => new IntegerConstantValue(i.Bits, v),
                    int @v => new IntegerConstantValue(i.Bits, v),
                    uint @v => new IntegerConstantValue(i.Bits, v),
                    long @v => new IntegerConstantValue(i.Bits, v),
                    ulong @v => new IntegerConstantValue(i.Bits, v),
                    Int128 @v => new IntegerConstantValue(i.Bits, v),
                    UInt128 @v => new IntegerConstantValue(i.Bits, v),
                    _ => throw new UnreachableException()
                };
            
        } // fallback when default
        
        return value switch
        {
            null => new NullConstantValue(type ?? throw new ArgumentNullException(nameof(type))),

            bool @v => new IntegerConstantValue(1, v ? 1 : 0),
            
            sbyte @v => new IntegerConstantValue(8, v),
            byte @v => new IntegerConstantValue(8, v),
            short @v => new IntegerConstantValue(16, v),
            ushort @v => new IntegerConstantValue(16, v),
            int @v => new IntegerConstantValue(32, v),
            uint @v => new IntegerConstantValue(32, v),
            long @v => new IntegerConstantValue(64, v),
            ulong @v => new IntegerConstantValue(64, v),
            Int128 @v => new IntegerConstantValue(128, v),
            UInt128 @v => new IntegerConstantValue(128, v),

            string @s => new SliceConstantValue(
                new IntegerTypeReference(false, 8), Encoding.UTF8.GetBytes(s)
                    .Select(e => new IntegerConstantValue(8, e)).ToArray<RealizerConstantValue>()),
            
            LiteralExpressionSyntax @lit => ParseConstantValue(lit.Token.Value, type),
            
            _ => throw new UnreachableException()
        };
    }


    bool HasBody(IMethodSymbol method)
    {
        var syntaxRef = method.DeclaringSyntaxReferences.FirstOrDefault();
        var syntax = syntaxRef?.GetSyntax();
        return syntax is AccessorDeclarationSyntax { Body: not null };
    }
    
    private ITypeSymbol TypeOf(SyntaxNode node) => _compilation.GetSemanticModel(node.SyntaxTree).GetTypeInfo(node).Type!;
    private ISymbol RefOf(SyntaxNode node) => _compilation.GetSemanticModel(node.SyntaxTree).GetSymbolInfo(node).Symbol!;
    private ISymbol RefOf(VariableDeclaratorSyntax var) => ModelExtensions.GetDeclaredSymbol(_compilation.GetSemanticModel(var.SyntaxTree!), var)!;
    
    private TypeReference TypeOf(ITypeSymbol typeSymbol)
    {
        IEnumerable<ISymbol> globalParts = [..typeSymbol.ContainingNamespace.ConstituentNamespaces, typeSymbol];
        var global = string.Join('.', globalParts.Select(e => e.Name));

        // Check for the builtin types
        switch (global)
        {
            case "System.Void": return null!;
            case "System.Boolean": return new IntegerTypeReference(false, 1);
            
            case "System.Byte": return new IntegerTypeReference(false, 8);
            case "System.SByte": return new IntegerTypeReference(true, 8);
            case "System.Int16": return new IntegerTypeReference(true, 16);
            case "System.UInt16": return new IntegerTypeReference(false, 16);
            case "System.Int32": return new IntegerTypeReference(true, 32);
            case "System.UInt32": return new IntegerTypeReference(false, 32);
            case "System.Int64": return new IntegerTypeReference(true, 64);
            case "System.UInt64": return new IntegerTypeReference(false, 64);
            case "System.Int128": return new IntegerTypeReference(true, 128);
            case "System.UInt128": return new IntegerTypeReference(false, 128);
            
            case "System.String": return new SliceTypeReference(new IntegerTypeReference(false, 8));
        }

        RealizerMember langmember;
        
        if (typeSymbol is INamedTypeSymbol @nts) langmember = SymbolsMap(nts.OriginalDefinition);
        else langmember = SymbolsMap(typeSymbol);

        var b = langmember switch
        {
            RealizerStructure @struc => new NodeTypeReference(struc),
            RealizerTypedef @typedef => new NodeTypeReference(typedef),
            
            _ => throw new UnreachableException()
        };
        
        return typeSymbol.IsValueType ? b : new ReferenceTypeReference(b);
    }

}