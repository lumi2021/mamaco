namespace Tq.Realizer.Core.Builder.References;

public class NoreturnTypeReference : TypeReference
{
    public override uint? Alignment { get => null; init {} }
    public override string ToString() => "noreturn";
}