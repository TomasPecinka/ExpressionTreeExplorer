using ExpressionTreeExplorer.Core;

using Microsoft.VisualStudio.Extensibility.UI;

namespace ExpressionTreeExplorer.Vsix;

internal class ExpressionTreeControl : RemoteUserControl
{
    public ExpressionTreeControl(ExpressionPayload dataContext) : base(dataContext)
    {
    }
}
