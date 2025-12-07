using Tq.Realizer.Data;

namespace Tq.Realizer.Core.Builder.References;

public class IntegerTypeReference : TypeReference
{
    public readonly bool Signed;
    public readonly ushort Bits;


    public override Alignment Alignment => Bits == 0 ? Alignment.PointerSized : Bits;
    public override Alignment Length => Bits == 0 ? Alignment.PointerSized : Bits;


    public IntegerTypeReference(bool signed, ushort bits)
    {
        if (bits > 256) throw new ArgumentOutOfRangeException(nameof(bits));
        
        Signed = signed;
        Bits = bits;
    }
    
    public override string ToString() => (Signed ? "i" : "u") + (Bits != 0 ? $"{Bits}" : "ptr");
    public override int GetHashCode() => HashCode.Combine(Signed, Bits);
}
