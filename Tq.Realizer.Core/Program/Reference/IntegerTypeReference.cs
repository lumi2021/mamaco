using Tq.Realizer.Data;

namespace Tq.Realizer.Core.Builder.References;

public class IntegerTypeReference(bool signed, ushort bits) : TypeReference
{
    public readonly bool Signed = signed;
    public readonly ushort Bits = bits;


    public override Alignment Alignment => Bits == 0 ? Alignment.PointerSized : Bits;
    public override Alignment Length => Bits == 0 ? Alignment.PointerSized : Bits;
    public override string ToString() => (Signed ? "i" : "u") + (Bits != 0 ? $"{Bits}" : "ptr");
}
