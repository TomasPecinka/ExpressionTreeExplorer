using System.Collections.Generic;
using System.Linq;

namespace ExpressionTreeExplorer.Core;

public static class SourceTokenizer
{
    public static List<SourceToken> TokenizeForPath(string text, List<SourceSpan> spans, string highlightPath)
    {
        var segments = ComputeSegments(text, spans);
        return segments.Select(s => new SourceToken
        {
            Text = s.Text,
            IsHighlighted = s.Paths.Contains(highlightPath)
        }).ToList();
    }

    public static List<(string Text, HashSet<string> Paths)> ComputeSegments(string text, List<SourceSpan> spans)
    {
        if (string.IsNullOrEmpty(text) || spans.Count == 0)
        {
            return new List<(string, HashSet<string>)>
            {
                (text ?? string.Empty, new HashSet<string>())
            };
        }

        var boundaries = new SortedSet<int> { 0, text.Length };
        foreach (var span in spans)
        {
            var end = span.Start + span.Length;
            if (span.Start >= 0 && span.Start <= text.Length)
            {
                boundaries.Add(span.Start);
            }

            if (end >= 0 && end <= text.Length)
            {
                boundaries.Add(end);
            }
        }

        var sortedBoundaries = boundaries.ToList();
        var segments = new List<(string Text, HashSet<string> Paths)>();

        for (int i = 0; i < sortedBoundaries.Count - 1; i++)
        {
            var segStart = sortedBoundaries[i];
            var segEnd = sortedBoundaries[i + 1];
            if (segStart == segEnd)
            {
                continue;
            }

            var segText = text.Substring(segStart, segEnd - segStart);
            var paths = new HashSet<string>();

            foreach (var span in spans)
            {
                var spanEnd = span.Start + span.Length;
                if (span.Start <= segStart && segEnd <= spanEnd)
                {
                    paths.Add(span.Path);
                }
            }

            segments.Add((segText, paths));
        }

        return segments;
    }
}
