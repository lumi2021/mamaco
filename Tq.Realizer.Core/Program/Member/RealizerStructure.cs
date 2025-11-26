using System.Text;
using Tq.Realizeer.Core.Program.Member;
using Tq.Realizer;

namespace Tq.Realizeer.Core.Program.Builder;

public class RealizerStructure: RealizerContainer
{
    public RealizerStructure? Extends { get; set; } = null;
    
    public ushort Alignment
    {
        get;
        init
        {
            if (value is < 1 or > 256)
                throw new ArgumentOutOfRangeException(nameof(Alignment), "Alignment must be between 1 and 256");
            field = value;
        }
    }
    public uint Length { get; init; }
    
    
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
