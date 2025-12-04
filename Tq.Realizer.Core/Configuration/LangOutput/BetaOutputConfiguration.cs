namespace Tq.Realizer.Core.Configuration.LangOutput;

public struct BetaOutputConfiguration : IOutputConfiguration
{
    public bool BakeGenerics { get; init; }
    public AbstractingOptions AbstractingOptions { get; init; }
    
    public byte MemoryUnit { get; init; }
    public byte NativeIntegerSize { get; init; }
    public GenericAllowedFeatures GenericAllowedFeatures { get; init; }

    public BetaExtendableInstructionSet EnabledOpcodes { get; init; }
    public BetaExtendableScopes EnabledScopes { get; init; }
    public BetaSizedOperationsOptions SizedOperations { get; init; }
    public BetaDataKinds LocalStores { get; init; }
    public bool UseMemoryStack { get; init; }
}

[Flags]
public enum BetaExtendableInstructionSet
{
        None = 0,
        All = None
            | Dup | Swap | NewObj,
            
        Dup = (1 << 0),
        Swap = (1 << 1),
        NewObj = (1 << 2),
}

[Flags]
public enum BetaExtendableScopes
{
        None = 0,
        All = int.MaxValue,
            
        Block = (1 << 0),
        Loop = (1 << 1),
        IfElse = (1 << 2),
        Switch = (1 << 3),
}

[Flags]
public enum BetaSizedOperationsOptions
{
    None = 0,
    All = None
            | IntegerSigness
            | IntegerSize
            | FloatingSize
            | Object,
    
    IntegerSigness = (1 << 0),
    IntegerSize = (1 << 1),
    
    FloatingSize = (1 << 2),
    
    Object = (1 << 3),
}

[Flags]
public enum BetaDataKinds: byte
{
    None = 0,
    All = None
          | Primitives
          | Objects,
    
    Primitives = (1 << 0),
    Objects = (1 << 1),
}
