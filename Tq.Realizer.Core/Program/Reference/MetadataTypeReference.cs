using System.Diagnostics;
using Tq.Realizeer.Core.Program.Builder;
using Tq.Realizeer.Core.Program.Member;
using Tq.Realizer.Data;

namespace Tq.Realizer.Core.Builder.References;

public class MetadataTypeReference(TypeReference of) : TypeReference
{
    public readonly TypeReference Typeof = of;
    
    public sealed override Alignment Alignment => Alignment.Zero;
    public sealed override Alignment Length => Alignment.Zero;


    public override string ToString() => $"Type({Typeof})";
}