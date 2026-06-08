using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace ExpressionTreeExplorer.Core;

[DataContract]
public sealed class ExpressionNode : INotifyPropertyChanged
{
    private bool _isExpanded = true;

    [DataMember] public string Path { get; set; } = string.Empty;
    [DataMember] public string RelationToParent { get; set; } = string.Empty;
    [DataMember] public string Kind { get; set; } = string.Empty;
    [DataMember] public string NodeType { get; set; } = string.Empty;
    [DataMember] public string Display { get; set; } = string.Empty;
    [DataMember] public string TypeDisplay { get; set; } = string.Empty;
    [DataMember] public List<NodeDetail> Details { get; set; } = new();
    [DataMember] public List<ExpressionNode> Children { get; set; } = new();
    [DataMember] public List<SourceLine> SourceLines { get; set; } = new();
    [DataMember] public string WatchExpression { get; set; } = string.Empty;

    [DataMember]
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
            {
                return;
            }

            _isExpanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
        }
    }

    public void SetAllExpanded(bool expanded)
    {
        IsExpanded = expanded;
        foreach (var child in Children)
        {
            child.SetAllExpanded(expanded);
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}
