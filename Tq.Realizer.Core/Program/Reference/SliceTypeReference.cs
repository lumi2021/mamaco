namespace Tq.Realizer.Core.Builder.References;

public class SliceTypeReference(TypeReference subtype) : TypeReference
{
    public readonly TypeReference Subtype = subtype;
    public override string ToString() => $"[]{Subtype}";
    
    public override uint? Alignment { get; init; } = subtype.Alignment;
}
