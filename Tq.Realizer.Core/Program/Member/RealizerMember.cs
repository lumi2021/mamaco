using Tq.Realizeer.Core.Program.Builder;

namespace Tq.Realizeer.Core.Program.Member;

public abstract class RealizerMember : IFormattable
{
    internal RealizerContainer? _parent = null;
    
    public string Name { get; set; }
    public RealizerContainer? Parent => _parent;
    public string[] Global => _parent != null ? [.._parent.Global, Name] : [Name];
    public string GlobalString => string.Join('.', Global);

    public RealizerNamespace? Namespace => _parent switch
    {
        null => null,
        RealizerNamespace namesp => namesp,
        _ => _parent.Namespace
    };
    public RealizerModule? Module => _parent switch
    {
        null => null,
        RealizerModule module => module,
        _ => _parent.Module
    };
    
    public bool Static { get => GetStatic(); set => SetStatic(value); }
    
    internal RealizerMember(string name) => Name = name;


    protected virtual bool GetStatic() => true;
    protected virtual bool SetStatic(bool value)
        => throw new InvalidOperationException($"{GetType().Name} does not allows writing to {nameof(Static)}");


    public sealed override string ToString() => ToString(null, null);
    public string ToString(string? format, IFormatProvider? formatProvider) => format switch
    {
        "full" => ToFullDump(),
        _ => ToReadableReference()
    };

    protected abstract string ToFullDump();
    protected virtual string ToReadableReference() => $"@{GlobalString}";
}
