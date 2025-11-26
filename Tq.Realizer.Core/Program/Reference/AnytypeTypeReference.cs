namespace Tq.Realizer.Core.Builder.References;

public class AnytypeTypeReference : TypeReference
{
    public override uint? Alignment { get => null; init {} }
    public override string ToString() => "anytype";
}