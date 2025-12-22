using Tq.Realizer.Data;

namespace Tq.Realizer.Core.Builder.References;

public class IntegerTypeReference : TypeReference
{
    public static readonly IntegerTypeReference Bool = new IntegerTypeReference(false, 1);
    public static readonly IntegerTypeReference Byte = new IntegerTypeReference(false, 8);
    public static readonly IntegerTypeReference SByte = new IntegerTypeReference(true, 8);
    public static readonly IntegerTypeReference Short = new IntegerTypeReference(true, 16);
    public static readonly IntegerTypeReference UShort = new IntegerTypeReference(false, 16);
    public static readonly IntegerTypeReference Int = new IntegerTypeReference(true, 32);
    public static readonly IntegerTypeReference UInt = new IntegerTypeReference(false, 32);
    public static readonly IntegerTypeReference Long = new IntegerTypeReference(true, 64);
    public static readonly IntegerTypeReference ULong = new IntegerTypeReference(false, 64);
    public static readonly IntegerTypeReference NInt = new IntegerTypeReference(true, 0);
    public static readonly IntegerTypeReference NUInt = new IntegerTypeReference(false, 0);
    
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
