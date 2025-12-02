using System.Text;
using Tq.Realizeer.Core.Program.Member;
using Tq.Realizer.Core.Builder.References;
using Tq.Realizer.Core.Intermediate.Values;
using Tq.Realizer.Data;

namespace Tq.Realizeer.Core.Program.Builder;

public class RealizerField: RealizerMember
{
    private bool _static = false;
    
    private Alignment _bitAlignment = 0;
    private Alignment _bitOffset = 0;
    
    public TypeReference? Type = null;
    public RealizerConstantValue? Initializer = null;
    
    internal RealizerField(string name) : base(name) { }

    public Alignment BitAlignment => _bitAlignment;
    public Alignment BitOffset => _bitOffset;
    public Alignment BitLength => Type.Length;
    public uint Index { get; private set; }
    

    protected override bool GetStatic() => _static;
    protected override void SetStatic(bool value) => _static = value;


    public void OverrideBitAlignment(Alignment value) => _bitAlignment = value;
    public void OverrideBitOffset(Alignment value) => _bitOffset = value;
    public void OverrideIndex(uint value) => Index = value;
    
    protected override string ToFullDump()
    {
        var sb = new StringBuilder();

        if (_parent is RealizerStructure)
            sb.AppendLine($"; index = {Index}" +
                          $" bitAlignment = {BitAlignment}" +
                          $" bitOffset = {BitOffset} " +
                          $" bitLength = {BitLength}");
        
        sb.Append($"field {Type?.ToString() ?? "void"} @{Name}");
        if (Initializer != null) sb.Append($" = {Initializer}");
        
        return sb.ToString();
    }
}
