using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tq.Realizer.Builder;
using Tq.Realizer.Builder.Language.Omega;
using Tq.Realizer.Builder.ProgramMembers;
using Tq.Realizer.Builder.References;
using Tq.Realizer.Core.Intermediate.Values;

namespace Mamaco;

public class CSharpCompressor
{

    private Compilation _compilation;
    
    private List<ModuleBuilder> _modules = [];
    private Dictionary<ProgramMemberBuilder, ISymbol> _symbolsMap_1 = [];
    private Dictionary<ISymbol, ProgramMemberBuilder> _symbolsMap_2 = [];
    private Dictionary<ISymbol, FieldBuilder> _backing_field = [];
    
    private enum ParseMode { Load, Store, Call, }
    
    public void CompressModules(ProgramBuilder program, INamespaceSymbol csGlobalNamespace, Compilation compilation)
    {
        _compilation = compilation;
        
        foreach (var csModule in csGlobalNamespace.GetMembers())
        {
            var module = program.AddModule(csModule.Name);
            _modules.Add(module);
            
            var members = csModule.GetMembers();
            var namespaceBuilder = new StringBuilder();
            namespaceBuilder.Append(csModule.Name);

            foreach (var i in members) CompressMember(i, module, namespaceBuilder);
        }
        _modules.TrimExcess();
        _symbolsMap_1.TrimExcess();
        _symbolsMap_2.TrimExcess();
    }
    public void ProcessReferences()
    {
        foreach (var m in _modules)
            ProcessReferencesRecursive(m);
    }
    public void ProccessBodies()
    {
        foreach (var (symbol, builder) in _symbolsMap_2)
        {

            SyntaxNode? body;
            
            switch (symbol)
            {
                case INamespaceOrTypeSymbol: continue;
                
                case IMethodSymbol methodSymbol:
                {
                    var node = symbol.DeclaringSyntaxReferences
                        .ToArray()
                        .FirstOrDefault(e => e.GetSyntax() is MethodDeclarationSyntax);
                    var methodDeclSyntax = node?.GetSyntax() as MethodDeclarationSyntax;
                    body = (SyntaxNode?)methodDeclSyntax?.Body ?? methodDeclSyntax?.ExpressionBody!.Expression;
                    if (body == null) continue;
                    
                    var funcBuilder = (FunctionBuilder)builder;
                    var block = funcBuilder.CreateOmegaBytecodeBlock("entry");
                    Dictionary<ISymbol, int> localsMap = [];

                    for (var i = 0; i < methodSymbol.Parameters.Length; i++)
                        localsMap.Add(methodSymbol.Parameters[i], -i - 1);
                    
                    if (body is BlockSyntax @b) ParseBlock(b, ref block, localsMap);
                    else ParseExpression((ExpressionSyntax)body, ref block, localsMap, ParseMode.Load);

                    if (!block.IsBlockFinished()) block.Writer.Ret(false);

                } break;

                case IFieldSymbol fieldSymbol:
                {
                    var node = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                    body = (node as VariableDeclaratorSyntax)?.Initializer?.Value;
                    if (body == null) continue;
                    
                    ((FieldBuilder)builder).Initializer = ParseConstantValue(body);
                } break;
                
                case IPropertySymbol propertySymbol:
                {
                    if (_backing_field.TryGetValue(propertySymbol, out var field))
                    {
                        var pbuilder = (PropertyBuilder)builder;
                        var getter = pbuilder.Getter!;
                        {
                            var block = getter.CreateOmegaBytecodeBlock("entry");
                            if (pbuilder is InstancePropertyBuilder)
                            {
                                block.Writer
                                    .Ret(true)
                                    .LdSelf()
                                    .LdField((InstanceFieldBuilder)field);
                            }
                            else
                            {
                                block.Writer
                                    .Ret(true)
                                    .LdField((StaticFieldBuilder)field);
                            }
                        }
                        
                        var setter = pbuilder.Setter!;
                        {
                            var block = setter!.CreateOmegaBytecodeBlock("entry");
                            if (pbuilder is InstancePropertyBuilder)
                            {
                                block.Writer
                                    .LdSelf()
                                    .StField((InstanceFieldBuilder)field)
                                    .LdLocal(-1)
                                    .Ret(false);
                            }
                            else
                            {
                                block.Writer
                                    .StField((InstanceFieldBuilder)field)
                                    .LdLocal(-1)
                                    .Ret(false);
                            }
                        }
                    }
                    else
                    {
                        var node = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                        body = (node as PropertyDeclarationSyntax)?.Initializer?.Value;
                        if (body == null) continue;
                    
                        ((PropertyBuilder)builder).Initializer = ParseConstantValue(body);
                    }
                } break;
                
                //AccessorDeclarationSyntax accessorDecl => accessorDecl.Body,
                //LocalFunctionStatementSyntax localFuncDecl => localFuncDecl.Body,
                
                default: throw new UnreachableException();
            };
        }
    }
    
    
    private void CompressMember(ISymbol csMember, INamespaceOrStructureBuilder parentBuilder, StringBuilder namespaceBuilder)
    {
        var globalIdentifier = $"{namespaceBuilder}.{csMember.Name}";
        if (csMember is not INamespaceOrTypeSymbol && !csMember.DeclaringSyntaxReferences.Any()) return;
        
        switch (csMember)
        {
            case INamespaceSymbol @nmsp:
            {
                var ns = ((NamespaceBuilder)parentBuilder).AddNamespace(nmsp.Name);
                AddSymbol(nmsp, ns);

                var stackpoint = namespaceBuilder.Length;
                namespaceBuilder.Append($".{nmsp.Name}");
                foreach (var i in nmsp.GetMembers()) CompressMember(i, ns, namespaceBuilder);
                namespaceBuilder.Length = stackpoint;
            } break;

            case IFieldSymbol @field:
            {
                var fd = parentBuilder.AddField(field.Name, field.IsStatic);
                AddSymbol(field, fd);
            } break;
            
            case IMethodSymbol @method:
            {
                if (!method.DeclaringSyntaxReferences.Any()) return;
                
                var mt = parentBuilder.AddFunction(method.Name, method.IsStatic);
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
                            //case "System.Object":
                            case "System.ValueType":
                            case "System.Enum":
                                
                            case "System.Byte":
                            case "System.SByte":
                            case "System.Int16":
                            case "System.UInt16":
                            case "System.Int32":
                            case "System.UInt32":
                            case "System.Int64":
                            case "System.UInt64":
                            case "System.Int128":
                            case "System.UInt128":
                            case "System.IntPtr":
                            case "System.UIntPtr":
                                
                            case "System.Char":
                            case "System.String":
                            case "System.Boolean":
                                
                            case "System.Single":
                            case "System.Double":
                                
                            case "System.Void":
                                return;
                        }
                        
                        INamespaceOrStructureBuilder ty = !typeClass.IsStatic
                            ? parentBuilder.AddStructure(typeClass.Name)
                            : parentBuilder.AddNamespace(typeClass.Name);
                        
                        AddSymbol(typeClass, (ProgramMemberBuilder)ty);
                        
                        var stackpoint = namespaceBuilder.Length;
                        namespaceBuilder.Append($".{typeClass.Name}");
                        foreach (var i in typeClass.GetMembers()) CompressMember(i, ty, namespaceBuilder);
                        namespaceBuilder.Length = stackpoint;
                    } break;

                    case TypeKind.Enum:
                    {
                        var td = parentBuilder.AddTypedef(typeClass.Name);
                        AddSymbol(typeClass, td);
                        
                        foreach (var i in typeClass.GetMembers())
                        {
                            switch (i)
                            {
                                case IFieldSymbol fs:
                                    td.AddNamedEntry(fs.Name, ParseConstantValue(fs.ConstantValue)); 
                                    continue;

                                case IMethodSymbol mt:
                                    AddSymbol(mt, td.AddFunction(mt.Name, mt.IsStatic));
                                    continue;

                                default: throw new UnreachableException();
                            }
                        }
                    } break;
                    
                    default: throw new UnreachableException();
                }
            } break;

            case IPropertySymbol @property:
            {
                var prop = parentBuilder.AddProperty(property.Name, property.IsStatic);
                AddSymbol(property, prop);
            } break;
            
            default: throw new UnreachableException();
        }
    }
    private void ProcessReferencesRecursive(ProgramMemberBuilder parentBuilder)
    {
        switch (parentBuilder)
        {
            case INamespaceOrStructureOrTypedefBuilder @nst:
                foreach (var i in nst.GetMembers()) ProcessReferencesRecursive(i);
                break;
            
            case FieldBuilder fb:
            {
                fb.Type = GetTypeReference(((IFieldSymbol)SymbolsMap(fb)).Type);
            } break;

            case FunctionBuilder fb:
            {
                var symbol = (IMethodSymbol)SymbolsMap(fb);
                
                fb.ReturnType = GetTypeReference(symbol.ReturnType);
                foreach (var i in symbol.Parameters)
                    fb.AddParameter(i.Name, GetTypeReference(i.Type));
                
            } break;

            case PropertyBuilder pb:
            {
                var symbol = (IPropertySymbol)SymbolsMap(pb);

                var accessorsAreAuto =
                    (symbol.GetMethod == null || symbol.GetMethod.IsImplicitlyDeclared || !HasBody(symbol.GetMethod)) &&
                    (symbol.SetMethod == null || symbol.SetMethod.IsImplicitlyDeclared || !HasBody(symbol.SetMethod));

                if (accessorsAreAuto)
                {
                    var bf = ((INamespaceOrStructureBuilder)pb.Parent!)
                        .AddField($"<{pb.Symbol}>k__BackingField", pb is StaticPropertyBuilder);
                    _backing_field.Add(symbol, bf);
                }
                
                if (symbol.GetMethod != null) pb.Getter = (FunctionBuilder)SymbolsMap(symbol.GetMethod);
                if (symbol.SetMethod != null) pb.Setter = (FunctionBuilder)SymbolsMap(symbol.SetMethod);
                
            } break;
            
            default: throw new UnreachableException();
        }
    }


    private void ParseBlock(BlockSyntax node, ref OmegaBlockBuilder block,  Dictionary<ISymbol, int> localsMap)
    {
        foreach (var s in node.Statements)
            ParseStatement(s, ref block, localsMap);
    }
    private void ParseStatement(StatementSyntax node, ref OmegaBlockBuilder block, Dictionary<ISymbol, int> localsMap)
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
                    block.Writer.MacroDefineLocal(GetTypeReference((ITypeSymbol)csVarType));
                    
                    if (csVarVal == null) continue;
                    block.Writer.StLocal((short)csVarInt);
                    ParseExpression(csVarVal, ref block, localsMap, ParseMode.Load);
                }
            } return;
            
            case ExpressionStatementSyntax exp:
                ParseExpression(exp.Expression, ref block, localsMap, ParseMode.Load);
                return;
            
            default: throw new UnreachableException();
        }
    }

    private void ParseExpression(
        ExpressionSyntax node,
        ref OmegaBlockBuilder block,
        Dictionary<ISymbol, int> localsMap,
        ParseMode parseMode,
        
        bool explicitThisHandled = false)
    {
        switch (node)
        {
            case AssignmentExpressionSyntax: ParseExpression_Assingment(node, ref block, localsMap); break;
            
            case InvocationExpressionSyntax @invocation:
            {
                var exp = invocation.Expression;
                var args = invocation.ArgumentList.Arguments;

                var symbol = (IMethodSymbol)RefOf(exp);
                var func = (FunctionBuilder)SymbolsMap(symbol);

                block.Writer.Call(func);
                foreach (var i in args)
                    ParseExpression(i.Expression, ref block, localsMap, ParseMode.Load);

            } break;

            case LiteralExpressionSyntax @lit:
            {
                var v = ParseConstantValue(lit);
                block.Writer.LdConst(v);
            } break;

            case ImplicitObjectCreationExpressionSyntax @iobj:
            {
                var type = SymbolsMap(((IMethodSymbol)RefOf(iobj)).ContainingSymbol);
                switch (type)
                {
                    case StructureBuilder @stru: block.Writer.LdNewObject(stru); break;
                }
            } break;
            case ObjectCreationExpressionSyntax @obj:
            {
                
            } break;

            case MemberAccessExpressionSyntax memberAccess:
            {
                var left = memberAccess.Expression;
                var right = memberAccess.Name;
                
                ParseExpression(left, ref block, localsMap, ParseMode.Load);
                ParseExpression(right, ref block, localsMap, parseMode,
                    explicitThisHandled: left is ThisExpressionSyntax);
            } break;
            
            case SimpleNameSyntax simpleName:
            {
                var memberSymbol = RefOf(simpleName);
                
                if (!explicitThisHandled && memberSymbol
                         is IMethodSymbol
                         or IPropertySymbol
                         or IFieldSymbol
                         or IEventSymbol
                     && !memberSymbol.IsStatic) block.Writer.LdSelf();
                
                switch (memberSymbol)
                {
                    case IFieldSymbol @field:
                    {
                        switch (parseMode)
                        {
                            case ParseMode.Load: block.Writer.LdField((InstanceFieldBuilder)SymbolsMap(field)); break;
                            case ParseMode.Store: block.Writer.StField((InstanceFieldBuilder)SymbolsMap(field)); break;
                            default: throw new UnreachableException();
                        }
                    } break;

                    case IParameterSymbol @arg:
                        switch (parseMode)
                        {
                            case ParseMode.Load: block.Writer.LdLocal((short)localsMap[arg]); break;
                            case ParseMode.Store: block.Writer.StLocal((short)localsMap[arg]); break;
                            default: throw new UnreachableException();
                        }
                        break;
                    
                    default: throw new UnreachableException();
                }
            } break;
            
            case ThisExpressionSyntax:
                block.Writer.LdSelf();
                break;
                
            
            default: throw new UnreachableException();
        }
    }

    private void ParseExpression_Assingment(ExpressionSyntax node, ref OmegaBlockBuilder block, Dictionary<ISymbol, int> localsMap)
    {
        switch (node)
        {
            case AssignmentExpressionSyntax assignment:
            {
                var target = assignment.Left;
                var val = assignment.Right;
                
                ParseExpression(target, ref block, localsMap, ParseMode.Store);
                ParseExpression(val,  ref block, localsMap, ParseMode.Load);
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

            LiteralExpressionSyntax @lit => ParseConstantValue(lit.Token.Value),
            
            _ => throw new UnreachableException()
        };
    }


    bool HasBody(IMethodSymbol method)
    {
        var syntaxRef = method.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxRef == null) return false;
        var syntax = syntaxRef.GetSyntax();
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
        }

        var langmember = SymbolsMap(typeSymbol);
        switch (langmember)
        {
            case StructureBuilder @struc: return new NodeTypeReference(struc);
            case TypeDefinitionBuilder @typedef: return new NodeTypeReference(typedef);
        }

        throw new UnreachableException();
    }


    private void AddSymbol(ISymbol symbol, ProgramMemberBuilder builder)
    {
        _symbolsMap_1.Add(builder, symbol);
        _symbolsMap_2.Add(symbol, builder);
    }
    private ISymbol SymbolsMap(ProgramMemberBuilder builder) =>  _symbolsMap_1[builder];
    private ProgramMemberBuilder SymbolsMap(ISymbol symbol) =>  _symbolsMap_2[symbol];
}
