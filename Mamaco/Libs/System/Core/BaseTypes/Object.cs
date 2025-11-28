using System.Diagnostics;

namespace System;

public class Object
{

    public Type GetType() => throw new UnreachableException();
    public virtual string ToString() => GetType().FullName;

}
