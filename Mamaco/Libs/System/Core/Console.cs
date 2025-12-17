namespace System;

public static class Console
{
    extern public static void __write(nuint fd, string content);
    
    public static void Write(string content) => __write(0, content);
    public static void Write(long content) => __write(0, content.ToString());
    public static void Write(ulong content) => __write(0, content.ToString());
    
    public static void WriteLine(string content) => __write(0, content + "\n");
    public static void WriteLine(long content) => __write(0, content.ToString() + "\n");
    public static void WriteLine(ulong content) => __write(0, content.ToString() + "\n");
}
