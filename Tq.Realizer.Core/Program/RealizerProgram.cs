using System.Text;
using Tq.Realizer.Core.Program.Builder;

namespace Tq.Realizer.Core.Program;

public class RealizerProgram(string name)
{
    public readonly string Name = name;
    private List<RealizerModule> _modules = [];
    
    
    public IEnumerable<RealizerModule> Modules => _modules.AsEnumerable();
    
    
    public void AddModule(RealizerModule module) => _modules.Add(module);

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
