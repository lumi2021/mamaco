using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tq.Realizeer.Core.Program.Builder;
using Tq.Realizeer.Core.Program.Member;
using Tq.Realizer.Core.Builder.Execution;
using Tq.Realizer.Core.Builder.Execution.Omega;
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
            if (_intrinsincsMap_2.ContainsKey(symbol)) continue;
            
            SyntaxNode? body;
            
            switch (symbol)
            {
                case INamespaceOrTypeSymbol: continue;
                
                case IMethodSymbol { IsExtern: true } methodSymbol:
                {
                    var func = (RealizerFunction)builder;
                    func.Import(methodSymbol.Name);
                } break;
                
                case IMethodSymbol { MethodKind: MethodKind.Constructor } methodSymbol:
                {
                    var node = symbol.DeclaringSyntaxReferences.ToArray().FirstOrDefault();
                    var ctorDeclSyntax = node?.GetSyntax() as ConstructorDeclarationSyntax;
                    body = (SyntaxNode?)ctorDeclSyntax?.Body ?? ctorDeclSyntax?.ExpressionBody!.Expression;
                    if (node == null) continue;
                    
                    var func = (RealizerFunction)builder;
                    var cell = func.AddOmegaCodeCell("entry");
                    Dictionary<ILocalSymbol, Register> localsMap = [];
                    
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
                            if (constructorDec.Initializer == null ||
                                constructorDec.Initializer.ThisOrBaseKeyword.Text == "base")
                            {
                                cell.Writer.IntrinsicCall(IntrinsicFunctions.initFields);
                            }
                            
                            if (constructorDec.Initializer != null)
                            {
                                var baseRef = (RealizerFunction)SymbolsMap(RefOf(constructorDec.Initializer));
                                
                                List<IOmegaExpression> args = [];
                                var baseargs = constructorDec.Initializer.ArgumentList.Arguments;
                                
                                args.AddRange(baseargs.Select((t, i) => ParseExpression(
                                    t.Expression, ref cell, [], expectedType: baseRef.Parameters[i].Type)));

                                cell.Writer.Call(new Access(new Self(), new Member(baseRef)), [.. args]);
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
                    Dictionary<ILocalSymbol, Register> localsMap = [];
                    
                    if (body != null)
                    {
                        var cell = func.AddOmegaCodeCell("entry");
                        
                        if (body is BlockSyntax @b) ParseBlock(b, ref cell, localsMap);
                        else
                        {
                            var x = ParseExpression((ExpressionSyntax)body, ref cell, localsMap, asStatement: methodSymbol.ReturnsVoid);
                            if (methodSymbol.ReturnsVoid) cell.Writer.Ret();
                            else cell.Writer.Ret(x);
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
    
    private void ParseBlock(BlockSyntax node, ref OmegaCodeCell cell,  Dictionary<ILocalSymbol, Register> localsMap)
    {
        foreach (var s in node.Statements)
            ParseStatement(s, ref cell, localsMap);
    }
    private void ParseStatement(StatementSyntax node, ref OmegaCodeCell cell, Dictionary<ILocalSymbol, Register> localsMap)
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

                    var reg = new Register(TypeOf(csVarSymbol.Type), (ushort)csVarInt);
                    localsMap.Add(csVarSymbol, reg);
                    
                    if (csVarVal == null) continue;
                    cell.Writer.Assignment(reg, ParseExpression(csVarVal, ref cell, localsMap));
                }
            } return;
            
            case ReturnStatementSyntax ret:
                cell.Writer.Ret(ret.Expression == null ? null 
                    : ParseExpression(ret.Expression, ref cell, localsMap));
                return;

            case WhileStatementSyntax whileStatement:
            {
                var function = cell.Source;
                var condition = function.AddOmegaCodeCell("while.condition");
                var execution = function.AddOmegaCodeCell("while.execution");
                var brk = function.AddOmegaCodeCell("while.break");

                cell.Writer.Branch(condition);
                ref var curCell = ref condition;
                
                var conditionExpression = ParseExpression(whileStatement.Condition, ref curCell, localsMap);
                condition.Writer.CBranch(conditionExpression, execution, brk);

                curCell = ref execution;
                ParseStatement(whileStatement.Statement, ref curCell, localsMap);
                execution.Writer.Branch(condition);
                
                cell = brk;
            } return;
            
            case ExpressionStatementSyntax exp:
                ParseExpression(exp.Expression, ref cell, localsMap,
                    asStatement: true);
                return;
            
            default: throw new UnreachableException();
        }
    }

    private IOmegaExpression ParseExpression(
        ExpressionSyntax node,
        ref OmegaCodeCell cell,
        Dictionary<ILocalSymbol, Register> localsMap,
        
        bool asStatement = false,
        TypeReference? expectedType = null,
        bool instanceRelative = false,
        bool ignoreInstance = false)
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

                if (!asStatement) return new Call((IOmegaCallable)callee, [.. argsList]);
                
                cell.Writer.Call((IOmegaCallable)callee, [.. argsList]);
                return null!;
            }
        
            case LiteralExpressionSyntax @lit:
            {
                var v = ParseConstantValue(lit, expectedType);
                return new Constant(v);
            }
        
            case ImplicitObjectCreationExpressionSyntax @iobj:
                throw new NotImplementedException();
            case ObjectCreationExpressionSyntax @obj:
            {
                var aaaa = RefOf(obj);
                if (_intrinsincsMap_2.TryGetValue(RefOf(obj), out var intrinsinc))
                { 
                    List<IOmegaExpression> args = [];
                    foreach(var i in obj.ArgumentList.Arguments)
                       args.Add(ParseExpression(i.Expression, ref cell, localsMap));
                   
                    return new Call(new Member((RealizerFunction)SymbolsMap(RefOf(obj))), [..args]);
                }
                
                var constructor = (RealizerFunction)SymbolsMap(RefOf(obj));
                var objstruct = (RealizerStructure)constructor.Parent!;
                var objtype = new NodeTypeReference(objstruct);
                var objsymbol = (ITypeSymbol)SymbolsMap(objstruct);
                
                var reg = cell.Writer.GetNewRegister(new ReferenceTypeReference(objtype));
                    
                if (objsymbol.IsValueType) cell.Writer.Assignment(reg, new Alloca(objtype));
                else
                {
                    var mem = new Call(
                        new Member(SymbolsMap(IntrinsincsMap(IntrinsincElements.Function_RuntimeInteropServicesNativeMemoryAlignedAlloc))),
                        [
                            new Constant(new IntegerConstantValue(new IntegerTypeReference(false, 0), objtype.Length / 8)),
                            new Constant(new IntegerConstantValue(new IntegerTypeReference(false, 0), objtype.Alignment / 8))
                        ]);
                    cell.Writer.Assignment(reg, mem);
                }
                
                List<IOmegaExpression> argsList = [];
                foreach (var e in obj.ArgumentList!.Arguments)
                    argsList.Add(ParseExpression(e.Expression, ref cell, localsMap));
                    
                cell.Writer.Call(new Member(constructor), [reg, ..argsList]);
                return objsymbol.IsValueType ? new Val(reg) : reg;
            }
        
            case MemberAccessExpressionSyntax memberAccess:
            {
                var left = memberAccess.Expression;
                var right = memberAccess.Name;

                if (RefOf(right).IsStatic) return ParseExpression(right, ref cell, localsMap);
                
                return new Access(
                    ParseExpression(left, ref cell, localsMap),
                    ParseExpression(right, ref cell, localsMap, instanceRelative: true, ignoreInstance: true) as Member ?? throw new UnreachableException()
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
                    ILocalSymbol @local => localsMap[local],
                    
                    IParameterSymbol @arg => new Argument(SymbolsMap(arg)),
                    
                    
                    _ => throw new UnreachableException()
                };

                return accessLeft == null
                    ? accessRight
                    : new Access(accessLeft, accessRight as Member ?? throw new UnreachableException());
            }

            case QualifiedNameSyntax @qns:
            {
                var memberSymbol = RefOf(qns);
                return memberSymbol switch
                {
                    IMethodSymbol @method => new Member(SymbolsMap(method)),
                    IFieldSymbol @field => new Member(SymbolsMap(field)),
                    IPropertySymbol @prop => new Member(SymbolsMap(prop)),

                    IParameterSymbol @arg => new Argument(SymbolsMap(arg)),

                    _ => throw new UnreachableException()
                };
            }
            
            case ThisExpressionSyntax:
                return new Self();

            case BinaryExpressionSyntax @bin: return ParseExpressin_BinExp(@bin, ref cell, localsMap, expectedType);

            case CastExpressionSyntax @cast:
            {
                var expression =  ParseExpression(cast.Expression, ref cell, localsMap);
                
                var sourceType = expression.Type;
                var targetType = TypeOf(TypeOf(cast));

                switch (targetType)
                {
                    case IntegerTypeReference @i when sourceType is IntegerTypeReference:
                        return new IntTypeCast(i, expression);
                    
                    case IntegerTypeReference @i when sourceType is ReferenceTypeReference:
                        return new IntFromPtr(i, expression);
                    
                    case ReferenceTypeReference @r when sourceType is IntegerTypeReference:
                        return new PtrFromInt(r, expression);
                    
                    case ReferenceTypeReference @r when sourceType is ReferenceTypeReference:
                        return new PtrTypeCast(r, expression);
                    
                    default: throw new InvalidOperationException();
                }
            }

            case ElementAccessExpressionSyntax element:
            {
                var array = ParseExpression(element.Expression, ref cell, localsMap);
                var index = ParseExpression(element.ArgumentList.Arguments[0].Expression, ref cell, localsMap);
                return new Indexer(array, index);
            }

            case PostfixUnaryExpressionSyntax postfixUnary:
            {
                var value = ParseExpression(postfixUnary.Operand, ref cell, localsMap);
                var operation = postfixUnary.OperatorToken.Text switch
                {
                    "++" => new Add(value.Type!, value,
                        new Constant(new IntegerConstantValue((IntegerTypeReference)value.Type!, 1))),
                    _ => throw new UnreachableException()
                };

                cell.Writer.Assignment((IOmegaAssignable)value, operation);

                return value;
            }
                
            default: throw new UnreachableException();
        }
    }

    
    private void ParseExpression_Assingment(
        ExpressionSyntax node,
        ref OmegaCodeCell cell,
        Dictionary<ILocalSymbol, Register> localsMap)
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
                    cell.Writer.Assignment(expl, new Call(new Member(SymbolsMap(r)), expl, expr));
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

    private IOmegaExpression ParseExpressin_BinExp(
        BinaryExpressionSyntax node,
        ref OmegaCodeCell cell,
        Dictionary<ILocalSymbol, Register> localsMap,
        TypeReference? expectedType)
    {
        var stringType = (ITypeSymbol)IntrinsincsMap(IntrinsincElements.Type_String);
        
        var r = (IMethodSymbol)RefOf(node);
        var t = expectedType;
        
        var expl = ParseExpression(node.Left, ref cell, localsMap, expectedType: expectedType);
        var expr = ParseExpression(node.Right, ref cell, localsMap, expectedType: expectedType);

        if (r.MethodKind != MethodKind.BuiltinOperator)
            return new Call(new Member(SymbolsMap(r)), [expl, expr]);
                    
        return node.OperatorToken.Text switch
        {
            "+" => new Add(t, expl, expr),
            "*" => new Mul(t, expl, expr),
                    
            "==" => new Cmp(ComparisonOperation.Equal, expl, expr),
            "!=" => new Cmp(ComparisonOperation.NotEqual, expl, expr),
            "<" => new Cmp(ComparisonOperation.UnsignedLessThan, expl, expr),
            ">" => new Cmp(ComparisonOperation.UnsignedGreaterThan, expl, expr),
            
            _ => throw new UnreachableException()
        };
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
                    sbyte @v => new IntegerConstantValue(i, v),
                    byte @v => new IntegerConstantValue(i, v),
                    short @v => new IntegerConstantValue(i, v),
                    ushort @v => new IntegerConstantValue(i, v),
                    int @v => new IntegerConstantValue(i, v),
                    uint @v => new IntegerConstantValue(i, v),
                    long @v => new IntegerConstantValue(i, v),
                    ulong @v => new IntegerConstantValue(i, v),
                    Int128 @v => new IntegerConstantValue(i, v),
                    UInt128 @v => new IntegerConstantValue(i, v),
                    _ => throw new UnreachableException()
                };
            
        } // fallback when default
        
        return value switch
        {
            null => new NullConstantValue(type ?? throw new ArgumentNullException(nameof(type))),

            bool @v => new IntegerConstantValue(new IntegerTypeReference(false, 1), v ? 1 : 0),
            
            sbyte @v => new IntegerConstantValue(new IntegerTypeReference(true, 8), v),
            byte @v => new IntegerConstantValue(new IntegerTypeReference(false, 8), v),
            short @v => new IntegerConstantValue(new IntegerTypeReference(true, 16), v),
            ushort @v => new IntegerConstantValue(new IntegerTypeReference(false, 16), v),
            int @v => new IntegerConstantValue(new IntegerTypeReference(true, 32), v),
            uint @v => new IntegerConstantValue(new IntegerTypeReference(false, 32), v),
            long @v => new IntegerConstantValue(new IntegerTypeReference(true, 64), v),
            ulong @v => new IntegerConstantValue(new IntegerTypeReference(false, 64), v),
            Int128 @v => new IntegerConstantValue(new IntegerTypeReference(true, 128), v),
            UInt128 @v => new IntegerConstantValue(new IntegerTypeReference(false, 128), v),

            string @s => new SliceConstantValue(
                new IntegerTypeReference(false, 8), Encoding.UTF8.GetBytes(s)
                    .Select(e => new IntegerConstantValue(new IntegerTypeReference(false, 8), e))
                    .ToArray<RealizerConstantValue>()),
            
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
        if (typeSymbol == null!) return null!; // void
        if (typeSymbol is IPointerTypeSymbol @pointerTypeSymbol)
            return new ReferenceTypeReference(TypeOf(pointerTypeSymbol.BaseType!));
        
        IEnumerable<ISymbol> globalParts = [..typeSymbol.ContainingNamespace.ConstituentNamespaces, typeSymbol];
        var global = string.Join('.', globalParts.Select(e => e.Name));

        // Check for the builtin types
        switch (global)
        {
            case "System.Void": return null!;
            case "System.Boolean": return new IntegerTypeReference(false, 1);
            
            case "System.Char":
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
            
            case "System.IntPtr": return new IntegerTypeReference(false, 0);
            case "System.UIntPtr": return new IntegerTypeReference(true, 0);
            
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