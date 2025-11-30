using Tq.Realizer.Data;

namespace Tq.Realizer.Core.Builder.References;

public class NullableTypeReference(TypeReference subtype) : TypeReference
{
    public readonly TypeReference? Subtype = subtype;
    
    public override Alignment Alignment => Subtype?.Alignment ?? Alignment.Zero;
    public override Alignment Length => Subtype?.Length ?? Alignment.Zero;
    
    
    public override string ToString() => $"?{Subtype}";
}
