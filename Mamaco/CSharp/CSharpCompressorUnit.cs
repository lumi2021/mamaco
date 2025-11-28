using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tq.Realizeer.Core.Program;
using Tq.Realizeer.Core.Program.Builder;
using Tq.Realizeer.Core.Program.Member;
using Tq.Realizer.Core.Builder.Execution.Omega;
using Tq.Realizer.Core.Builder.Language.Omega;
using Tq.Realizer.Core.Builder.References;
using Tq.Realizer.Core.Intermediate.Values;

using static Tq.Realizer.Core.Builder.Language.Omega.OmegaInstructions;

namespace Mamaco;

public class CSharpCompressorUnit
{

    private Compilation _compilation;
    
    private RealizerProgram _program = null!;
    private Dictionary<RealizerMember, ISymbol> _symbolsMap_1 = [];
    private Dictionary<ISymbol, RealizerMember> _symbolsMap_2 = [];
    private Dictionary<IParameterSymbol, RealizerParameter> _symbolsMap_3 = [];
    
    private Dictionary<IntrinsincElements, ISymbol> _intrinsincsMap_1 = [];
    private Dictionary<ISymbol, IntrinsincElements> _intrinsincsMap_2 = [];
    
    private Dictionary<ISymbol, RealizerField> _backingField = [];

    
    
