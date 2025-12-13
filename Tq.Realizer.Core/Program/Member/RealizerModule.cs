using System.Text;
using Tq.Realizeer.Core.Program.Member;
using Tq.Realizer;

namespace Tq.Realizeer.Core.Program.Builder;

public class RealizerModule: RealizerContainer
{
    internal RealizerModule(string name) : base(name) { }
    
    private Dictionary<int, RealizerMember> _allMembersDictionary = [];
    
    
    internal void RegisterMember(RealizerMember member)
    {
        var index = _allMembersDictionary.Count;
        member._globalIndex = index+1;
        _allMembersDictionary.Add(index, member);
    }
    internal void UnregisterMember(RealizerMember member)
    {
        _allMembersDictionary[member._globalIndex-1] = null!;
        member._globalIndex = 0;
    }
    internal void ReplaceMemberRegistry(RealizerMember old, RealizerMember member)
    {
        var index = old._globalIndex;
        _allMembersDictionary[index-1] = member;
        member._globalIndex = index;
        old._globalIndex = 0;
    }
    internal RealizerMember GetMemberByGlobalIndex(int globalIndex) => _allMembersDictionary[globalIndex-1];
    
    
    protected override bool GetStatic() => true;
    protected override string ToFullDump()
    {
        var sb = new StringBuilder();

        sb.Append($"module @{Name} {{");
        foreach (var i in GetMembers())
            sb.AppendLine($"{Environment.NewLine}{i.ToString("full", null).TabAllLines()}");
        sb.Append('}');
        
        return sb.ToString();
    }
}
