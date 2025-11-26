using System.Text;
using Tq.Realizer.Core.Builder.References;

namespace Tq.Realizer.Core.Intermediate.Values;

public class SliceConstantValue(TypeReference elemtype, RealizerConstantValue[] content) : RealizerConstantValue
{
    public readonly TypeReference ElementType = elemtype;
    public readonly RealizerConstantValue[] Content = content;

    public override string ToString()
    {
        var sb = new StringBuilder();

        if (ElementType is IntegerTypeReference { Bits: 8 })
        {
            sb.Append('"');
            foreach (var e in Content)
            {
                var c = (char)((IntegerConstantValue)e).Value;
                if (char.IsAscii(c) && !char.IsControl(c)) sb.Append(c);
                else sb.Append($"\\{(byte)c:x2}");
            };
            sb.Append('"');
        }
        else if (ElementType is IntegerTypeReference)
        {
            sb.Append('"'
                      + string.Join(", ", Content.Select(e => ((IntegerConstantValue)e).Value))
                      + '"');
        }
        
        return sb.ToString();
    }
    public override int GetHashCode() => Content.Aggregate(0, HashCode.Combine);

}
