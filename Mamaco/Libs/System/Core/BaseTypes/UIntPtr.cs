using System.Diagnostics;

namespace System;

public readonly struct UIntPtr
{
    
    public static implicit operator UIntPtr(int value) { throw new UnreachableException(); }
}
