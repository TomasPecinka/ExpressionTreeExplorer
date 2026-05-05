using System;
using System.IO;
using System.Linq.Expressions;

using ExpressionTreeExplorer.Core;

using Microsoft.VisualStudio.DebuggerVisualizers;

namespace ExpressionTreeExplorer.ObjectSource;

public class ExpressionVisualizerObjectSource : VisualizerObjectSource
{
    public override void GetData(object target, Stream outgoingData)
    {
        if (target is not Expression expression)
        {
            throw new InvalidOperationException("Target is not an Expression.");
        }

        var payload = ExpressionNodeBuilder.Build(expression);

        SerializeAsJson(outgoingData, payload);
    }
}
