namespace System.Reflection;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = true)]
public sealed class DefaultMemberAttribute : Attribute
{
    public DefaultMemberAttribute(string memberName) {}
}
