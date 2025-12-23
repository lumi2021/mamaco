namespace Tq.Realizer.Core.Program.Member;

public abstract class RealizerContainer: RealizerMember
{

    internal List<RealizerMember> _membersList = [];
    
    internal RealizerContainer(string name) : base(name) {}

    
    public IEnumerable<RealizerMember> GetMembers() => _membersList.AsEnumerable();
    public IEnumerable<T> GetMembers<T>() where T: RealizerMember => _membersList.OfType<T>().AsEnumerable();

    public void AddMember(RealizerMember member, int index = -1)
    {
        if (!CanAccept(member))
            throw new ArgumentException($"Cannot add member {member.Name} of " +
                                        $"type {member.GetType().Name} inside {this.GetType().Name}");
            
        member._parent = this;
        _membersList.Insert(index >= 0 ? index : _membersList.Count, member);
        if (member._globalIndex == 0) Module!.RegisterMember(member);
    }

    public void AddMembers(IEnumerable<RealizerMember> members)
    {
        foreach (var member in members) AddMember(member);
    }

    public void RemoveMember(RealizerMember member)
    {
        if (member._parent != this)
            throw new InvalidOperationException("Cannot remove member from container, member is not a child");
        
        _membersList.Remove(member);
        member._parent = null;
    }

    public void ReplaceMember(RealizerMember oldMember, RealizerMember newMember)
    {
        if (oldMember.GetType() != newMember.GetType())
            throw new ArgumentException("Cannot replace members of different types");
        
        if (oldMember._parent != this)
            throw new InvalidOperationException("Cannot remove member from container, member is not a child");
        
        if (!CanAccept(newMember))
            throw new ArgumentException($"Cannot add member {newMember.Name} of type {newMember.GetType().Name} inside {GetType().Name}");
        
        Module!.ReplaceMemberRegistry(oldMember, newMember);
        
        var index = _membersList.IndexOf(oldMember);
        _membersList[index] = newMember;
        
        newMember._parent = this;
        oldMember._parent = null;
    }
    
    protected virtual bool CanAccept(RealizerMember member) => true;
}
