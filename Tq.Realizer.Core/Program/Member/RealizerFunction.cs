using System.Text;
using Tq.Realizeer.Core.Program.Member;
using Tq.Realizer;
using Tq.Realizer.Core.Builder.Execution;
using Tq.Realizer.Core.Builder.Execution.Omega;
using Tq.Realizer.Core.Builder.References;

namespace Tq.Realizeer.Core.Program.Builder;

public class RealizerFunction: RealizerMember
{
    private bool _static;
    
    public RealizerParameter[] Parameters { get; private set; }
    public TypeReference? ReturnType = null;

    private List<CodeCell> _executionBlocks = [];
    public IEnumerable<CodeCell> ExecutionBlocks => _executionBlocks.AsEnumerable();
    public int ExecutionBlocksCount => _executionBlocks.Count;

    public string? ExportSymbol { get; private set; } = null;
    
    
    internal RealizerFunction(string name, RealizerParameter[]  parameters) : base(name) => Parameters = parameters;


    protected override bool GetStatic() => _static;
    protected override void SetStatic(bool value) => _static = value;

    public RealizerParameter AddParameter(string name, TypeReference type, int index = -1)
    {
        var p = new RealizerParameter(name, type);
        AddParameter(p, index);
        return p;
    }
    public void AddParameter(RealizerParameter parameter, int index = -1)
    {
        var l= Parameters.ToList();
        l.Insert(index == -1 ? l.Count : index, parameter);
        Parameters = l.ToArray();
    }

    public void Export(string symbol) => ExportSymbol = symbol;
    
    
    public OmegaCodeCell AddOmegaCodeCell(string name)
    {
        var i = (uint)_executionBlocks.Count;
        var cell = new OmegaCodeCell(this, name, i);
        _executionBlocks.Add(cell);
        return cell;
    }
    protected override string ToFullDump()
    {
        var sb = new StringBuilder();
        
        if (ExportSymbol != null) sb.Append($"export \"{ExportSymbol}\" ");
        sb.Append($"func @{Name}(");
        sb.Append(string.Join(", ", Parameters));
        sb.Append($") {ReturnType?.ToString() ?? "void"} {{");
        foreach (var i in ExecutionBlocks) sb.Append($"\n{i:full}".TabAllLines());
        if (ExecutionBlocksCount > 0) sb.AppendLine();
        sb.Append('}');
        
        return sb.ToString();
    }
}
