using System.IO;

using ExpressionTreeExplorer.Core;

using Microsoft.VisualStudio.DebuggerVisualizers;

namespace ExpressionTreeExplorer.ObjectSource;

public class ExpressionVisualizerObjectSource : VisualizerObjectSource
{
    public override void GetData(object target, Stream outgoingData)
    {
        var payload = new ExpressionPayload     // fake payload for testing the visualizer without the actual expression tree parsing logic
        {
            ReadableText = "FAKE EXPRESSION",

            Roots =
            [
                new ExpressionNode
                {
                    Display = "AndAlso",
                    TypeDisplay = "bool",
                    Children =
                    [
                        new ExpressionNode
                        {
                            Display = "GreaterThan",
                            TypeDisplay = "bool"
                        },
                        new ExpressionNode
                        {
                            Display = "LessThan",
                            TypeDisplay = "bool"
                        }
                    ]
                }
            ]
        };

        SerializeAsJson(outgoingData, payload);
    }
}
