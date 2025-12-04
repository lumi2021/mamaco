namespace Tq.Realizer.Core.Configuration.LangOutput;

public struct AlphaOutputConfiguration : IOutputConfiguration
{
    public bool BakeGenerics { get; init; }
    public AbstractingOptions AbstractingOptions { get; init; }
    
    public byte MemoryUnit { get; init; }
    public byte NativeIntegerSize { get; init; }
    
    public GenericAllowedFeatures GenericAllowedFeatures { get; init; }
}
