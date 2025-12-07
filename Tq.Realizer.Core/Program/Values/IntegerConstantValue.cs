using System.Numerics;
using Tq.Realizer.Core.Builder.References;

namespace Tq.Realizer.Core.Intermediate.Values;

public class IntegerConstantValue(IntegerTypeReference typeref, BigInteger value) : RealizerConstantValue
{
    public readonly IntegerTypeReference Type = typeref;
    public readonly BigInteger Value = value;

    public override string ToString() => $"({Type} {Value})";
    public override int GetHashCode() => HashCode.Combine(Type.GetHashCode(), Value.GetHashCode());
}