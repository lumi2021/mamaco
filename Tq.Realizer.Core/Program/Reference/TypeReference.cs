using Tq.Realizer.Data;

namespace Tq.Realizer.Core.Builder.References;

public abstract class TypeReference
{
    public abstract Alignment Alignment { get; }
    public abstract Alignment Length { get; }
}