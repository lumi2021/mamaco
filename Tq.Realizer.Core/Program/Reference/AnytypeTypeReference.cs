using Tq.Realizer.Data;

namespace Tq.Realizer.Core.Builder.References;

public class AnytypeTypeReference : TypeReference
{
    public override Alignment Alignment => 0;
    public override Alignment Length => 0;
    public override string ToString() => "anytype";
}