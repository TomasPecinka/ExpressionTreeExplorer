using System.Runtime.Serialization;

namespace ExpressionTreeExplorer.Core;

[DataContract]
public sealed class SourceToken
{
    [DataMember] public string Text { get; set; } = string.Empty;
    [DataMember] public bool IsHighlighted { get; set; }
}
