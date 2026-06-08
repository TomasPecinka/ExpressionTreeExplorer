using System.Linq.Expressions;

using ExpressionTreeExplorer.Core;

namespace ExpressionTreeExplorer.Tests;

public class WatchExpressionGeneratorTests
{
    [Fact]
    public void Generate_RootNode_ReturnsEmpty()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;
        var payload = ExpressionNodeBuilder.Build(expr);
        var root = payload.Roots[0];

        var result = WatchExpressionGenerator.Generate(root, root.Path);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Generate_LambdaBody_ReturnsBody()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;
        var payload = ExpressionNodeBuilder.Build(expr);
        var root = payload.Roots[0];
        var body = root.Children[0];

        var result = WatchExpressionGenerator.Generate(root, body.Path);

        Assert.Equal(".Body", result);
    }

    [Fact]
    public void Generate_BinaryLeftRight_ReturnsCorrectPath()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20 && x.Age < 30;
        var payload = ExpressionNodeBuilder.Build(expr);
        var root = payload.Roots[0];
        var body = root.Children[0];
        var left = body.Children[0];
        var right = body.Children[1];

        Assert.Equal(".Body.Left", WatchExpressionGenerator.Generate(root, left.Path));
        Assert.Equal(".Body.Right", WatchExpressionGenerator.Generate(root, right.Path));
    }

    [Fact]
    public void Generate_DeepPath_ChainsAllRelations()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20 && x.Age < 30;
        var payload = ExpressionNodeBuilder.Build(expr);
        var root = payload.Roots[0];
        // Body.Left.Left = the member access x.Age on the left side of >
        var leftLeft = root.Children[0].Children[0].Children[0];

        var result = WatchExpressionGenerator.Generate(root, leftLeft.Path);

        Assert.Equal(".Body.Left.Left", result);
    }

    [Fact]
    public void Generate_UnaryOperand_ReturnsOperand()
    {
        Expression<Func<User, double>> expr = x => (double)x.Age;
        var payload = ExpressionNodeBuilder.Build(expr);
        var root = payload.Roots[0];
        var convert = root.Children[0]; // Convert unary
        var operand = convert.Children[0]; // x.Age member

        Assert.Equal(".Body.Operand", WatchExpressionGenerator.Generate(root, operand.Path));
    }

    [Fact]
    public void Generate_MemberExpression_ReturnsExpression()
    {
        Expression<Func<User, int>> expr = x => x.Age;
        var payload = ExpressionNodeBuilder.Build(expr);
        var root = payload.Roots[0];
        var member = root.Children[0]; // x.Age
        var param = member.Children[0]; // x

        Assert.Equal(".Body.Expression", WatchExpressionGenerator.Generate(root, param.Path));
    }

    [Fact]
    public void Generate_ConditionalBranches_ReturnsCorrectRelations()
    {
        var param = Expression.Parameter(typeof(int), "x");
        var cond = Expression.Condition(
            Expression.GreaterThan(param, Expression.Constant(0)),
            Expression.Constant(1),
            Expression.Constant(-1));
        var lambda = Expression.Lambda<Func<int, int>>(cond, param);

        var payload = ExpressionNodeBuilder.Build(lambda);
        var root = payload.Roots[0];
        var conditional = root.Children[0];

        Assert.Equal(".Body.Test", WatchExpressionGenerator.Generate(root, conditional.Children[0].Path));
        Assert.Equal(".Body.IfTrue", WatchExpressionGenerator.Generate(root, conditional.Children[1].Path));
        Assert.Equal(".Body.IfFalse", WatchExpressionGenerator.Generate(root, conditional.Children[2].Path));
    }

    [Fact]
    public void Generate_MethodCallArguments_ReturnsIndexedArguments()
    {
        Expression<Func<string, bool>> expr = s => s.StartsWith("a");
        var payload = ExpressionNodeBuilder.Build(expr);
        var root = payload.Roots[0];
        var methodCall = root.Children[0];

        Assert.Equal(".Body.Object", WatchExpressionGenerator.Generate(root, methodCall.Children[0].Path));
        Assert.Equal(".Body.Arguments[0]", WatchExpressionGenerator.Generate(root, methodCall.Children[1].Path));
    }

    [Fact]
    public void Generate_InvalidPath_ReturnsEmpty()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;
        var payload = ExpressionNodeBuilder.Build(expr);
        var root = payload.Roots[0];

        var result = WatchExpressionGenerator.Generate(root, "999/999");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RelationToParent_RootNode_IsEmpty()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;
        var payload = ExpressionNodeBuilder.Build(expr);

        Assert.Equal(string.Empty, payload.Roots[0].RelationToParent);
    }

    [Fact]
    public void RelationToParent_LambdaBody_IsBody()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;
        var payload = ExpressionNodeBuilder.Build(expr);
        var body = payload.Roots[0].Children[0];

        Assert.Equal("Body", body.RelationToParent);
    }

    [Fact]
    public void RelationToParent_BinaryChildren_AreLeftAndRight()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;
        var payload = ExpressionNodeBuilder.Build(expr);
        var binary = payload.Roots[0].Children[0];

        Assert.Equal("Left", binary.Children[0].RelationToParent);
        Assert.Equal("Right", binary.Children[1].RelationToParent);
    }

    public class User
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }
}
