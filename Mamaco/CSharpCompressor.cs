using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Tq.Realizer.Builder;
using Tq.Realizer.Builder.ProgramMembers;
using Tq.Realizer.Builder.References;
using Tq.Realizer.Core.Intermediate.Values;

namespace Mamaco;

public class CSharpCompressor
{

    private List<ModuleBuilder> _modules = [];
    private Dictionary<ProgramMemberBuilder, ISymbol> _symbolsMap_1 = [];
    private Dictionary<ISymbol, ProgramMemberBuilder> _symbolsMap_2 = [];

    public void CompressModules(ProgramBuilder program, INamespaceSymbol csGlobalNamespace)
    {
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
        foreach (var m in _modules) ProcessReferencesRecursive(m);
    }

    public void ProcessFunctionBodies()
    {
        // TODO
    }
    
    
    private void CompressMember(ISymbol csMember, INamespaceOrStructureBuilder parentBuilder, StringBuilder namespaceBuilder)
    {
        var globalIdentifier = $"{namespaceBuilder}.{csMember.Name}";
        
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
                
            } break;
            
            default: throw new UnreachableException();
        }
    }

    private void ProcessReferencesRecursive(ProgramMemberBuilder parentBuilder)
    {
        switch (parentBuilder)
        {
            case NamespaceBuilder nmsp:
            {
                foreach (var i in nmsp.Namespaces) ProcessReferencesRecursive(i);
                foreach (var i in nmsp.Fields) ProcessReferencesRecursive(i);
                foreach (var i in nmsp.Functions) ProcessReferencesRecursive(i);
                foreach (var i in nmsp.Structures) ProcessReferencesRecursive(i);
                foreach (var i in nmsp.TypeDefinitions) ProcessReferencesRecursive(i);
            } break;

            case FieldBuilder fb:
            {
                fb.Type = GetTypeReference(((IFieldSymbol)SymbolsMap(fb)).Type);
            } break;

            case StructureBuilder sb:
            {
                foreach (var i in sb.InnerNamespaces) ProcessReferencesRecursive(i);
                foreach (var i in sb.StaticFields) ProcessReferencesRecursive(i);
                foreach (var i in sb.Fields) ProcessReferencesRecursive(i);
                foreach (var i in sb.Functions) ProcessReferencesRecursive(i);
                foreach (var i in sb.InnerStructures) ProcessReferencesRecursive(i);
                foreach (var i in sb.InnerTypedefs) ProcessReferencesRecursive(i);
            } break;

            case FunctionBuilder fb:
            {
                var symbol = (IMethodSymbol)SymbolsMap(fb);
                
                fb.ReturnType = GetTypeReference(symbol.ReturnType);
                foreach (var i in symbol.Parameters)
                    fb.AddParameter(i.Name, GetTypeReference(i.Type));
                
            } break;

            case TypeDefinitionBuilder td:
            {
                foreach (var i in td.Functions) ProcessReferencesRecursive(i);
            } break;
            
            default: throw new UnreachableException();
        }
    }
    

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
    private RealizerConstantValue ParseConstantValue(object? value)
    {
        return value switch
        {
            null => new NullConstantValue(),

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

            _ => throw new UnreachableException()
        };
    }

    private void AddSymbol(ISymbol symbol, ProgramMemberBuilder builder)
    {
        _symbolsMap_1.Add(builder, symbol);
        _symbolsMap_2.Add(symbol, builder);
    }
    private ISymbol SymbolsMap(ProgramMemberBuilder builder) =>  _symbolsMap_1[builder];
    private ProgramMemberBuilder SymbolsMap(ISymbol symbol) =>  _symbolsMap_2[symbol];
}
