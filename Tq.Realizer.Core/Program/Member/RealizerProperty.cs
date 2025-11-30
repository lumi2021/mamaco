using System.Text;
using Tq.Realizeer.Core.Program.Member;
using Tq.Realizer.Core.Builder.References;
using Tq.Realizer.Core.Intermediate.Values;

namespace Tq.Realizeer.Core.Program.Builder;

public class RealizerProperty: RealizerMember
{
    private bool _static = false;
    
    public TypeReference? Type = null;
    public RealizerFunction? Getter = null;
    public RealizerFunction? Setter = null;
    public RealizerConstantValue? Initializer = null;
    
    internal RealizerProperty(string name) : base(name) { }


    protected override bool GetStatic() => _static;
    protected override void SetStatic(bool value) => _static = value;


    protected override string ToFullDump()
    {
        var sb = new StringBuilder();

        sb.Append($"property {Type?.ToString() ?? "void"} @{Name}");
        sb.Append(" { ");
        if (Getter != null) sb.Append($"get = {Getter} ");
        if (Setter != null) sb.Append($"set = {Setter} ");
        sb.Append("}");
        if (Initializer != null) sb.Append($" = {Initializer}");
        
        return sb.ToString();
    }
}
