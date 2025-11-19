namespace System;

public sealed class AttributeUsageAttribute(AttributeTargets validOn) : Attribute
{
    public AttributeTargets ValidOn { get; init; } = validOn;
    public bool AllowMultiple { get; set; }
    public bool Inherited { get; set; }
}
