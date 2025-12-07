using System.Text;
using Tq.Realizeer.Core.Program.Builder;
using Tq.Realizer.Core.Builder.Language.Omega;
using Tq.Realizer.Core.Builder.References;
using IS = Tq.Realizer.Core.Builder.Execution.Omega.OmegaCodeCell.InstructionWriter;
using static Tq.Realizer.Core.Builder.Language.Omega.OmegaInstructions;

namespace Tq.Realizer.Core.Builder.Execution.Omega;

public class OmegaCodeCell(RealizerFunction s, string n, uint idx) : CodeCell(s, n, idx)
{
    private List<IOmegaInstruction> _instructions = [];
    public IOmegaInstruction[] Instructions => [.. _instructions];
    public InstructionWriter Writer => new(this);
    private ushort _nextReg = 0;

    public void OverrideInstructions(IOmegaInstruction[] instructions) => _instructions = [.. instructions];
    public override bool IsFinished() => _instructions.Count > 0 && _instructions[^1] is IOmegaBranch;
    public override string DumpInstructionsToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{Name}:");
        foreach (var i in Instructions)
            sb.AppendLine(i.ToString().TabAllLines());
        return sb.ToString();
    }
    
    
    public readonly struct InstructionWriter
    {
        private readonly OmegaCodeCell _parentBuilder;
        internal InstructionWriter(OmegaCodeCell builder) => _parentBuilder = builder;
    
        
        private IS AppendInstruction(IOmegaInstruction value)
        {
            _parentBuilder._instructions.Add(value);
            return this;
        }
        
        public IS Nop() => AppendInstruction(new Nop());
        public IS Unreachable() => AppendInstruction(new Invalid());
        public IS Invalid() => AppendInstruction(new Invalid());
        
        public IS Assignment(IOmegaAssignable left, IOmegaExpression right) => AppendInstruction(new Assignment(left, right));
        public IS Call(IOmegaCallable callable, params IOmegaExpression[] args) => AppendInstruction(new Call(callable, args));

        public IS Ret(IOmegaExpression? value = null) => AppendInstruction(new Ret(value));
        public IS Throw(IOmegaExpression? value) => AppendInstruction(new Throw(value));

        public Register GetNewRegister(TypeReference t) => new Register(t, _parentBuilder._nextReg++);
    }
    
}
