namespace Tq.Realizeer.Core.Program.Member;

public abstract class RealizerContainer: RealizerMember
{

    internal List<RealizerMember> _membersList = [];
    
    internal RealizerContainer(string name) : base(name) {}

    
    public IEnumerable<RealizerMember> GetMembers() => _membersList.AsEnumerable();
    public IEnumerable<T> GetMembers<T>() where T: RealizerMember => _membersList.OfType<T>().AsEnumerable();

    public void AddMember(RealizerMember member)
    {
        if (!CanAccept(member))
            throw new ArgumentException($"Cannot add member {member.Name} of " +
                                        $"type {member.GetType().Name} inside {this.GetType().Name}");
            
        member._parent = this;
        _membersList.Add(member);
    }

    public void AddMembers(IEnumerable<RealizerMember> members)
    {
        foreach (var member in members) AddMember(member);
    }

    public void RemoveMember(RealizerMember member)
    {
        if (member._parent != this)
            throw new InvalidOperationException("Cannot remove member from container," +
                                                "member is not a child");
        
        _membersList.Remove(member);
        member._parent = null;
    }
    
    protected virtual bool CanAccept(RealizerMember member) => true;
}
