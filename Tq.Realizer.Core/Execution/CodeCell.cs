using System.Text;
using Tq.Realizer.Core.Program.Builder;

namespace Tq.Realizer.Core.Builder.Execution;

public abstract class CodeCell (RealizerFunction s, string n, uint idx) : IFormattable
{
    public readonly RealizerFunction Source = s;
    public readonly string Name = n;
    public readonly uint Index = idx;

    public abstract bool IsFinished();
    
    public override string ToString() => ToString(null, null);
    public string ToString(string? format, IFormatProvider? provider) => format switch
        {
            "full" => DumpInstructionsToString(),
            _ => $"Code block '{Name}'({Index})"
        };
    public abstract string DumpInstructionsToString();
    
}
