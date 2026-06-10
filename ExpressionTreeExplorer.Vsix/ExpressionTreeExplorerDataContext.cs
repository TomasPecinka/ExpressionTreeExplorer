using System.Diagnostics;
using System.Runtime.Serialization;

using ExpressionTreeExplorer.Core;

using Microsoft.VisualStudio.Extensibility.UI;

namespace ExpressionTreeExplorer.Vsix;

[DataContract]
internal sealed class ExpressionTreeExplorerDataContext : NotifyPropertyChangedObject
{
    private int _selectedFormatIndex;
    private string _currentPlainText = string.Empty;
    private bool _isReadableFormat = true;

    public ExpressionTreeExplorerDataContext(ExpressionPayload payload)
    {
        Roots = payload.Roots;
        ReadableText = payload.ReadableText;
        DebugText = payload.DebugText;
        DebugViewText = payload.DebugViewText;
        Summary = payload.Summary;
        EndNodes = payload.EndNodes;
        SourceSpans = payload.SourceSpans;
        FormatOptions = ["Readable", "ToString", "DebugView"];

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

        CopyWatchCommand = new AsyncCommand(async (parameter, ct) =>
        {
            if (parameter is string text && !string.IsNullOrEmpty(text))
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "clip.exe",
                        RedirectStandardInput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    },
                };
                process.Start();
                await process.StandardInput.WriteAsync(text);
                process.StandardInput.Close();
                await process.WaitForExitAsync(ct);
            }
        });

        OpenDocsCommand = new AsyncCommand((parameter, ct) =>
        {
            if (parameter is string url && !string.IsNullOrEmpty(url))
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }

            return Task.CompletedTask;
        });
    }

    [DataMember] public List<ExpressionNode> Roots { get; }
    [DataMember] public string ReadableText { get; }
    [DataMember] public string DebugText { get; }
    [DataMember] public string DebugViewText { get; }
    [DataMember] public string Summary { get; }
    [DataMember] public IAsyncCommand ExpandAllCommand { get; }
    [DataMember] public IAsyncCommand CollapseAllCommand { get; }
    [DataMember] public IAsyncCommand CopyWatchCommand { get; }
    [DataMember] public IAsyncCommand OpenDocsCommand { get; }
    [DataMember] public List<EndNodeInfo> EndNodes { get; }
    [DataMember] public List<SourceSpan> SourceSpans { get; }
    [DataMember] public List<string> FormatOptions { get; }

    [DataMember]
    public int SelectedFormatIndex
    {
        get => _selectedFormatIndex;
        set
        {
            if (_selectedFormatIndex == value)
            {
                return;
            }

            _selectedFormatIndex = value;

            IsReadableFormat = value == 0;
            CurrentPlainText = value switch
            {
                1 => DebugText,
                2 => DebugViewText,
                _ => string.Empty
            };
        }
    }

    [DataMember]
    public bool IsReadableFormat
    {
        get => _isReadableFormat;
        private set => SetProperty(ref _isReadableFormat, value);
    }

    [DataMember]
    public string CurrentPlainText
    {
        get => _currentPlainText;
        private set => SetProperty(ref _currentPlainText, value);
    }

    [DataMember] public List<EndNodeInfo> ParameterEndNodes { get; }
    [DataMember] public List<EndNodeInfo> ConstantEndNodes { get; }
    [DataMember] public List<EndNodeInfo> ClosedOverEndNodes { get; }
    [DataMember] public List<EndNodeInfo> DefaultEndNodes { get; }

    [DataMember] public bool HasParameterEndNodes { get; }
    [DataMember] public bool HasConstantEndNodes { get; }
    [DataMember] public bool HasClosedOverEndNodes { get; }
    [DataMember] public bool HasDefaultEndNodes { get; }
}