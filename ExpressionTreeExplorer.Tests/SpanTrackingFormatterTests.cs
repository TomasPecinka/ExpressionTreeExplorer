using System.Linq.Expressions;

using ExpressionTreeExplorer.Core;

namespace ExpressionTreeExplorer.Tests;

public class SpanTrackingFormatterTests
{
    #region Formatting tests

    [Fact]
    public void Format_SimplePredicate_GeneratesReadableCode()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;
        var result = SpanTrackingFormatter.Format(expr);
        Assert.Equal("x => x.Age > 20", result.Text);
    }

    [Fact]
    public void Format_CompoundPredicate_GeneratesReadableCode()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20 && x.Age < 30;
        var result = SpanTrackingFormatter.Format(expr);
        Assert.Equal("x => (x.Age > 20) && (x.Age < 30)", result.Text);
    }

    [Fact]
    public void Format_MemberAccess_GeneratesReadableCode()
    {
        Expression<Func<User, string>> expr = x => x.Name;
        var result = SpanTrackingFormatter.Format(expr);
        Assert.Equal("x => x.Name", result.Text);
    }

    [Fact]
    public void Format_MethodCall_GeneratesReadableCode()
    {
        Expression<Func<User, string>> expr = x => x.Name.ToUpper();
        var result = SpanTrackingFormatter.Format(expr);
        Assert.Equal("x => x.Name.ToUpper()", result.Text);
    }

    [Fact]
    public void Format_MemberInit_GeneratesReadableCode()
    {
        Expression<Func<User, UserDto>> expr = x => new UserDto { FullName = x.Name, Age = x.Age };
        var result = SpanTrackingFormatter.Format(expr);
        Assert.Equal("x => new UserDto()\n{\n    FullName = x.Name,\n    Age = x.Age\n}", result.Text);
    }

    [Fact]
    public void Format_Constant_GeneratesReadableCode()
    {
        Expression<Func<int>> expr = () => 42;
        var result = SpanTrackingFormatter.Format(expr);
        Assert.Equal("() => 42", result.Text);
    }

    [Fact]
    public void Format_StringConstant_GeneratesReadableCode()
    {
        Expression<Func<string>> expr = () => "hello";
        var result = SpanTrackingFormatter.Format(expr);
        Assert.Equal("() => \"hello\"", result.Text);
    }

    [Fact]
    public void Format_UnaryConvert_GeneratesReadableCode()
    {
        Expression<Func<User, double>> expr = x => x.Age;
        var result = SpanTrackingFormatter.Format(expr);
        Assert.Equal("x => (double)x.Age", result.Text);
    }

    [Fact]
    public void Format_UnaryNot_GeneratesReadableCode()
    {
        Expression<Func<bool, bool>> expr = x => !x;
        var result = SpanTrackingFormatter.Format(expr);
        Assert.Equal("x => !x", result.Text);
    }

    [Fact]
    public void Format_Conditional_GeneratesReadableCode()
    {
        Expression<Func<int, string>> expr = x => x > 0 ? "positive" : "non-positive";
        var result = SpanTrackingFormatter.Format(expr);
        Assert.Equal("x => x > 0 ? \"positive\" : \"non-positive\"", result.Text);
    }

    [Fact]
    public void Format_NewExpression_GeneratesReadableCode()
    {
        Expression<Func<List<int>>> expr = () => new List<int>();
        var result = SpanTrackingFormatter.Format(expr);
        Assert.Equal("() => new List<int>()", result.Text);
    }

    [Fact]
    public void Format_ClosureVariable_GeneratesReadableCode()
    {
        int threshold = 5;
        Expression<Func<int, bool>> expr = x => x > threshold;
        var result = SpanTrackingFormatter.Format(expr);
        Assert.Equal("x => x > threshold", result.Text);
    }

    [Fact]
    public void Format_MultipleParameters_GeneratesReadableCode()
    {
        Expression<Func<int, int, int>> expr = (a, b) => a + b;
        var result = SpanTrackingFormatter.Format(expr);
        Assert.Equal("(a, b) => a + b", result.Text);
    }

    [Fact]
    public void Format_Default_GeneratesReadableCode()
    {
        var param = Expression.Parameter(typeof(User), "x");
        var defaultExpr = Expression.Default(typeof(int));
        var lambda = Expression.Lambda<Func<User, int>>(defaultExpr, param);
        var result = SpanTrackingFormatter.Format(lambda);
        Assert.Equal("x => default(int)", result.Text);
    }

    #endregion

    #region Span position tests

    [Fact]
    public void Format_SimplePredicate_RootSpanCoversEntireText()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;
        var result = SpanTrackingFormatter.Format(expr);

        var rootSpan = result.Spans.First(s => s.Path == "0");
        Assert.Equal(0, rootSpan.Start);
        Assert.Equal(result.Text.Length, rootSpan.Length);
    }

    [Fact]
    public void Format_SimplePredicate_BinarySpanPointsToCorrectText()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;
        var result = SpanTrackingFormatter.Format(expr);

        var binarySpan = result.Spans.First(s => s.Path == "0/0");
        var binaryText = result.Text.Substring(binarySpan.Start, binarySpan.Length);
        Assert.Equal("x.Age > 20", binaryText);
    }

    [Fact]
    public void Format_SimplePredicate_ConstantSpanPointsToCorrectText()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;
        var result = SpanTrackingFormatter.Format(expr);

        var constantSpan = result.Spans.First(s => s.Path == "0/0/1");
        var constantText = result.Text.Substring(constantSpan.Start, constantSpan.Length);
        Assert.Equal("20", constantText);
    }

    [Fact]
    public void Format_SimplePredicate_MemberSpanPointsToCorrectText()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;
        var result = SpanTrackingFormatter.Format(expr);

        var memberSpan = result.Spans.First(s => s.Path == "0/0/0");
        var memberText = result.Text.Substring(memberSpan.Start, memberSpan.Length);
        Assert.Equal("x.Age", memberText);
    }

    [Fact]
    public void Format_MemberInit_BindingSpansPointToCorrectText()
    {
        Expression<Func<User, UserDto>> expr = x => new UserDto { FullName = x.Name, Age = x.Age };
        var result = SpanTrackingFormatter.Format(expr);

        var binding1 = result.Spans.First(s => s.Path == "0/0/1");
        var binding1Text = result.Text.Substring(binding1.Start, binding1.Length);
        Assert.Equal("FullName = x.Name", binding1Text);

        var binding2 = result.Spans.First(s => s.Path == "0/0/2");
        var binding2Text = result.Text.Substring(binding2.Start, binding2.Length);
        Assert.Equal("Age = x.Age", binding2Text);
    }

    #endregion

    #region Path consistency tests

    [Fact]
    public void Format_SimplePredicate_PathsMatchBuilder()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;
        var payload = ExpressionNodeBuilder.Build(expr);
        var result = SpanTrackingFormatter.Format(expr);

        var builderPaths = CollectAllPaths(payload.Roots[0]).ToHashSet();
        var spanPaths = result.Spans.Select(s => s.Path).ToHashSet();

        Assert.Subset(builderPaths, spanPaths);
    }

    [Fact]
    public void Format_CompoundPredicate_PathsMatchBuilder()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20 && x.Age < 30;
        var payload = ExpressionNodeBuilder.Build(expr);
        var result = SpanTrackingFormatter.Format(expr);

        var builderPaths = CollectAllPaths(payload.Roots[0]).ToHashSet();
        var spanPaths = result.Spans.Select(s => s.Path).ToHashSet();

        Assert.Subset(builderPaths, spanPaths);
    }

    [Fact]
    public void Format_MemberInit_PathsMatchBuilder()
    {
        Expression<Func<User, UserDto>> expr = x => new UserDto { FullName = x.Name, Age = x.Age };
        var payload = ExpressionNodeBuilder.Build(expr);
        var result = SpanTrackingFormatter.Format(expr);

        var builderPaths = CollectAllPaths(payload.Roots[0]).ToHashSet();
        var spanPaths = result.Spans.Select(s => s.Path).ToHashSet();

        Assert.Subset(builderPaths, spanPaths);
    }

    [Fact]
    public void Format_Closure_PathsMatchBuilder()
    {
        int threshold = 5;
        Expression<Func<int, bool>> expr = x => x > threshold;
        var payload = ExpressionNodeBuilder.Build(expr);
        var result = SpanTrackingFormatter.Format(expr);

        var builderPaths = CollectAllPaths(payload.Roots[0]).ToHashSet();
        var spanPaths = result.Spans.Select(s => s.Path).ToHashSet();

        Assert.Subset(builderPaths, spanPaths);
    }

    #endregion

    #region Snapshot tests

    [Fact]
    public Task Format_Predicate_Snapshot()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20 && x.Age < 30;
        var result = SpanTrackingFormatter.Format(expr);
        return Verify(new { result.Text, result.Spans });
    }

    [Fact]
    public Task Format_MethodCall_Snapshot()
    {
        Expression<Func<User, string>> expr = x => x.Name.ToUpper();
        var result = SpanTrackingFormatter.Format(expr);
        return Verify(new { result.Text, result.Spans });
    }

    #endregion

    #region Helpers

    private static IEnumerable<string> CollectAllPaths(ExpressionNode node)
    {
        yield return node.Path;
        foreach (var child in node.Children)
        {
            foreach (var p in CollectAllPaths(child))
            {
                yield return p;
            }
        }
    }

    public class User
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    public class UserDto
    {
        public string FullName { get; set; } = "";
        public int Age { get; set; }
    }

    #endregion
}
