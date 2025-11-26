namespace Tq.Realizer.Core.Configuration.LangOutput;

public interface IOutputConfiguration
{
    public bool BakeGenerics { get; init; }
    public UnnestMembersFlags UnnestMembers { get; init; }
    
    
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
public enum UnnestMembersFlags
{
    None = 0,
    All = Namespaces
        | Structures
        | Typedefs
        | StaticFunctions
        | InstanceFunctions
        | StaticFields
    //  | InstanceFields
        | StaticProperties
        | InstanceProperties,
    
    Namespaces = 1 << 0,
    Structures = 1 << 1,
    Typedefs   = 1 << 2,
    
    StaticFunctions    = 1 << 3,
    InstanceFunctions  = 1 << 4,
    
    StaticFields       = 1 << 5,
    //InstanceFields     = 1 << 6,
    
    StaticProperties   = 1 << 7,
    InstanceProperties = 1 << 8,
}