using Tq.Realizer.Data;

namespace Tq.Realizer.Core.Builder.References;

public class SliceTypeReference(TypeReference subtype) : TypeReference
{
    public static readonly SliceTypeReference Utf8String = new SliceTypeReference(new IntegerTypeReference(false, 8));
    
    public override Alignment Alignment => Alignment.PointerSized;
    public override Alignment Length => Alignment.WithValueNativeSized(2);


    public readonly TypeReference Subtype = subtype;
    public override string ToString() => $"[]{Subtype}";
    
}
