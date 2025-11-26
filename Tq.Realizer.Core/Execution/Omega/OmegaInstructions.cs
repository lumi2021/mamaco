using System.Text;
using Tq.Realizeer.Core.Program.Member;
using Tq.Realizer.Core.Intermediate.Values;
using i16 = short;
using u1 = bool;
using u128 = System.UInt128;
using u16 = ushort;
using u32 = uint;
using u64 = ulong;
using u8 = byte;

namespace Tq.Realizer.Core.Builder.Language.Omega;

public static class OmegaInstructions
{

    public interface IOmegaInstruction {}
    
    public interface IOmegaValue : IOmegaInstruction {}
    public interface IOmegaAssignable : IOmegaValue {}
    public interface IOmegaCallable : IOmegaValue {}
    public interface IOmegaBranch : IOmegaInstruction {}
    
    
    // Misc
    public readonly struct Invalid : IOmegaInstruction
    {
        public override string ToString() => "unreachable";
    }
    public readonly struct Nop : IOmegaInstruction
    {
        public override string ToString() => "nop";
    }

    // Values
    public readonly struct Register(u16 i) : IOmegaAssignable, IOmegaCallable
    {
        public readonly u16 Index = i;
        public override string ToString() => $"%{Index}";
    }
    public readonly struct Argument(RealizerParameter p) : IOmegaAssignable, IOmegaCallable
    {
        public readonly RealizerParameter Parameter = p;
        public override string ToString() => $"%{Parameter.Name}";
    }
    public readonly struct Member(RealizerMember m) : IOmegaAssignable, IOmegaCallable
    {
        public readonly RealizerMember Node = m;
        public override string ToString() => Node.ToString();
    }
    public readonly struct Constant(RealizerConstantValue v) : IOmegaValue
    {
        public readonly RealizerConstantValue Value = v;
        public override string ToString() => $"{Value}";
    }
    public readonly struct Access(IOmegaValue l, IOmegaValue r) : IOmegaAssignable, IOmegaCallable
    {
        public readonly IOmegaValue Left = l;
        public readonly IOmegaValue Reft = r;
        public override string ToString() => $"{Left}->{Reft}";
    }
    public readonly struct Self() : IOmegaValue
    {
        public override string ToString() => "self";
    }
    
    // Statemets
    public readonly struct Assignment(IOmegaAssignable l, IOmegaValue r) : IOmegaInstruction
    {
        public readonly IOmegaAssignable Left = l;
        public readonly IOmegaValue Right = r;
        public override string ToString() => $"{Left} = {Right}";
    }
    
    public readonly struct Call(IOmegaCallable c, params IOmegaValue[] args) : IOmegaValue
    {
        public readonly IOmegaCallable Callable = c;
        public readonly IOmegaValue[] Arguments = args;
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append(Callable);
            sb.Append(" (");
            string.Join(", ", Arguments);
            sb.Append(')');
            
            return sb.ToString();
        }
    }
    
    // Expressions
    public readonly struct Add(IOmegaValue l, IOmegaValue r) : IOmegaValue
    {
        public readonly IOmegaValue Left = l;
        public readonly IOmegaValue Right = r;
        public override string ToString() => $"{Left} + {Right}";
    }
    public readonly struct Mul(IOmegaValue l, IOmegaValue r) : IOmegaValue
    {
        public readonly IOmegaValue Left = l;
        public readonly IOmegaValue Right = r;
        public override string ToString() => $"{Left} * {Right}";
    }
    
    // Control flow
    public readonly struct Ret(IOmegaValue? value) : IOmegaBranch
    {
        public readonly IOmegaValue? Value = value;
        public override string ToString() => "ret" + (Value == null ? "" : $" {Value}");
    }
}

