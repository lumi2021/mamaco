namespace System;

public class ArgumentException: Exception
{
    
    public ArgumentException() : base(null, null) {}
    public ArgumentException(string? message) : base(message, null) {}
    public ArgumentException(string? message, Exception innerException) : base(message, innerException) {}

    
    public override string ToString() => "Exception: Idk what this one specifically is";
}
