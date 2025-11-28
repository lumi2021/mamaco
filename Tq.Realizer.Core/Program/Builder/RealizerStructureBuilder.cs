namespace Tq.Realizeer.Core.Program.Builder;

public struct RealizerStructureBuilder
{

    private string _baseSymbol;
    
    public static RealizerStructureBuilder Create(string baseSymbol)
    {
        return new RealizerStructureBuilder() { _baseSymbol = baseSymbol };
    }
    
    public RealizerStructure Build() => new(_baseSymbol);
    
    
}
