using System.Text;
using Tq.Realizeer.Core.Program.Member;
using Tq.Realizer;
using Tq.Realizer.Core.Builder.Execution;
using Tq.Realizer.Core.Builder.Execution.Omega;
using Tq.Realizer.Core.Builder.References;

namespace Tq.Realizeer.Core.Program.Builder;

public class RealizerFunction: RealizerContainer
{
    private bool _static;
    
    public RealizerParameter[] Parameters { get; private set; }
    public TypeReference? ReturnType = null;

    private List<CodeCell> _executionBlocks = [];
    public IEnumerable<CodeCell> ExecutionBlocks => _executionBlocks.AsEnumerable();
    public int ExecutionBlocksCount => _executionBlocks.Count;
    
    
    internal RealizerFunction(string name, RealizerParameter[]  parameters) : base(name) => Parameters = parameters;


    protected override bool GetStatic() => _static;
    protected override bool SetStatic(bool value) => _static = value;

    public RealizerParameter AddParameter(string name, TypeReference type)
    {
        var p = new RealizerParameter(name, type);
        AddParameter(p);
        return p;
    }
    public void AddParameter(RealizerParameter parameter)
    {
        Parameters = [..Parameters.Append(parameter)];
    }


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

        sb.Append($"func @{Name}(");

        string.Join(", ", Parameters.Select(e => $"{e}"));
        
        sb.Append($") {ReturnType?.ToString() ?? "void"} {{");
        foreach (var i in ExecutionBlocks) sb.Append($"\n{i:full}".TabAllLines());
        if (ExecutionBlocksCount > 0) sb.AppendLine();
        sb.Append('}');
        
        return sb.ToString();
    }
}
