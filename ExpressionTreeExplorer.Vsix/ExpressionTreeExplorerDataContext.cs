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
        EndNodes = payload.EndNodes;

        ParameterEndNodes = payload.EndNodes.Where(e => e.Category == "Parameter").ToList();
        ConstantEndNodes = payload.EndNodes.Where(e => e.Category == "Constant").ToList();
        ClosedOverEndNodes = payload.EndNodes.Where(e => e.Category == "ClosedOver").ToList();
        DefaultEndNodes = payload.EndNodes.Where(e => e.Category == "Default").ToList();

        HasParameterEndNodes = ParameterEndNodes.Count > 0;
        HasConstantEndNodes = ConstantEndNodes.Count > 0;
        HasClosedOverEndNodes = ClosedOverEndNodes.Count > 0;
        HasDefaultEndNodes = DefaultEndNodes.Count > 0;

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
    [DataMember] public List<EndNodeInfo> EndNodes { get; }

    [DataMember] public List<EndNodeInfo> ParameterEndNodes { get; }
    [DataMember] public List<EndNodeInfo> ConstantEndNodes { get; }
    [DataMember] public List<EndNodeInfo> ClosedOverEndNodes { get; }
    [DataMember] public List<EndNodeInfo> DefaultEndNodes { get; }

    [DataMember] public bool HasParameterEndNodes { get; }
    [DataMember] public bool HasConstantEndNodes { get; }
    [DataMember] public bool HasClosedOverEndNodes { get; }
    [DataMember] public bool HasDefaultEndNodes { get; }
}