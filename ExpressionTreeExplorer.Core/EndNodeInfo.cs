using System.Runtime.Serialization;

namespace ExpressionTreeExplorer.Core;

[DataContract]
public sealed class EndNodeInfo
{
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public string TypeDisplay { get; set; } = string.Empty;
    [DataMember] public string Value { get; set; } = string.Empty;
    [DataMember] public string Category { get; set; } = string.Empty;
    [DataMember] public string Path { get; set; } = string.Empty;
}
