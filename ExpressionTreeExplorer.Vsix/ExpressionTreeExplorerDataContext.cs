using System.Runtime.Serialization;

using ExpressionTreeExplorer.Core;

namespace ExpressionTreeExplorer.Vsix;

[DataContract]
internal sealed class ExpressionTreeExplorerDataContext
{
    public ExpressionTreeExplorerDataContext(ExpressionPayload payload)
    {
        Roots = payload.Roots;
        ReadableText = payload.ReadableText;
        DebugText = payload.DebugText;
        Summary = payload.Summary;
    }

    [DataMember] public List<ExpressionNode> Roots { get; }
    [DataMember] public string ReadableText { get; }
    [DataMember] public string DebugText { get; }
    [DataMember] public string Summary { get; }
}