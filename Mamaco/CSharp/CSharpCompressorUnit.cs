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
