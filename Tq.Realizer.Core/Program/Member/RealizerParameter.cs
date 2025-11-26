using Tq.Realizer.Core.Builder.References;

namespace Tq.Realizeer.Core.Program.Member;

public class RealizerParameter
{
    public readonly string Name;
    public readonly TypeReference Type;

    internal RealizerParameter(string name, TypeReference type) => (Name, Type) = (name, type);
    
    public override string ToString() => $"{Type} {Name}";
}
