using System.Diagnostics;

namespace System;

public readonly struct UIntPtr
{
    
    public static implicit operator UIntPtr(int value) { throw new UnreachableException(); }
    public static implicit operator UIntPtr(void* value) { throw new UnreachableException(); }
}
