using System.Runtime.InteropServices;

namespace System;

public class String
{
    public int Length { get; }
    
    public static string Concat(string str0, string str1)
    {
        var totalLength = str0.Length + str1.Length;
        var memory = NativeMemory.AlignedAlloc(totalLength, 1);
        
        return "todo\n";
    }
}
