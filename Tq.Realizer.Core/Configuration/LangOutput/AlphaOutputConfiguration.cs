namespace Tq.Realizer.Core.Configuration.LangOutput;

public struct AlphaOutputConfiguration : IOutputConfiguration
{
    public bool BakeGenerics { get; init; }
    public UnnestMembersFlags UnnestMembers { get; init; }
    
    public byte MemoryUnit { get; init; }
    public byte NativeIntegerSize { get; init; }
    
    public GenericAllowedFeatures GenericAllowedFeatures { get; init; }
}
