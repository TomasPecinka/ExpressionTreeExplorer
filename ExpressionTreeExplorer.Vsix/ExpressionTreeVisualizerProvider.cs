using System.Linq.Expressions;

using ExpressionTreeExplorer.Core;
using ExpressionTreeExplorer.ObjectSource;

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.DebuggerVisualizers;
using Microsoft.VisualStudio.RpcContracts.RemoteUI;

namespace ExpressionTreeExplorer.Vsix;

[VisualStudioContribution]
internal sealed class ExpressionTreeVisualizerProvider : DebuggerVisualizerProvider
{
    public override DebuggerVisualizerProviderConfiguration DebuggerVisualizerProviderConfiguration =>
        new(new VisualizerTargetType("%ExpressionTreeExplorer.Vsix.ExpressionTreeVisualizerProvider.DisplayName%", typeof(Expression<>)))
        {
            VisualizerObjectSourceType = new(typeof(ExpressionVisualizerObjectSource))
        };

    public override async Task<IRemoteUserControl> CreateVisualizerAsync(
        VisualizerTarget visualizerTarget,
        CancellationToken cancellationToken)
    {
        var payload = await visualizerTarget.ObjectSource
            .RequestDataAsync<ExpressionPayload>(null, cancellationToken);

        return new ExpressionTreeControl(
            new ExpressionTreeExplorerDataContext(payload));
    }
}