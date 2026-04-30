using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ExpressionTreeExplorer.Core;

[DataContract]
public sealed class ExpressionNode
{
    [DataMember] public string Path { get; set; } = string.Empty;
    [DataMember] public string Kind { get; set; } = string.Empty;
    [DataMember] public string NodeType { get; set; } = string.Empty;
    [DataMember] public string Display { get; set; } = string.Empty;
    [DataMember] public string TypeDisplay { get; set; } = string.Empty;
    [DataMember] public List<NodeDetail> Details { get; set; } = new();
    [DataMember] public List<ExpressionNode> Children { get; set; } = new();
}
