using System.Text;
using Tq.Realizeer.Core.Program.Member;
using Tq.Realizer;
using Tq.Realizer.Data;

namespace Tq.Realizeer.Core.Program.Builder;

public class RealizerStructure: RealizerContainer
{
    public RealizerStructure? Extends { get; set; } = null;
    
    public Alignment Alignment { get; init; }
    public Alignment Length { get; init; }
    
    
    internal RealizerStructure(string name) : base(name) { }


    protected override bool GetStatic() => false;


    protected override string ToFullDump()
    {
        var sb = new StringBuilder();

        sb.Append($"struct @{Name}");
        if (Extends != null) sb.Append($" : {Extends.ToReadableReference()}");
        sb.Append(" {");
        foreach (var i in GetMembers())
            sb.AppendLine($"{Environment.NewLine}{i.ToString("full", null).TabAllLines()}");
        sb.Append('}');
        
        return sb.ToString();
    }
}
