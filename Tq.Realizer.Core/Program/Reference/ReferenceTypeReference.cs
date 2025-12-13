using Tq.Realizer.Data;

namespace Tq.Realizer.Core.Builder.References;

public class ReferenceTypeReference(TypeReference? subtype, Alignment? alignment = null): TypeReference
{
    public static ReferenceTypeReference Opaque => new ReferenceTypeReference(null, Data.Alignment.PointerSized);
    
    public sealed override Alignment Alignment => alignment ?? Alignment.PointerSized;
    public override Alignment Length => Alignment.PointerSized;
    
    
    public readonly TypeReference? Subtype = subtype;
    public override string ToString() => $"*{Subtype?.ToString() ?? "opaque"}";
    
}
