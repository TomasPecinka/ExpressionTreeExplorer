using System.Runtime.Serialization;

using ExpressionTreeExplorer.Core;

using Microsoft.VisualStudio.Extensibility.UI;

namespace ExpressionTreeExplorer.Vsix;

[DataContract]
internal sealed class ExpressionTreeExplorerDataContext : NotifyPropertyChangedObject
{
    public ExpressionTreeExplorerDataContext(ExpressionPayload payload)
    {
        Roots = payload.Roots;
        ReadableText = payload.ReadableText;
        DebugText = payload.DebugText;
        Summary = payload.Summary;

        ExpandAllCommand = new AsyncCommand((_, ct) =>
        {
            foreach (var root in Roots)
            {
                root.SetAllExpanded(true);
            }

            return Task.CompletedTask;
        });

        CollapseAllCommand = new AsyncCommand((_, ct) =>
        {
            foreach (var root in Roots)
            {
                root.SetAllExpanded(false);
            }

            return Task.CompletedTask;
        });
    }

    [DataMember] public List<ExpressionNode> Roots { get; }
    [DataMember] public string ReadableText { get; }
    [DataMember] public string DebugText { get; }
    [DataMember] public string Summary { get; }
    [DataMember] public IAsyncCommand ExpandAllCommand { get; }
    [DataMember] public IAsyncCommand CollapseAllCommand { get; }
}