namespace Tq.Realizer.Core.Configuration.LangOutput;

public class OmegaOutputConfiguration : IOutputConfiguration
{
    public bool BakeGenerics { get; init; }
    public UnnestMembersFlags UnnestMembers { get; init; }
    
    public byte MemoryUnit { get; init; }
    public byte NativeIntegerSize { get; init; }
    
    public GenericAllowedFeatures GenericAllowedFeatures { get; init; }
    public OmegaAllowedFeatures OmegaAllowedFeatures { get; init; } 
}


[Flags]
public enum OmegaAllowedFeatures
{
    None = 0,
    All = 0,
}
