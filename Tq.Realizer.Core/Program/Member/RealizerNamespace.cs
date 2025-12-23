using System.Text;
using Tq.Realizer;
using Tq.Realizer.Core.Program.Member;

namespace Tq.Realizer.Core.Program.Builder;

public class RealizerNamespace: RealizerContainer
{
    internal RealizerNamespace(string name) : base(name) { }
    
    protected override bool GetStatic() => true;

    
    protected override string ToFullDump()
    {
        var sb = new StringBuilder();

        sb.Append($"namespace @{Name} {{");
        foreach (var i in GetMembers())
            sb.AppendLine($"{Environment.NewLine}{i.ToString("full", null).TabAllLines()}");
        sb.Append('}');
        
        return sb.ToString();
    }
}
