namespace Tq.Realizeer.Core.Program.Builder;

public struct RealizerModuleBuilder
{
    
    private string _baseSymbol;
    
    
    public static RealizerModuleBuilder Create(string baseSymbol)
    {
        return new RealizerModuleBuilder { _baseSymbol = baseSymbol };
    }

    public RealizerModule Build() => new(_baseSymbol);
    
}
