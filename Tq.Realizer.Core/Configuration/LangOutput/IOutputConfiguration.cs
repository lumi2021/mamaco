namespace Tq.Realizer.Core.Configuration.LangOutput;

public interface IOutputConfiguration
{
    public bool BakeGenerics { get; init; }
    public AbstractingOptions AbstractingOptions { get; init; }
    
    
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
    
    InitializeFieldsOnCall = 1 << 3,
}

[Flags]
public enum AbstractingOptions
{
    None = 1 << 0,
    
    NoNamespaces = 1 << 1,
    
    NoInstanceMethod = 1 << 2,
    NoInheritance = 1 << 3,
    
    NoObjectOrientation = NoInstanceMethod | NoInheritance,
}
