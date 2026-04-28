using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ExpressionTreeExplorer.Core;

[DataContract]
public class ExpressionNode
{
    [DataMember]
    public string Display { get; set; } = "";

    [DataMember]
    public string TypeDisplay { get; set; } = "";

    [DataMember]
    public List<ExpressionNode> Children { get; set; } = new();
}
