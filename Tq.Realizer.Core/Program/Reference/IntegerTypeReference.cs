namespace Tq.Realizer.Core.Builder.References;

public class IntegerTypeReference(bool signed, byte? bits) : TypeReference
{
    public readonly bool Signed = signed;
    public readonly byte? Bits = bits;


    public override uint? Alignment { get; init; } = bits ?? null;
    public override string ToString() => (Signed ? "i" : "u") + (Bits.HasValue ? $"{Bits.Value}" : "ptr");
}