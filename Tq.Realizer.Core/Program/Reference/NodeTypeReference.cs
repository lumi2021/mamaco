using System.Diagnostics;
using Tq.Realizer.Core.Program.Builder;
using Tq.Realizer.Core.Program.Member;
using Tq.Realizer.Data;

namespace Tq.Realizer.Core.Builder.References;

public class NodeTypeReference : TypeReference
{
    public readonly RealizerMember TypeReference;
    public sealed override Alignment Alignment => TypeReference switch
            {
                RealizerStructure @struct => @struct.Alignment,
                RealizerTypedef @typedef => @typedef.Alignment,
                _ => throw new UnreachableException(),
            };

    public sealed override Alignment Length => TypeReference switch
        {
            RealizerStructure @struct => @struct.Alignment,
            RealizerTypedef @typedef => @typedef.Alignment,
            _ => throw new UnreachableException(),
        };
    
    public NodeTypeReference(RealizerStructure structure) =>TypeReference = structure;
    public NodeTypeReference(RealizerTypedef typedef) => TypeReference = typedef;

   
    public override string ToString() => TypeReference.ToString();
}