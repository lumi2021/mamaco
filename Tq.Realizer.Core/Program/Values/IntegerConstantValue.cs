using System.Numerics;

namespace Tq.Realizer.Core.Intermediate.Values;

public class IntegerConstantValue(ushort bitSize, BigInteger value) : RealizerConstantValue
{
    public readonly ushort BitSize = bitSize > 256
        ? throw new ArgumentOutOfRangeException()
        : bitSize;
    public readonly BigInteger Value = value;

    public override string ToString() => BitSize == 0 ? $"(iptr {Value})" : $"(i{BitSize} {Value})";
    public override int GetHashCode() => HashCode.Combine(BitSize, Value.GetHashCode());
}