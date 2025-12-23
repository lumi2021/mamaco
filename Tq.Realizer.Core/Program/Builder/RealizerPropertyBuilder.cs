using Tq.Realizer.Core.Builder.References;
using Tq.Realizer.Core.Intermediate.Values;

namespace Tq.Realizer.Core.Program.Builder;

public struct RealizerPropertyBuilder
{
    
    private string _baseSymbol;
    private bool _isStatic;
    private TypeReference? _fieldType;
    private RealizerFunction? _getter;
    private RealizerFunction? _setter;
    
    
    public static RealizerPropertyBuilder Create(string baseSymbol)
    {
        return new RealizerPropertyBuilder { _baseSymbol = baseSymbol };
    }

    public RealizerPropertyBuilder WithType(TypeReference typeref)
    {
        _fieldType = typeref;
        return this;
    }
    
    public RealizerPropertyBuilder WithGetter(RealizerFunction getter)
    {
        _getter = getter;
        return this;
    }
    public RealizerPropertyBuilder WithSetter(RealizerFunction setter)
    {
        _setter = setter;
        return this;
    }
    
    public RealizerPropertyBuilder SetStatic(bool value)
    {
        _isStatic = value;
        return this;
    }
    
    public RealizerPropertyBuilder AsStatic()
    {
        _isStatic = true;
        return this;
    }
    public RealizerPropertyBuilder AsInstance()
    {
        _isStatic = false;
        return this;
    }

    public RealizerProperty Build() => new(_baseSymbol)
    {
        Type = _fieldType,
        Getter =  _getter,
        Setter =  _setter,
        Static = _isStatic
    };

}
