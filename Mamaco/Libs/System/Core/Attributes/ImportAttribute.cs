namespace System;

[AttributeUsage(AttributeTargets.Method)]
public class ImportAttribute : Attribute
{
    public string? Domain { get; init; }
    public string Symbol { get; init; }

    public ImportAttribute(string? domain, string symbol)
    {
        Domain = domain;
        Symbol = symbol;
    }

    public ImportAttribute(string symbol)
    {
        Domain = null;
        Symbol = symbol;
    }
}
