using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ExpressionTreeExplorer.Core;

[DataContract]
public sealed class SourceLine
{
    [DataMember] public List<SourceToken> Tokens { get; set; } = new();
}
