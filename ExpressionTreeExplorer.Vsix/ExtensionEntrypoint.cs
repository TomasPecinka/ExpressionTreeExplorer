using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;

namespace ExpressionTreeExplorer.Vsix;

[VisualStudioContribution]
internal class ExtensionEntrypoint : Extension
{
    public override ExtensionConfiguration ExtensionConfiguration => new()
    {
        Metadata = new(
                id: "ExpressionTreeExplorer.Vsix.dba879fe-b40c-43a4-8831-9a5ab6abc011",
                version: this.ExtensionAssemblyVersion,
                publisherName: "TomasPecinka",
                displayName: "Expression Tree Explorer",
                description: "A debugger visualizer for exploring expression trees in Visual Studio.")
        {
            Icon = "Assets/icon.png",
            PreviewImage = "Assets/preview.png",
            License = "LICENSE.txt",
            Tags = ["expression", "expression-tree", "linq", "debugger", "visualizer", "ef-core"],
            MoreInfo = "https://github.com/TomasPecinka/ExpressionTreeExplorer",
            Preview = true,
        },
    };

    protected override void InitializeServices(IServiceCollection serviceCollection)
    {
        base.InitializeServices(serviceCollection);
    }
}
