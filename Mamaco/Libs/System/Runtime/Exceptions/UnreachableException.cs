namespace System;

public class UnreachableException: Exception
{
    
    public UnreachableException() : base(null, null) {}
    public UnreachableException(string? message) : base(message, null) {}
    public UnreachableException(string? message, Exception innerException) : base(message, innerException) {}

    
    public override string ToString() => "Exception: Trying to execute unreachable code";
    
}
