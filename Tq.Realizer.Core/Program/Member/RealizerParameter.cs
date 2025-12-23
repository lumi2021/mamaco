using Tq.Realizer.Core.Builder.References;

namespace Tq.Realizer.Core.Program.Member;

public class RealizerParameter
{
    public readonly string Name;
    public readonly TypeReference Type;

    public RealizerParameter(string name, TypeReference type) => (Name, Type) = (name, type);
    
    public override string ToString() => $"{Type} {Name}";
}
