namespace Tq.Realizer.Core.Configuration.LangOutput;

public interface IOutputConfiguration
{
    public bool BakeGenerics { get; init; }
    public UnnestMembersOptions UnnestMembersOption { get; init; }
    
    
    public byte MemoryUnit { get; init; }
    public byte NativeIntegerSize { get; init; }
    
    public GenericAllowedFeatures GenericAllowedFeatures { get; init; }
}

[Flags]
public enum GenericAllowedFeatures
{
    None = 0,
    All = None
          | LdSelf
          | PrimitivesOnStack
          | StructuresOnStack,
    
    LdSelf = 1 << 0,
    PrimitivesOnStack = 1 << 1,
    StructuresOnStack = 1 << 2,
    
}

[Flags]
public enum UnnestMembersOptions
{
    None = 1 << 0,
    NoNamespaces = 1 << 1,
    ForceStaticFunctions = 1 << 2,
}
