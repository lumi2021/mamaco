using Tq.Realizer.Core.Builder.References;
using Tq.Realizer.Core.Intermediate.Values;

namespace Tq.Realizeer.Core.Program.Builder;

public struct RealizerTypedefBuilder()
{

    private string _baseSymbol;
    private TypeReference _backedType;
    private List<RealizerTypedefEntry> _entries = [];
        
    public static RealizerTypedefBuilder Create(string baseSymbol)
    {
        return new RealizerTypedefBuilder() { _baseSymbol = baseSymbol };
    }

    public RealizerTypedefBuilder WithBackedTtype(TypeReference typeref)
    {
        _backedType = typeref;
        return this;
    }
    public RealizerTypedefBuilder WithNamedEntry(string name, RealizerConstantValue? value = null)
    {
        _entries.Add(new RealizerTypedefNamedEntry(name, value));
        return this;
    }
    public RealizerTypedef Build() => new RealizerTypedef(_baseSymbol, [.._entries]);
    
    
}
