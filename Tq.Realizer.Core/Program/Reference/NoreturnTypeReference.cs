using Tq.Realizer.Data;

namespace Tq.Realizer.Core.Builder.References;

public class NoreturnTypeReference : TypeReference
{
    public override Alignment Alignment => Alignment.Zero;
    public override Alignment Length => Alignment.Zero;
    public override string ToString() => "noreturn";
}