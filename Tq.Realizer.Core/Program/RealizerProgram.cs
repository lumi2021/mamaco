using System.Text;
using Tq.Realizeer.Core.Program.Builder;

namespace Tq.Realizeer.Core.Program;

public class RealizerProgram(string name)
{
    public readonly string Name = name;
    private List<RealizerNamespace> _modules = [];
    
    
    public IEnumerable<RealizerNamespace> Modules => _modules.AsEnumerable();
    
    
    public void AddModule(RealizerNamespace module) => _modules.Add(module);

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"; Program: {Name}");
        sb.AppendLine($"; {_modules.Count} modules");
        sb.AppendLine();
        
        sb.AppendLine(string.Join("\n", _modules.Select(e => e.ToString("full", null))));

        return sb.ToString();
    }
}
