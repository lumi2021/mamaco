using System.Text;
using Tq.Realizeer.Core.Program.Member;
using Tq.Realizer.Core.Builder.References;
using Tq.Realizer.Core.Intermediate.Values;

namespace Tq.Realizeer.Core.Program.Builder;

public class RealizerTypedef: RealizerContainer
{
    
    public TypeReference? BackingType = null;
    public RealizerTypedefEntry[] Entries;
    public ushort Alignment
    {
        get;
        set
        {
            if (value is < 1 or > 256) throw new ArgumentOutOfRangeException(nameof(Alignment), "Alignment must be between 1 and 256");
            field = value;
        }
    }

    internal RealizerTypedef(string name, RealizerTypedefEntry[] entries) : base(name) => Entries = entries;

    protected override bool GetStatic() => false;

    
    protected override string ToFullDump()
    {
        var sb = new StringBuilder();

        sb.Append($"typedef @{Name} ");
        if (BackingType != null) sb.Append($": {BackingType} ");
        sb.Append('{');
        if (Entries.Length > 0) sb.Append($"{Environment.NewLine}\t");
        sb.AppendLine(string.Join($"{Environment.NewLine}\t", Entries));
        sb.Append('}');
        
        return sb.ToString();
    }
}

public class RealizerTypedefEntry { }

public class RealizerTypedefNamedEntry(string name, RealizerConstantValue? value) : RealizerTypedefEntry
{
    public readonly string Name = name;
    public RealizerConstantValue? Value = value;

    public override string ToString()
    {
        var sb = new StringBuilder();
        
        sb.Append($"{Name}");
        if (Value != null) sb.Append($" => {Value}");
        
        return sb.ToString();
    }
}
