using System.Linq.Expressions;

using ExpressionTreeExplorer.Core;

namespace ExpressionTreeExplorer.Tests;

public class SourceTokenizerTests
{
    [Fact]
    public void Tokenize_SimplePredicate_SplitsAtBoundaries()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;
        var spanResult = SpanTrackingFormatter.Format(expr);
        var segments = SourceTokenizer.ComputeSegments(spanResult.Text, new List<SourceSpan>(spanResult.Spans));

        var joined = string.Concat(segments.Select(s => s.Text));
        Assert.Equal(spanResult.Text, joined);
        Assert.True(segments.Count >= 3);
    }

    [Fact]
    public void Tokenize_SimplePredicate_AssignsCorrectPaths()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;
        var spanResult = SpanTrackingFormatter.Format(expr);
        var segments = SourceTokenizer.ComputeSegments(spanResult.Text, new List<SourceSpan>(spanResult.Spans));

        var memberSegments = segments.Where(s => s.Paths.Contains("0/0/0")).ToList();
        Assert.NotEmpty(memberSegments);
        var memberText = string.Concat(memberSegments.Select(s => s.Text));
        Assert.Equal("x.Age", memberText);
        Assert.All(memberSegments, s => Assert.Contains("0/0", s.Paths));
        Assert.All(memberSegments, s => Assert.Contains("0", s.Paths));
    }

    [Fact]
    public void Tokenize_HighlightLeafNode_OnlyLeafHighlighted()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;
        var spanResult = SpanTrackingFormatter.Format(expr);
        var tokens = SourceTokenizer.TokenizeForPath(spanResult.Text, new List<SourceSpan>(spanResult.Spans), "0/0/0");

        var highlighted = tokens.Where(t => t.IsHighlighted).ToList();
        var highlightedText = string.Concat(highlighted.Select(t => t.Text));
        Assert.Equal("x.Age", highlightedText);
    }

    [Fact]
    public void Tokenize_HighlightParentNode_AllChildrenHighlighted()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;
        var spanResult = SpanTrackingFormatter.Format(expr);
        var tokens = SourceTokenizer.TokenizeForPath(spanResult.Text, new List<SourceSpan>(spanResult.Spans), "0/0");

        var highlighted = tokens.Where(t => t.IsHighlighted).ToList();
        var highlightedText = string.Concat(highlighted.Select(t => t.Text));
        Assert.Equal("x.Age > 20", highlightedText);
    }

    [Fact]
    public void Tokenize_HighlightRoot_AllHighlighted()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;
        var spanResult = SpanTrackingFormatter.Format(expr);
        var tokens = SourceTokenizer.TokenizeForPath(spanResult.Text, new List<SourceSpan>(spanResult.Spans), "0");

        Assert.All(tokens, t => Assert.True(t.IsHighlighted));
    }

    [Fact]
    public void Tokenize_EmptyText_ReturnsSingleToken()
    {
        var segments = SourceTokenizer.ComputeSegments("", new List<SourceSpan>());
        Assert.Single(segments);
        Assert.Equal("", segments[0].Text);
    }

    [Fact]
    public void Tokenize_NoSpans_ReturnsSingleUnhighlightedToken()
    {
        var tokens = SourceTokenizer.TokenizeForPath("hello world", new List<SourceSpan>(), "0");
        Assert.Single(tokens);
        Assert.Equal("hello world", tokens[0].Text);
        Assert.False(tokens[0].IsHighlighted);
    }

    [Fact]
    public void Tokenize_NoMatchingPath_NothingHighlighted()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;
        var spanResult = SpanTrackingFormatter.Format(expr);
        var tokens = SourceTokenizer.TokenizeForPath(spanResult.Text, new List<SourceSpan>(spanResult.Spans), "9/9/9");

        Assert.All(tokens, t => Assert.False(t.IsHighlighted));
    }

    [Fact]
    public void Tokenize_BuilderAssignsTokensToAllNodes()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;
        var payload = ExpressionNodeBuilder.Build(expr);

        var allNodes = CollectAllNodes(payload.Roots[0]);
        Assert.All(allNodes, n => Assert.NotEmpty(n.SourceTokens));
    }

    [Fact]
    public void Tokenize_BuilderTokensConcatenateToReadableText()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;
        var payload = ExpressionNodeBuilder.Build(expr);

        foreach (var node in CollectAllNodes(payload.Roots[0]))
        {
            var joined = string.Concat(node.SourceTokens.Select(t => t.Text));
            Assert.Equal(payload.ReadableText, joined);
        }
    }

    private static IEnumerable<ExpressionNode> CollectAllNodes(ExpressionNode node)
    {
        yield return node;
        foreach (var child in node.Children)
        {
            foreach (var n in CollectAllNodes(child))
            {
                yield return n;
            }
        }
    }

    public class User
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }
}
