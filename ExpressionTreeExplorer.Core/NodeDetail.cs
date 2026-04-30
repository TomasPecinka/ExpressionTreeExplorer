using System.Runtime.Serialization;

namespace ExpressionTreeExplorer.Core;

[DataContract]
public sealed class NodeDetail
{
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public string Value { get; set; } = string.Empty;
}
