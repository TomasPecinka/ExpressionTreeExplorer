using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ExpressionTreeExplorer.Core;

[DataContract]
public sealed class ExpressionPayload
{
    [DataMember] public List<ExpressionNode> Roots { get; set; } = new();
    [DataMember] public string ReadableText { get; set; } = string.Empty;
    [DataMember] public string DebugText { get; set; } = string.Empty;
    [DataMember] public string Summary { get; set; } = string.Empty;
}