namespace Tq.Realizeer.Core.Program.Member;

public abstract class RealizerMember : IFormattable
{
    internal string _name;
    internal RealizerContainer? _parent = null;
    
    public string Name => _name;
    public RealizerContainer? Parent => _parent;
    public string[] Global => _parent != null ? [.._parent.Global, _name] : [_name];
    public string GlobalString => string.Join('.', Global);

    public bool Static { get => GetStatic(); set => SetStatic(value); }
    
    internal RealizerMember(string name) => _name = name;


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
    protected virtual string ToReadableReference() => $"@{Name}";
}
