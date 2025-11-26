using Tq.Realizeer.Core.Program.Member;
using Tq.Realizer.Core.Builder.References;
using Tq.Realizer.Core.Intermediate.Values;

namespace Tq.Realizeer.Core.Program.Builder;

public struct RealizerFunctionBuilder()
{
    
    private string _baseSymbol;
    private bool _isStatic;
    private TypeReference? _returnType;
    private List<RealizerParameter> _parameters = [];
    
    
    public static RealizerFunctionBuilder Create(string baseSymbol)
    {
        return new RealizerFunctionBuilder { _baseSymbol = baseSymbol };
    }

    public RealizerFunctionBuilder WithReturnType(TypeReference typeref)
    {
        _returnType = typeref;
        return this;
    }
    
    public RealizerFunctionBuilder SetStatic(bool value)
    {
        _isStatic = value;
        return this;
    }
    public RealizerFunctionBuilder AsStatic()
    {
        _isStatic = true;
        return this;
    }
    public RealizerFunctionBuilder AsInstance()
    {
        _isStatic = false;
        return this;
    }

    public RealizerFunctionBuilder WithParameter(string name, TypeReference typeref)
    {
        _parameters.Add(new RealizerParameter(name, typeref));
        return this;
    }
    public RealizerFunctionBuilder WithParameter(RealizerParameter parameter)
    {
        _parameters.Add(parameter);
        return this;
    }
    public RealizerFunctionBuilder WithParameters(params RealizerParameter[] parameters)
    {
        _parameters.AddRange(parameters);
        return this;
    }
    
    public RealizerFunction Build() => new(_baseSymbol, [.. _parameters])
    {
        ReturnType = _returnType,
        Static = _isStatic
    };

}
