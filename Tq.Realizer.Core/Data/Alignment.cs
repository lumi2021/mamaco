namespace Tq.Realizer.Data;

public readonly struct Alignment
{
    private readonly bool _nativeMultiplyer;
    private readonly ushort _value;

    public static readonly Alignment Zero = new(true, 0);
    public static readonly Alignment PointerSized = new(true, 1);
    
    public Alignment() => throw new InvalidOperationException("Create this instance with a helper method!");

    private Alignment(bool nat, int val)
    {
        _nativeMultiplyer = nat;
        _value = (ushort)val;
    }
    
    public int ToInt(int? nativeSize = null)
    {
        if (!_nativeMultiplyer) return _value;
        
        if (!nativeSize.HasValue)
            throw new NotSupportedException($"Cannot convert native-sized alignment without providing native size");
        return _value * nativeSize.Value;

    }
    
    public static Alignment WithValue(int value) => new (false, value);
    public static Alignment WithValueNativeSized(int value) => new (true, value);
    
    public static implicit operator Alignment(int value) => new(false, value);
    
    public static implicit operator int(Alignment value) => value.ToInt();
    public static implicit operator short(Alignment value) => (short)value.ToInt();
    public static implicit operator ushort(Alignment value) => (ushort)value.ToInt();
    public static implicit operator byte(Alignment value) => (byte)value.ToInt();

    public override string ToString() => (_nativeMultiplyer ? "ptr * " : "") + $"{_value}";
}
