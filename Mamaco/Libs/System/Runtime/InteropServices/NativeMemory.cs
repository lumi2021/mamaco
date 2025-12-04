namespace System.Runtime.InteropServices;

public static class NativeMemory
{

  private static extern void* Sys_Malloc(nuint size);
  private static extern void Sys_Free(void* ptr);
  private static extern void* Sys_Realloc(void* ptr, UIntPtr new_size);
  
  internal static extern void* Sys_AlignedAlloc(UIntPtr alignment, UIntPtr size);
  internal static extern void Sys_AlignedFree(void* ptr);
  internal static extern void* Sys_AlignedRealloc(void* ptr, UIntPtr alignment, UIntPtr new_size);
  
  
  public static unsafe void* AlignedAlloc(UIntPtr byteCount, UIntPtr alignment) => Sys_AlignedAlloc(alignment, byteCount);
  public static unsafe void AlignedFree(void* ptr) => Sys_AlignedFree(ptr);
  public static unsafe void* AlignedRealloc(void* ptr, UIntPtr byteCount, UIntPtr alignment) => Sys_AlignedRealloc(ptr, alignment, byteCount);

  
  public static unsafe void* Alloc(UIntPtr byteCount) => Sys_Malloc(byteCount);
  public static unsafe void Free(void* ptr) => Sys_Free(ptr);
  public static unsafe void* Realloc(void* ptr, UIntPtr byteCount) => Sys_Realloc(ptr, byteCount);
}
