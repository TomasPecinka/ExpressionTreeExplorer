using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ExpressionTreeExplorer.Core;

[DataContract]
public class ExpressionPayload
{
    [DataMember]
    public List<ExpressionNode> Roots { get; set; } = new();

    [DataMember]
    public string ReadableText { get; set; } = "";
}
