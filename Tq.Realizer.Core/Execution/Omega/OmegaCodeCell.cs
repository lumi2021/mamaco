using System.Text;
using Tq.Realizer;
using Tq.Realizer.Core.Builder.Execution;
using Tq.Realizer.Core.Builder.References;
using Tq.Realizer.Core.Program.Builder;
using static Tq.Realizer.Core.Execution.Omega.OmegaInstructions;

namespace Tq.Realizer.Core.Execution.Omega;

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
    
        
        private InstructionWriter AppendInstruction(IOmegaInstruction value)
        {
            _parentBuilder._instructions.Add(value);
            return this;
        }
        
        public InstructionWriter Nop() => AppendInstruction(new Nop());
        public InstructionWriter Unreachable() => AppendInstruction(new Invalid());
        public InstructionWriter Invalid() => AppendInstruction(new Invalid());
        
        public InstructionWriter Assignment(IOmegaAssignable left, IOmegaExpression right) => AppendInstruction(new Assignment(left, right));
        public InstructionWriter Call(IOmegaCallable callable, params IOmegaExpression[] args) => AppendInstruction(new Call(callable, args));
        public InstructionWriter IntrinsicCall(IntrinsicFunctions func, params IOmegaExpression[] args) => AppendInstruction(new CallIntrinsic(func, args));

        public InstructionWriter Ret(IOmegaExpression? value = null) => AppendInstruction(new Ret(value));
        public InstructionWriter Branch(CodeCell block) => AppendInstruction(new Branch(block));
        public InstructionWriter CBranch(IOmegaExpression condition, CodeCell iftrue, CodeCell iffalse)
            => AppendInstruction(new CBranch(condition, iftrue, iffalse));
        public Register GetNewRegister(TypeReference t) => new Register(t, _parentBuilder._nextReg++);
    }
    
}
