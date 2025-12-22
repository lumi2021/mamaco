using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System;

public class String
{
    public int Length { get; }

    public extern String(char* pointer, int length);

    public char this[int index]
    {
        get { throw new UnreachableException(); }
    }
    
    public static string Concat(string str0, string str1)
    {
        var totalLength = str0.Length + str1.Length;
        var memory = (char*)NativeMemory.AlignedAlloc(totalLength, 1);

        var i = 0;
        var j = 0;
        
        while (i < str0.Length) memory[i++] = str0[j++]; j = 0;
        while (i < str1.Length) memory[i++] = str1[j++];
        
        var newstr = new string(memory, totalLength);
        return newstr;
    }
}
