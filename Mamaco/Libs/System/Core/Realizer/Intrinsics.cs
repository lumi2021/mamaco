namespace System.Realizer;

public static class Intrinsics
{
    
    public extern static void* RealizerGetStructMetadataPointer(object? instance);
    public extern static string RealizerGetStructFullName(void* metadataPtr);
    
    public extern static void* RealizerGetObjectPointer(object? instance);

}