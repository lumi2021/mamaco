namespace System.InteropServices;

public static class NativeMemory
{

  public extern static void* AlignedAlloc(UIntPtr length, UIntPtr alignment);
  public extern static void AlignedFree(void* ptr);
  public extern static void* AlignedRealloc(void* ptr, UIntPtr length, UIntPtr alignment);

  public extern static void* Alloc(UIntPtr length);
  public extern static void Free(void* ptr);
  public extern static void* Realloc(void* ptr, UIntPtr length);
}