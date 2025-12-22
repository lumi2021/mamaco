using System.Diagnostics;
using System.Text;
using Tq.Realizeer.Core.Program.Builder;
using Tq.Realizeer.Core.Program.Member;
using Tq.Realizer.Core.Builder.Execution;
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

    public interface IOmegaCallable : IOmegaExpression
    {
        public new CallableTypeReference Type { get; }
    }
    public interface IOmegaBranch : IOmegaInstruction {}
    
    
    // Misc
    public class Invalid : IOmegaInstruction
    {
        public override string ToString() => "unreachable";
    }
    public class Nop : IOmegaInstruction
    {
        public override string ToString() => "nop";
    }

    // Values
    public class Member(RealizerMember m) : IOmegaAssignable, IOmegaCallable
    {
        private readonly RealizerModule _nodeModule = m.Module ?? throw new ArgumentException("Member is not attached to a module!");
        private readonly WeakReference<RealizerMember> _nodeWeakRef = new(m);
        private readonly int _memberGlobalIndex = m._globalIndex;
        
        public RealizerMember Node
        {
            get
            {
                if (_nodeWeakRef.TryGetTarget(out var target)) return target;
                var node = _nodeModule.GetMemberByGlobalIndex(_memberGlobalIndex);
                _nodeWeakRef.SetTarget(node);
                return node;
            }
        }
        
        public TypeReference Type => Node switch
        {
            RealizerFunction @f => new CallableTypeReference(f),
            RealizerField @f => f.Type!,
            _ => throw new UnreachableException()
        };
        CallableTypeReference IOmegaCallable.Type => (CallableTypeReference)Type!;
        
        public override string ToString() => Node.ToString();
    }
    
    public class Register(TypeReference? type, u16 i) : IOmegaAssignable, IOmegaCallable
    {
        public TypeReference? Type => type;
        CallableTypeReference IOmegaCallable.Type => (CallableTypeReference)Type!;
        
        public readonly u16 Index = i;
        public override string ToString() => $"{Type} %{Index}";

    }
    public class Argument(RealizerParameter p) : IOmegaAssignable, IOmegaCallable
    {
        public TypeReference? Type => Parameter.Type;
        CallableTypeReference IOmegaCallable.Type => (CallableTypeReference)Type!;
        
        public readonly RealizerParameter Parameter = p;
        public override string ToString() => $"%{Parameter.Name}";
    }
    public class Constant(RealizerConstantValue v) : IOmegaExpression
    {
        public TypeReference? Type => null!;
        public readonly RealizerConstantValue Value = v;
        public override string ToString() => $"{Value}";
    }
    public class Access(IOmegaExpression l, Member r) : IOmegaAssignable, IOmegaCallable
    {
        public TypeReference? Type => Right.Type;
        CallableTypeReference IOmegaCallable.Type => (CallableTypeReference)Type!;
        
        public readonly IOmegaExpression Left = l ?? throw new UnreachableException();
        public readonly Member Right = r ?? throw new UnreachableException();
        public override string ToString() => $"{Left}->{Right}";
    }
    public class Self() : IOmegaExpression
    {
        public TypeReference? Type => null;
        public override string ToString() => "self";
    }
    
    public class Ref(IOmegaExpression exp) : IOmegaExpression
    {
        public TypeReference? Type => new ReferenceTypeReference(Expression.Type);
        public readonly IOmegaExpression Expression = exp;
        public override string ToString() => $"ref {Expression}";
    }
    public class Val(IOmegaExpression exp) : IOmegaExpression
    {
        public TypeReference? Type => ((ReferenceTypeReference)exp.Type!).Subtype;

        public readonly IOmegaExpression Expression = exp is { Type: ReferenceTypeReference }
            ? exp : throw new ArgumentException();

        public override string ToString() => $"val {Expression}";
    }
    public class Typeof(IOmegaExpression exp) : IOmegaExpression
    {
        public TypeReference? Type => new MetadataTypeReference(Expression.Type!);
        public readonly IOmegaExpression Expression = exp;
        public override string ToString() => $"typeof {Expression}";
    }
    public class LenOf(IOmegaExpression exp) : IOmegaExpression
    {
        public TypeReference? Type => IntegerTypeReference.NUInt;
        public readonly IOmegaExpression Expression = exp.Type is SliceTypeReference
            ? exp : throw new ArgumentException();
        public override string ToString() => $"lenof {Expression}";
}
    
    // Statemets
    public class Assignment(IOmegaAssignable l, IOmegaExpression r) : IOmegaInstruction
    {
        public readonly IOmegaAssignable Left = l;
        public readonly IOmegaExpression Right = r;
        public override string ToString() => $"{Left} = {Right}";
    }
    
    // Expressions
    public class Alloca(TypeReference type) : IOmegaExpression
    {
        public TypeReference Type => new ReferenceTypeReference(AllocaType);
        public readonly TypeReference AllocaType = type;
        public override string ToString() => $"alloca {AllocaType}";
    }
    public class Slice(TypeReference elmType, IOmegaExpression ptr, IOmegaExpression len): IOmegaExpression
    {
        public TypeReference Type => new SliceTypeReference(ElementType);
        public readonly TypeReference ElementType = elmType;

        public IOmegaExpression Pointer = ptr.Type is ReferenceTypeReference ? ptr : throw new ArgumentException();
        public IOmegaExpression Lenght = len;
        
        public override string ToString() => $"{Pointer}[..{Lenght}]";
    } 
        
    public class Call(IOmegaCallable c, params IOmegaExpression[] args) : IOmegaExpression
    {
        public TypeReference? Type => Callable.Type.ReturnType;
        
        public readonly IOmegaCallable Callable = c;
        public readonly IOmegaExpression[] Arguments = args;
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append($"call {Callable.Type.ReturnType?.ToString() ?? "void"} {Callable}");
            sb.Append("(");
            sb.Append(string.Join(", ", Arguments));
            sb.Append(')');
            
            return sb.ToString();
        }
    }
    public class Add(TypeReference type, IOmegaExpression l, IOmegaExpression r) : IOmegaExpression
    {
        public TypeReference? Type => type;
        public readonly IOmegaExpression Left = l;
        public readonly IOmegaExpression Right = r;
        public override string ToString() => $"{Type} add {Left}, {Right}";
    }
    public class Mul(TypeReference type, IOmegaExpression l, IOmegaExpression r) : IOmegaExpression
    {
        public TypeReference? Type => type;
        public readonly IOmegaExpression Left = l;
        public readonly IOmegaExpression Right = r;
        public override string ToString() => $"{Type} mul {Left}, {Right}";
    }

    public class Indexer(IOmegaExpression slice, IOmegaExpression index) : IOmegaExpression, IOmegaAssignable
    {
        public TypeReference? Type => ((SliceTypeReference)slice.Type!).Subtype;
        public readonly IOmegaExpression Slice = slice;
        public readonly IOmegaExpression Index = index;
        
        public override string ToString() => $"{Slice}[{Index}]";
    }
    
    public class Cmp(ComparisonOperation op, IOmegaExpression l, IOmegaExpression r) : IOmegaExpression
    {
        public TypeReference? Type => new IntegerTypeReference(false, 1);
        public readonly IOmegaExpression Left = l;
        public readonly IOmegaExpression Right = r;
        public readonly ComparisonOperation Op = op;

        public override string ToString() => $"cmp {Op} {Left}, {Right}";
    }
    
    public class IntTypeCast(IntegerTypeReference toType, IOmegaExpression exp) : IOmegaExpression
    {
        public TypeReference? Type => toType;
        public readonly IOmegaExpression Exp = exp.Type is IntegerTypeReference
            ? exp : throw new ArgumentException();

        public override string ToString() => $"{Exp} as {Type}";
    }
    public class PtrTypeCast(ReferenceTypeReference toType, IOmegaExpression exp) : IOmegaExpression
    {
        public TypeReference? Type => toType;
        public readonly IOmegaExpression Exp = exp.Type is ReferenceTypeReference
            ? exp : throw new ArgumentException();

        public override string ToString() => $"{Exp} as {Type}";
    }
    public class IntFromPtr(IntegerTypeReference toType, IOmegaExpression exp) : IOmegaExpression
    {
        public TypeReference? Type => toType;
        public readonly IOmegaExpression Expression = exp.Type is ReferenceTypeReference
            ? exp : throw new ArgumentException();

        public override string ToString() => $"{Expression} as {Type}";
    }
    public class PtrFromInt(ReferenceTypeReference toType, IOmegaExpression exp) : IOmegaExpression
    {
        public TypeReference? Type => toType;
        public readonly IOmegaExpression Expression = exp.Type is IntegerTypeReference
            ? exp : throw new ArgumentException();

        public override string ToString() => $"{Expression} as {Type}";
    }

    
    // Control flow
    public class Ret(IOmegaExpression? value) : IOmegaBranch
    {
        public readonly IOmegaExpression? Value = value;
        public override string ToString() => "ret" + (Value == null ? "" : $" {Value}");
    }
    public class Throw(IOmegaExpression fault) : IOmegaBranch
    {
        public readonly IOmegaExpression Fault = fault;
        public override string ToString() => $"throw {Fault}";
    }
    public class Branch: IOmegaBranch
    {
        public readonly uint Cell;

        public Branch(CodeCell cell) => Cell = cell.Index;
        public Branch(uint cellIndex) => Cell = cellIndex;
        
        public override string ToString() => $"branch {Cell}";
    }
    public class CBranch : IOmegaBranch
    {
        public readonly IOmegaExpression Expression;
        public readonly uint IfTrue;
        public readonly uint IfFalse;

        public CBranch(IOmegaExpression expression, CodeCell iftrue, CodeCell iffalse)
        {
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
            IfTrue = iftrue.Index;
            IfFalse = iffalse.Index;
        }
        public CBranch(IOmegaExpression expression, uint iftrue, uint iffalse)
        {
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
            IfTrue = iftrue;
            IfFalse = iffalse;
        }
        
        public override string ToString() => $"cbranch {Expression}, {IfTrue}, {IfFalse}";
    }

    // Misc
    public class CallIntrinsic(IntrinsicFunctions func, params IOmegaExpression[] args) : IOmegaExpression
    {
        public TypeReference? Type => null;
        public IntrinsicFunctions Function = func;
        public readonly IOmegaExpression[] Arguments = args;

        public override string ToString() => $"call {Function} ({string.Join(", ", Arguments)})";
            
    }
    
    
    public enum ComparisonOperation
    {
        Equal,
        NotEqual,
        
        SignedLessThan,
        SignedLessThanOrEqual,
        SignedGreaterThan,
        SignedGreaterThanOrEqual,
        
        UnsignedLessThan,
        UnsignedLessThanOrEqual,
        UnsignedGreaterThan,
        UnsignedGreaterThanOrEqual,
    }
    public enum IntrinsicFunctions : byte
    {
        initFields,
        
    }
}
