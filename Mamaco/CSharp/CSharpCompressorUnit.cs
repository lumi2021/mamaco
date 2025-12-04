using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Tq.Realizeer.Core.Program;
using Tq.Realizeer.Core.Program.Builder;
using Tq.Realizeer.Core.Program.Member;


namespace Mamaco;

public partial class CSharpCompressorUnit
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
            ImplementInstrinsics();
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
                switch (globalIdentifier)
                {
                    case "System.Realizer.Intrinsics.RealizerGetStructMetadataPointer": AddIntrinsinc(IntrinsincElements.Function_IntrinsincGetVtable, method); goto ret_lbl;
                    case "System.Realizer.Intrinsics.RealizerGetStructFullName": AddIntrinsinc(IntrinsincElements.Function_IntrinsincGetFullName, method); goto ret_lbl;
                        
                    default:
                        break; 
                        
                    ret_lbl:
                        return;
                }
                
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
                            case "System.ExportAttribute": AddIntrinsinc(IntrinsincElements.Attribute_Export, typeClass); goto ret_lbl;
                            case "System.ImportAttribute": AddIntrinsinc(IntrinsincElements.Attribute_Import, typeClass); goto ret_lbl;
                            
                            case "System.Object": AddIntrinsinc(IntrinsincElements.ClassObject, typeClass); break;
                            case "System.Type": AddIntrinsinc(IntrinsincElements.ClassType, typeClass); break;
                            case "System.ValueType": AddIntrinsinc(IntrinsincElements.ClassValueType, typeClass); break;
                            case "System.Enum": goto ret_lbl;
                            
                            case "System.Byte": AddIntrinsinc(IntrinsincElements.Type_Byte, typeClass); goto ret_lbl;
                            case "System.SByte": AddIntrinsinc(IntrinsincElements.Type_SByte, typeClass); goto ret_lbl;
                            case "System.Int16": AddIntrinsinc(IntrinsincElements.Type_Int16, typeClass); goto ret_lbl;
                            case "System.UInt16": AddIntrinsinc(IntrinsincElements.Type_UInt16, typeClass); goto ret_lbl;
                            case "System.Int32": AddIntrinsinc(IntrinsincElements.Type_Int32, typeClass); goto ret_lbl;
                            case "System.UInt32": AddIntrinsinc(IntrinsincElements.Type_UInt32, typeClass); goto ret_lbl;
                            case "System.Int64": AddIntrinsinc(IntrinsincElements.Type_Int64, typeClass); goto ret_lbl;
                            case "System.UInt64": AddIntrinsinc(IntrinsincElements.Type_UInt64, typeClass); goto ret_lbl;
                            case "System.Int128": AddIntrinsinc(IntrinsincElements.Type_Int128, typeClass); goto ret_lbl;
                            case "System.UInt128": AddIntrinsinc(IntrinsincElements.Type_UInt128, typeClass); goto ret_lbl;
                            case "System.IntPtr": AddIntrinsinc(IntrinsincElements.Type_IntPtr, typeClass); goto ret_lbl;
                            case "System.UIntPtr": AddIntrinsinc(IntrinsincElements.Type_UIntPtr, typeClass); goto ret_lbl;
                                
                            case "System.Char": AddIntrinsinc(IntrinsincElements.Type_Char, typeClass); goto ret_lbl;
                            case "System.String": AddIntrinsinc(IntrinsincElements.Type_String, typeClass); goto ret_lbl;
                            case "System.Boolean": AddIntrinsinc(IntrinsincElements.Type_Boolean, typeClass); goto ret_lbl;
                                
                            case "System.Single": AddIntrinsinc(IntrinsincElements.Type_Float, typeClass); goto ret_lbl;
                            case "System.Double": AddIntrinsinc(IntrinsincElements.Type_Double, typeClass); goto ret_lbl;
                                
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

    private void ImplementInstrinsics()
    {
        // TODO
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
                f.Type = TypeOf(((IFieldSymbol)SymbolsMap(f)).Type);
            } break;
        
            case RealizerFunction f:
            {
                var symbol = (IMethodSymbol)SymbolsMap(f);
                
                f.ReturnType = TypeOf(symbol.ReturnType);
                foreach (var i in symbol.Parameters)
                {
                    var newp = f.AddParameter(i.Name, TypeOf(i.Type));
                    _symbolsMap_3.Add(i, newp);
                }

            } break;
        
            case RealizerProperty p:
            {
                var symbol = (IPropertySymbol)SymbolsMap(p);
        
                p.Type = TypeOf(symbol.Type);
                
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
        ClassObject,
        ClassValueType,
        
        ClassType,
        
        Type_Byte, Type_SByte,
        Type_UInt16, Type_Int16,
        Type_UInt32, Type_Int32,
        Type_UInt64, Type_Int64,
        Type_UInt128, Type_Int128,
        Type_UIntPtr, Type_IntPtr,
        
        Type_Float, Type_Double,
        
        Type_Boolean,
        Type_String,
        Type_Char,
        
        Attribute_Export,
        Attribute_Import,
        
        Function_IntrinsincGetVtable,
        Function_IntrinsincGetFullName,
    }
}
