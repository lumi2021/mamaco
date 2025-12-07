using Tq.Realizeer.Core.Program.Builder;
using Tq.Realizeer.Core.Program.Member;
using Tq.Realizer.Data;

namespace Tq.Realizer.Core.Builder.References;

public class CallableTypeReference : TypeReference
{
    public sealed override Alignment Alignment => Alignment.Zero;
    public sealed override Alignment Length => Alignment.Zero;


    public readonly TypeReference? Instance;
    public readonly TypeReference? ReturnType;
    public readonly TypeReference[] Arguments;

    public bool IsStatic => Instance == null;
    
    public CallableTypeReference(RealizerFunction from)
    {
        if (!from.Static) Instance = from._parent switch
        {
            RealizerStructure @s => new NodeTypeReference(s),
            RealizerTypedef @t => new NodeTypeReference(t),
            _ => Instance
        };

        ReturnType = from.ReturnType;
        Arguments = from.Parameters.Select(e => e.Type).ToArray();
    }

   
    public override string ToString() => IsStatic
        ? $"func({string.Join(", ", Arguments)}) {ReturnType?.ToString() ?? "void"}"
        : $"{Instance}.func({string.Join(", ", Arguments)}) {ReturnType?.ToString() ?? "void"}";
}
