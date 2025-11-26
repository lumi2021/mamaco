using System.Diagnostics;
using Tq.Realizeer.Core.Program.Builder;
using Tq.Realizeer.Core.Program.Member;

namespace Tq.Realizer.Core.Builder.References;

public class NodeTypeReference : TypeReference
{
    public readonly RealizerMember TypeReference;
    public sealed override uint? Alignment
    {
        get => TypeReference switch
            {
                RealizerStructure @struct => @struct.Alignment,
                RealizerTypedef @typedef => @typedef.Alignment,
                _ => throw new UnreachableException(),
            };
        init { }
    }

    public NodeTypeReference(RealizerStructure structure) =>TypeReference = structure;
    public NodeTypeReference(RealizerTypedef typedef) => TypeReference = typedef;

   
    public override string ToString() => TypeReference.ToString();
}