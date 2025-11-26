using Tq.Realizer.Core.Builder.References;
using Tq.Realizer.Core.Intermediate.Values;

namespace Tq.Realizeer.Core.Program.Builder;

public struct RealizerFieldBuilder
{
    
    private string _baseSymbol;
    private bool _isStatic;
    private TypeReference? _fieldType;
    private RealizerConstantValue? _initializer;
    
    
    public static RealizerFieldBuilder Create(string baseSymbol)
    {
        return new RealizerFieldBuilder { _baseSymbol = baseSymbol };
    }

    public RealizerFieldBuilder WithType(TypeReference typeref)
    {
        _fieldType = typeref;
        return this;
    }
    
    public RealizerFieldBuilder WithInitializer(RealizerConstantValue initializer)
    {
        _initializer = initializer;
        return this;
    }
    
    public RealizerFieldBuilder SetStatic(bool value)
    {
        _isStatic = value;
        return this;
    }
    
    public RealizerFieldBuilder AsStatic()
    {
        _isStatic = true;
        return this;
    }
    public RealizerFieldBuilder AsInstance()
    {
        _isStatic = false;
        return this;
    }

    public RealizerField Build() => new(_baseSymbol)
    {
        
        Type = _fieldType,
        Initializer = _initializer,
        Static = _isStatic
    };

}
