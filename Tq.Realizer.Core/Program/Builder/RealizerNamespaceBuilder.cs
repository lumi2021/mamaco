namespace Tq.Realizer.Core.Program.Builder;

public struct RealizerNamespaceBuilder
{
    
    private string _baseSymbol;
    
    
    public static RealizerNamespaceBuilder Create(string baseSymbol)
    {
        return new RealizerNamespaceBuilder { _baseSymbol = baseSymbol };
    }

    public RealizerNamespace Build() => new(_baseSymbol);
    
}
