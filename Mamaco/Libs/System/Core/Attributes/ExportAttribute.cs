namespace System;

[AttributeUsage(AttributeTargets.Method)]
public class ExportAttribute : Attribute
{
    public string? Symbol { get; init; }
    
    public ExportAttribute(string symbol) => Symbol = symbol;
    public ExportAttribute() => Symbol = null;
}
