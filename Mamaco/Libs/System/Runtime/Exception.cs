namespace System;

/// <summary>
/// Represents errors that occur during application execution.
/// </summary>
//[Serializable]
public class Exception //: ISerializable
{

    private string? _message;
    private Exception _innerException;

    public Exception(string? message) : this(message, null) {}
    public Exception(string? message, Exception innerException)
    {
        this._message = message; 
        this._innerException = innerException;
    }



    public override string ToString()
    {
        return "TODO";
    }

}
