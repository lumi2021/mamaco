using System.Diagnostics;
using System.Text;
using Tq.Realizeer.Core.Program.Builder;
using Tq.Realizeer.Core.Program.Member;
using Tq.Realizer.Core.Builder.References;
using Tq.Realizer.Core.Intermediate.Values;
using u16 = ushort;

namespace Tq.Realizer.Core.Builder.Language.Omega;

public static class OmegaInstructions
{

    public interface IOmegaInstruction {}

    public interface IOmegaExpression : IOmegaInstruction
    {
        public TypeReference? Type { get; }
    }
    public interface IOmegaAssignable : IOmegaExpression {}
    public interface IOmegaCallable : IOmegaExpression {}
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
    public readonly struct Register(TypeReference? type, u16 i) : IOmegaAssignable, IOmegaCallable
    {
        public TypeReference? Type => type;
        public readonly u16 Index = i;
        public override string ToString() => $"{Type} %{Index}";
    }
    public readonly struct Argument(RealizerParameter p) : IOmegaAssignable, IOmegaCallable
    {
        public TypeReference? Type => Parameter.Type;
        public readonly RealizerParameter Parameter = p;
        public override string ToString() => $"%{Parameter.Name}";
    }
    public readonly struct Member(RealizerMember m) : IOmegaAssignable, IOmegaCallable
    {
        public TypeReference Type => Node switch
        {
            RealizerFunction @f => f.ReturnType!,
            _ => throw new UnreachableException()
        };
        public readonly RealizerMember Node = m;
        public override string ToString() => Node.ToString();
    }
    public readonly struct Constant(RealizerConstantValue v) : IOmegaExpression
    {
        public TypeReference? Type => null!;
        public readonly RealizerConstantValue Value = v;
        public override string ToString() => $"{Value}";
    }
    public readonly struct Access(IOmegaExpression l, IOmegaExpression r) : IOmegaAssignable, IOmegaCallable
    {
        public TypeReference? Type => Right.Type;
        
        public readonly IOmegaExpression Left = l;
        public readonly IOmegaExpression Right = r;
        public override string ToString() => $"{Left}->{Right}";
    }
    public readonly struct Self() : IOmegaExpression
    {
        public TypeReference? Type => null;
        public override string ToString() => "self";
    }
    
    // Statemets
    public readonly struct Assignment(IOmegaAssignable l, IOmegaExpression r) : IOmegaInstruction
    {
        public readonly IOmegaAssignable Left = l;
        public readonly IOmegaExpression Right = r;
        public override string ToString() => $"{Left} = {Right}";
    }
    
    // Expressions
    public readonly struct Alloca(TypeReference type) : IOmegaExpression
    {
        public TypeReference Type => type;
        public override string ToString() => $"alloca {Type}";
    }
    public readonly struct Call(TypeReference? type, IOmegaCallable c, params IOmegaExpression[] args) : IOmegaExpression
    {
        public TypeReference Type => type;
        public readonly IOmegaCallable Callable = c;
        public readonly IOmegaExpression[] Arguments = args;
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append($"call {Type?.ToString() ?? "void"} {Callable}");
            sb.Append("(");
            sb.Append(string.Join(", ", Arguments));
            sb.Append(')');
            
            return sb.ToString();
        }
    }
    public readonly struct Add(TypeReference type, IOmegaExpression l, IOmegaExpression r) : IOmegaExpression
    {
        public TypeReference? Type => type;
        public readonly IOmegaExpression Left = l;
        public readonly IOmegaExpression Right = r;
        public override string ToString() => $"{Type} add {Left}, {Right}";
    }
    public readonly struct Mul(TypeReference type, IOmegaExpression l, IOmegaExpression r) : IOmegaExpression
    {
        public TypeReference? Type => type;
        public readonly IOmegaExpression Left = l;
        public readonly IOmegaExpression Right = r;
        public override string ToString() => $"{Type} mul {Left}, {Right}";
    }
    
    // Control flow
    public readonly struct Ret(IOmegaExpression? value) : IOmegaBranch
    {
        public readonly IOmegaExpression? Value = value;
        public override string ToString() => "ret" + (Value == null ? "" : $" {Value}");
    }
    public readonly struct Throw(IOmegaExpression fault) : IOmegaBranch
    {
        public readonly IOmegaExpression Fault = fault;
        public override string ToString() => $"throw {Fault}";
    }
}
