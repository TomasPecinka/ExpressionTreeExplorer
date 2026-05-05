using Microsoft.VisualStudio.Extensibility.UI;

namespace ExpressionTreeExplorer.Vsix;

internal class ExpressionTreeControl : RemoteUserControl
{
    public ExpressionTreeControl(ExpressionTreeExplorerDataContext dataContext) : base(dataContext)
    {
    }
}
