using System.Diagnostics;
using System.Realizer;

namespace System;

public class Type
{

    private void* metadataPtr;
    
    public string Name { get => Intrinsics.RealizerGetStructFullName(metadataPtr); }
    public string FullName { get => Intrinsics.RealizerGetStructFullName(metadataPtr); }

    
    internal Type(void* metadataPtr) => this.metadataPtr = metadataPtr;
}
