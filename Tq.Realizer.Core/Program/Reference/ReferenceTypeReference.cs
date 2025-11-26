namespace Tq.Realizer.Core.Builder.References;

public class ReferenceTypeReference(TypeReference? subtype, uint? alignment = null): TypeReference
{
    public readonly TypeReference? Subtype = subtype;
    public override string ToString() => $"*{Subtype?.ToString() ?? "any"}";
    
    public sealed override uint? Alignment { get; init; } = alignment;
}