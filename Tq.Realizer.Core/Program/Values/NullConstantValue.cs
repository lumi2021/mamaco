using Tq.Realizer.Core.Builder.References;

namespace Tq.Realizer.Core.Intermediate.Values;

public class NullConstantValue(TypeReference? type): RealizerConstantValue
{
    public readonly TypeReference? Type = type;
    public override string ToString() => $"({Type?.ToString() ?? "<nil>"} null)";
}