    public void CompressModules(RealizerProgram program, INamespaceSymbol csGlobalNamespace, Compilation compilation)
    {
        _compilation = compilation;
        _program = program;
        
        foreach (var csModule in csGlobalNamespace.GetMembers())
        {
            var module = RealizerModuleBuilder.Create(csModule.Name).Build();
            program.AddModule(module);
            
            var members = csModule.GetMembers();
            var namespaceBuilder = new StringBuilder();
            namespaceBuilder.Append(csModule.Name);

            foreach (var i in members) CompressMember(i, module, namespaceBuilder);
        }
        
        _symbolsMap_1.TrimExcess();
        _symbolsMap_2.TrimExcess();
    }
    public void ProcessReferences()
    {
        foreach (var m in _program.Modules)
        {
            ProcessReferencesRecursive(m);
            
            var cancelToken = new CancellationTokenSource().Token;
            var entryPoint = _compilation.GetEntryPoint(cancelToken);
            if (entryPoint == null) continue;
            var func = (RealizerFunction)SymbolsMap(entryPoint);
            func.Export("_start");
        }
    }
    public void ProccessBodies()
    {
        foreach (var (symbol, builder) in _symbolsMap_2)
        {
        
            SyntaxNode? body;
            
            switch (symbol)
            {
                case INamespaceOrTypeSymbol: continue;
                
                case IMethodSymbol methodSymbol when methodSymbol.MethodKind == MethodKind.Constructor:
                {
                    var node = symbol.DeclaringSyntaxReferences.ToArray().FirstOrDefault();
                    var methodDeclSyntax = node?.GetSyntax() as MethodDeclarationSyntax;
                    body = (SyntaxNode?)methodDeclSyntax?.Body ?? methodDeclSyntax?.ExpressionBody!.Expression;
                    if (node == null) continue;
                    
                    var func = (RealizerFunction)builder;
                    var cell = func.AddOmegaCodeCell("entry");
                    Dictionary<ISymbol, int> localsMap = [];
        
                    for (var i = 0; i < methodSymbol.Parameters.Length; i++)
                        localsMap.Add(methodSymbol.Parameters[i], -i - 1);

                    var instancet = GetTypeReference(methodSymbol.ReceiverType!);
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
                                var baseRef = SymbolsMap(RefOf(constructorDec.Initializer));
                                cell.Writer.Call(null, new Member(baseRef),
                                    [new Argument(instancep), ..constructorDec.Initializer.ArgumentList.Arguments
                                    .Select(e => ParseExpression(e.Expression, ref cell, []))]
                                );
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
                            if (x != null) cell.Writer.Ret(x);
                        }
                    
                        if (!cell.IsFinished()) cell.Writer.Ret();
                    }
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
                    
                    ((RealizerField)builder).Initializer = ParseConstantValue(body);
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
    
    private void CompressMember(ISymbol csMember, RealizerContainer parent, StringBuilder namespaceBuilder)
    {
        var globalIdentifier = $"{namespaceBuilder}.{csMember.Name}";
        if (csMember is not INamespaceOrTypeSymbol && !csMember.DeclaringSyntaxReferences.Any()) return;
        
        switch (csMember)
        {
            case INamespaceSymbol @nmsp:
            {
                var newNmsp = RealizerNamespaceBuilder
                    .Create(@nmsp.Name)
                    .Build();
                
                parent.AddMember(newNmsp);
                AddSymbol(nmsp, newNmsp);
        
                var stackpoint = namespaceBuilder.Length;
                namespaceBuilder.Append($".{nmsp.Name}");
                foreach (var i in nmsp.GetMembers()) CompressMember(i, newNmsp, namespaceBuilder);
                namespaceBuilder.Length = stackpoint;
            } break;
        
            case IFieldSymbol @field:
            {
                var fd = RealizerFieldBuilder
                    .Create(field.Name)
                    .SetStatic(field.IsStatic)
                    .Build();
                
                parent.AddMember(fd);
                AddSymbol(field, fd);
            } break;
            
            case IMethodSymbol @method:
            {
                if (!method.DeclaringSyntaxReferences.Any()) return;
        
                var mt = RealizerFunctionBuilder.Create(method.Name)
                    .SetStatic(method.IsStatic || method.MethodKind == MethodKind.Constructor)
                    .Build();
                
                parent.AddMember(mt);
                AddSymbol(method, mt);
            } break;
            
            case ITypeSymbol @typeClass:
            {
                switch (typeClass.TypeKind)
                {
                    case TypeKind.Class:
                    case TypeKind.Struct:
                    {
                        switch (globalIdentifier)
                        {
                            case "System.ExportAttribute": AddIntrinsinc(IntrinsincElements.AttributeExport, typeClass); goto ret_lbl;
                            case "System.ImportAttribute": AddIntrinsinc(IntrinsincElements.AttributeImport, typeClass); goto ret_lbl;
                            
                            case "System.Object": AddIntrinsinc(IntrinsincElements.TypeObject, typeClass); break;
                            case "System.ValueType": AddIntrinsinc(IntrinsincElements.TypeValueType, typeClass); break;
                            case "System.Enum": goto ret_lbl;
                            
                            case "System.Byte": AddIntrinsinc(IntrinsincElements.TypeByte, typeClass); goto ret_lbl;
                            case "System.SByte": AddIntrinsinc(IntrinsincElements.TypeSByte, typeClass); goto ret_lbl;
                            case "System.Int16": AddIntrinsinc(IntrinsincElements.TypeInt16, typeClass); goto ret_lbl;
                            case "System.UInt16": AddIntrinsinc(IntrinsincElements.TypeUInt16, typeClass); goto ret_lbl;
                            case "System.Int32": AddIntrinsinc(IntrinsincElements.TypeInt32, typeClass); goto ret_lbl;
                            case "System.UInt32": AddIntrinsinc(IntrinsincElements.TypeUInt32, typeClass); goto ret_lbl;
                            case "System.Int64": AddIntrinsinc(IntrinsincElements.TypeInt64, typeClass); goto ret_lbl;
                            case "System.UInt64": AddIntrinsinc(IntrinsincElements.TypeUInt64, typeClass); goto ret_lbl;
                            case "System.Int128": AddIntrinsinc(IntrinsincElements.TypeInt128, typeClass); goto ret_lbl;
                            case "System.UInt128": AddIntrinsinc(IntrinsincElements.TypeUInt128, typeClass); goto ret_lbl;
                            case "System.IntPtr": AddIntrinsinc(IntrinsincElements.TypeIntPtr, typeClass); goto ret_lbl;
                            case "System.UIntPtr": AddIntrinsinc(IntrinsincElements.TypeUIntPtr, typeClass); goto ret_lbl;
                                
                            case "System.Char": AddIntrinsinc(IntrinsincElements.TypeChar, typeClass); goto ret_lbl;
                            case "System.String": AddIntrinsinc(IntrinsincElements.TypeString, typeClass); goto ret_lbl;
                            case "System.Boolean": AddIntrinsinc(IntrinsincElements.TypeBoolean, typeClass); goto ret_lbl;
                                
                            case "System.Single": AddIntrinsinc(IntrinsincElements.TypeFloat, typeClass); goto ret_lbl;
                            case "System.Double": AddIntrinsinc(IntrinsincElements.TypeDouble, typeClass); goto ret_lbl;
                                
                            case "System.Void":
                                
                            ret_lbl: return;
                        }
                        
                        RealizerContainer ty = !typeClass.IsStatic
                            ? RealizerStructureBuilder.Create(typeClass.Name).Build()
                            : RealizerNamespaceBuilder.Create(typeClass.Name).Build();
                        
                        parent.AddMember(ty);
                        AddSymbol(typeClass, ty);
                        
                        var stackpoint = namespaceBuilder.Length;
                        namespaceBuilder.Append($".{typeClass.Name}");
                        foreach (var i in typeClass.GetMembers()) CompressMember(i, ty, namespaceBuilder);
                        namespaceBuilder.Length = stackpoint;
                    } break;
        
                    case TypeKind.Enum:
                    {
                        var tdb = RealizerTypedefBuilder.Create(typeClass.Name);
                        List<RealizerFunction> functions = [];
                        
                        foreach (var i in typeClass.GetMembers())
                        {
                            switch (i)
                            {
                                case IFieldSymbol fs:
                                    tdb.WithNamedEntry(fs.Name, ParseConstantValue(fs.ConstantValue)); 
                                    continue;
        
                                case IMethodSymbol mt:
                                    var f = RealizerFunctionBuilder
                                        .Create(mt.Name)
                                        .SetStatic(mt.IsStatic)
                                        .Build();
                                    
                                    AddSymbol(mt, f);
                                    functions.Add(f);
                                    continue;
        
                                default: throw new UnreachableException();
                            }
                        }

                        var td = tdb.Build();
                        td.AddMembers(functions);
                        
                        parent.AddMember(td);
                        AddSymbol(typeClass, td);
                        
                    } break;
                    
                    default: throw new UnreachableException();
                }
            } break;
        
            case IPropertySymbol @property:
            {
                var prop = RealizerPropertyBuilder.Create(property.Name)
                    .SetStatic(property.IsStatic)
                    .Build();
                
                parent.AddMember(prop);
                AddSymbol(property, prop);
            } break;
            
            default: throw new UnreachableException();
        }
    }
    private void ProcessReferencesRecursive(RealizerMember parentBuilder)
    {
        switch (parentBuilder)
        {
            
            case RealizerModule:
            case RealizerNamespace: goto skipall;

            case RealizerStructure s:
            {
                var symbol = (ITypeSymbol)SymbolsMap(s);
                if (symbol.BaseType != null) s.Extends = (RealizerStructure)SymbolsMap(symbol.BaseType);
            } break;

            case RealizerTypedef t:
            {
                var symbol = (ITypeSymbol)SymbolsMap(t);
                // TODO
            } break;
            
            case RealizerField f:
            {
                f.Type = GetTypeReference(((IFieldSymbol)SymbolsMap(f)).Type);
            } break;
        
            case RealizerFunction f:
            {
                var symbol = (IMethodSymbol)SymbolsMap(f);
                
                f.ReturnType = GetTypeReference(symbol.ReturnType);
                foreach (var i in symbol.Parameters)
                {
                    var newp = f.AddParameter(i.Name, GetTypeReference(i.Type));
                    _symbolsMap_3.Add(i, newp);
                }

            } break;
        
            case RealizerProperty p:
            {
                var symbol = (IPropertySymbol)SymbolsMap(p);
        
                p.Type = GetTypeReference(symbol.Type);
                
                var accessorsAreAuto =
                    (symbol.GetMethod == null || symbol.GetMethod.IsImplicitlyDeclared || !HasBody(symbol.GetMethod)) &&
                    (symbol.SetMethod == null || symbol.SetMethod.IsImplicitlyDeclared || !HasBody(symbol.SetMethod));
        
                if (accessorsAreAuto)
                {
                    var backingField = RealizerFieldBuilder
                        .Create($"<{p.Name}>k__BackingField")
                        .SetStatic(p.Static)
                        .Build();
                    
                    p.Parent!.AddMember(backingField);
                    backingField.Type = p.Type;
                    _backingField.Add(symbol, backingField);
                }
                
                if (symbol.GetMethod != null) p.Getter = (RealizerFunction)SymbolsMap(symbol.GetMethod);
                if (symbol.SetMethod != null) p.Setter = (RealizerFunction)SymbolsMap(symbol.SetMethod);
                
            } break;
            
            default: throw new UnreachableException();
        }
        
        skipall:
        if (parentBuilder is not RealizerContainer @c) return;
        foreach (var i in c.GetMembers().ToArray()) ProcessReferencesRecursive(i);
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
                        new Register((ushort)csVarInt),
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

    private IOmegaValue ParseExpression(
        ExpressionSyntax node,
        ref OmegaCodeCell cell,
        Dictionary<ISymbol, int> localsMap,
        
        bool explicitThisHandled = false)
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
        
                var symbol = (IMethodSymbol)RefOf(exp);
                var func = (RealizerFunction)SymbolsMap(symbol);

                List<IOmegaValue> argsList = [];
                foreach (var i in args)
                    argsList.Add(ParseExpression(i.Expression, ref cell, localsMap));
                
                return new Call(func.ReturnType, new Member(func), [.. argsList]);
            }
        
            case LiteralExpressionSyntax @lit:
            {
                var v = ParseConstantValue(lit);
                return new Constant(v);
            }
        
            case ImplicitObjectCreationExpressionSyntax @iobj:
            case ObjectCreationExpressionSyntax @obj:
                throw new NotImplementedException();
        
            case MemberAccessExpressionSyntax memberAccess:
            {
                var left = memberAccess.Expression;
                var right = memberAccess.Name;
                
                return new Access(
                    ParseExpression(left, ref cell, localsMap),
                    ParseExpression(right, ref cell, localsMap, explicitThisHandled: left is ThisExpressionSyntax)
                );
            }
            
            case SimpleNameSyntax simpleName:
            {
                var memberSymbol = RefOf(simpleName);

                IOmegaValue? accessLeft = null;
                IOmegaValue? accessRight = null;
                
                if (!explicitThisHandled && memberSymbol
                         is IMethodSymbol
                         or IPropertySymbol
                         or IFieldSymbol
                         or IEventSymbol
                     && !memberSymbol.IsStatic) accessLeft = new Self();

                accessRight = memberSymbol switch
                {
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
                var expl = ParseExpression(bin.Left, ref cell, localsMap);
                var expr = ParseExpression(bin.Right, ref cell, localsMap);
                
                var r = (IMethodSymbol)RefOf(bin);
                var t = GetTypeReference(r.ReturnType);
                
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

                var expl = (IOmegaAssignable)ParseExpression(target, ref cell, localsMap);
                var expr = ParseExpression(val, ref cell, localsMap);

                if (assig.Kind() == SyntaxKind.SimpleAssignmentExpression)
                {
                    cell.Writer.Assignment(expl, expr);
                    return;
                }

                var r = (IMethodSymbol)RefOf(assig);
                var t = GetTypeReference(r.ReturnType);
                
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

    private RealizerConstantValue ParseConstantValue(object? value)
    {
        return value switch
        {
            null => new NullConstantValue(),

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
            
            LiteralExpressionSyntax @lit => ParseConstantValue(lit.Token.Value),
            
            _ => throw new UnreachableException()
        };
    }


    bool HasBody(IMethodSymbol method)
    {
        var syntaxRef = method.DeclaringSyntaxReferences.FirstOrDefault();
        var syntax = syntaxRef?.GetSyntax();
        return syntax is AccessorDeclarationSyntax { Body: not null };
    }
    
    private ISymbol RefOf(SyntaxNode node) => _compilation.GetSemanticModel(node.SyntaxTree).GetSymbolInfo(node).Symbol!;
    private ISymbol RefOf(VariableDeclaratorSyntax var) => _compilation.GetSemanticModel(var.SyntaxTree!).GetDeclaredSymbol(var)!;
    
    private TypeReference GetTypeReference(ITypeSymbol typeSymbol)
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


    private void AddSymbol(ISymbol symbol, RealizerMember builder)
    {
        _symbolsMap_1.Add(builder, symbol);
        _symbolsMap_2.Add(symbol, builder);
    }
    private ISymbol SymbolsMap(RealizerMember builder) =>  _symbolsMap_1[builder];
    private RealizerMember SymbolsMap(ISymbol symbol) =>  _symbolsMap_2[symbol];
    private RealizerParameter SymbolsMap(IParameterSymbol symbol) =>  _symbolsMap_3[symbol];


    private void AddIntrinsinc(IntrinsincElements kind, ISymbol symbol)
    {
        _intrinsincsMap_1.Add(kind, symbol);
        _intrinsincsMap_2.Add(symbol, kind);
    }
    private ISymbol IntrinsincsMap(IntrinsincElements kind) => _intrinsincsMap_1[kind];
    private IntrinsincElements IntrinsincsMap(ISymbol symbol) => _intrinsincsMap_2[symbol];
    
    private enum IntrinsincElements
    {
        TypeObject,
        TypeValueType,
        
        TypeByte, TypeSByte,
        TypeUInt16, TypeInt16,
        TypeUInt32, TypeInt32,
        TypeUInt64, TypeInt64,
        TypeUInt128, TypeInt128,
        TypeUIntPtr, TypeIntPtr,
        
        TypeFloat, TypeDouble,
        
        TypeBoolean,
        TypeString,
        TypeChar,
        
        AttributeExport,
        AttributeImport,
    }
}
