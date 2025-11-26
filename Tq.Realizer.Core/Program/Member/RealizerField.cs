using System.Text;
using Tq.Realizeer.Core.Program.Member;
using Tq.Realizer.Core.Builder.References;
using Tq.Realizer.Core.Intermediate.Values;

namespace Tq.Realizeer.Core.Program.Builder;

public class RealizerField: RealizerMember
{
    private bool _static = false;
    
    public TypeReference? Type = null;
    public RealizerConstantValue? Initializer = null;
    
    internal RealizerField(string name) : base(name) { }


    protected override bool GetStatic() => _static;
    protected override bool SetStatic(bool value) => _static = value;


    protected override string ToFullDump()
    {
        var sb = new StringBuilder();

        sb.Append($"field {Type?.ToString() ?? "void"} @{Name}");
        if (Initializer != null) sb.Append($" = {Initializer}");
        
        return sb.ToString();
    }
}
