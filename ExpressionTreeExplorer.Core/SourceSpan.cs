using System.Runtime.Serialization;

namespace ExpressionTreeExplorer.Core;

[DataContract]
public sealed class SourceSpan
{
    [DataMember] public string Path { get; set; } = string.Empty;
    [DataMember] public int Start { get; set; }
    [DataMember] public int Length { get; set; }
}
