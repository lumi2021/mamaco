using System.Diagnostics;
using System.Realizer;

namespace System;

public class Object
{

    public Type GetType() => new Type(Intrinsics.RealizerGetStructMetadataPointer(this));
    public virtual string ToString() => GetType().FullName;

    
    public virtual bool Equals(object? obj) => Intrinsics.RealizerGetObjectPointer(this) == Intrinsics.RealizerGetObjectPointer(obj);
    public virtual int GetHashCode() => 0; //(int)Intrinsics.RealizerGetObjectPointer(this);
}
