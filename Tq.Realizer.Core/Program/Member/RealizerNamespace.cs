using System.Text;
using Tq.Realizeer.Core.Program.Member;
using Tq.Realizer;

namespace Tq.Realizeer.Core.Program.Builder;

public class RealizerNamespace: RealizerContainer
{
    internal RealizerNamespace(string name) : base(name) { }


    protected override bool GetStatic() => true;

    
    protected override string ToFullDump()
    {
        var sb = new StringBuilder();

        sb.Append((_parent == null ? "module" : "namespace") + $" @{Name} {{");
        foreach (var i in GetMembers())
            sb.AppendLine($"{Environment.NewLine}{i.ToString("full", null).TabAllLines()}");
        sb.Append('}');
        
        return sb.ToString();
    }
}
