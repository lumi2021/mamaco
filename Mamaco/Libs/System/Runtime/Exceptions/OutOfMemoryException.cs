namespace System;

public class OutOfMemoryException: Exception
{
    
    public OutOfMemoryException() : base(null, null) {}
    public OutOfMemoryException(string? message) : base(message, null) {}
    public OutOfMemoryException(string? message, Exception innerException) : base(message, innerException) {}

    
    public override string ToString() => "Exception: Out Of Memory";
    
}
