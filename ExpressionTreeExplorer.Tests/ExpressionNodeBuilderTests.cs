using System.Linq.Expressions;

using ExpressionTreeExplorer.Core;

namespace ExpressionTreeExplorer.Tests;

public class ExpressionNodeBuilderTests
{
    #region Unit tests

    [Fact]
    public void Build_Predicate_RootIsLambda()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20 && x.Age < 30;

        var payload = ExpressionNodeBuilder.Build(expr);
        var root = payload.Roots[0];

        Assert.Equal("Lambda", root.Kind);
        Assert.Equal("Lambda", root.NodeType);
        Assert.Single(root.Children);
    }

    [Fact]
    public void Build_Predicate_BinaryStructure()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20 && x.Age < 30;

        var payload = ExpressionNodeBuilder.Build(expr);
        var body = payload.Roots[0].Children[0];

        Assert.Equal("Binary", body.Kind);
        Assert.Equal("AndAlso", body.NodeType);
        Assert.Equal(2, body.Children.Count);
        Assert.Equal("GreaterThan", body.Children[0].Display);
        Assert.Equal("LessThan", body.Children[1].Display);
    }

    [Fact]
    public void Build_Constant_FormatsStringWithQuotes()
    {
        Expression<Func<User, bool>> expr = x => x.Name == "test";

        var payload = ExpressionNodeBuilder.Build(expr);
        var constant = FindNodeByKind(payload.Roots[0], "Constant");

        Assert.NotNull(constant);
        Assert.Equal("\"test\"", constant.Display);
    }

    [Fact]
    public void Build_Constant_ClosureSafeFallback()
    {
        var closureObj = new User { Name = "Alice", Age = 25 };
        Expression<Func<User, bool>> expr = x => x == closureObj;

        var payload = ExpressionNodeBuilder.Build(expr);
        var constant = FindNodeByKind(payload.Roots[0], "Constant");

        Assert.NotNull(constant);
        Assert.StartsWith("<", constant.Display);
        Assert.EndsWith(">", constant.Display);
    }

    [Fact]
    public void Build_Constant_FormatsIntWithInvariantCulture()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 42;

        var payload = ExpressionNodeBuilder.Build(expr);
        var constant = FindNodeByKind(payload.Roots[0], "Constant");

        Assert.NotNull(constant);
        Assert.Equal("42", constant.Display);
    }

    [Fact]
    public void Build_Parameter_SetsNameAndKind()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 0;

        var payload = ExpressionNodeBuilder.Build(expr);
        var param = FindNodeByKind(payload.Roots[0], "Parameter");

        Assert.NotNull(param);
        Assert.Equal("x", param.Display);
        Assert.Equal("Parameter", param.Kind);
        Assert.Equal("Parameter", param.NodeType);
    }

    [Fact]
    public void Build_Member_SetsNameAndDeclaringType()
    {
        Expression<Func<User, int>> expr = x => x.Age;

        var payload = ExpressionNodeBuilder.Build(expr);
        var member = FindNodeByKind(payload.Roots[0], "Member");

        Assert.NotNull(member);
        Assert.Equal("Age", member.Display);
        Assert.Equal("Member", member.Kind);
        Assert.Equal("MemberAccess", member.NodeType);
    }

    [Fact]
    public void Build_MethodCall_SetsMethodDetails()
    {
        Expression<Func<User, bool>> expr = x => x.Name.StartsWith('A');

        var payload = ExpressionNodeBuilder.Build(expr);
        var call = FindNodeByKind(payload.Roots[0], "MethodCall");

        Assert.NotNull(call);
        Assert.Equal("StartsWith", call.Display);
        Assert.Equal("MethodCall", call.Kind);
        Assert.Contains(call.Details, d => d.Name == "Method" && d.Value == "StartsWith");
        Assert.Contains(call.Details, d => d.Name == "DeclaringType" && d.Value.Contains("String"));
    }

    [Fact]
    public void Build_Unary_Convert()
    {
        Expression<Func<User, double>> expr = x => (double)x.Age;

        var payload = ExpressionNodeBuilder.Build(expr);
        var unary = FindNodeByKind(payload.Roots[0], "Unary");

        Assert.NotNull(unary);
        Assert.Equal("Unary", unary.Kind);
        Assert.Equal("Convert", unary.NodeType);
        Assert.StartsWith("Convert", unary.Display);
    }

    [Fact]
    public void Build_MemberInit_HasBindings()
    {
        var param = Expression.Parameter(typeof(User), "x");
        var newExpr = Expression.New(typeof(UserDto));
        var binding = Expression.Bind(
            typeof(UserDto).GetProperty(nameof(UserDto.FullName))!,
            Expression.Property(param, nameof(User.Name)));
        var memberInit = Expression.MemberInit(newExpr, binding);
        var lambda = Expression.Lambda<Func<User, UserDto>>(memberInit, param);

        var payload = ExpressionNodeBuilder.Build(lambda);
        var initNode = FindNodeByKind(payload.Roots[0], "MemberInit");

        Assert.NotNull(initNode);
        Assert.Equal("MemberInit", initNode.Kind);
        Assert.Contains(initNode.Children, c => c.Kind == "Binding" && c.Display == "FullName");
    }

    [Fact]
    public void Build_UnknownNode_FallsBackGracefully()
    {
        var param = Expression.Parameter(typeof(User), "x");
        var arrayExpr = Expression.NewArrayInit(typeof(int), Expression.Constant(1), Expression.Constant(2));
        var lambda = Expression.Lambda<Func<User, int[]>>(arrayExpr, param);

        var payload = ExpressionNodeBuilder.Build(lambda);
        var fallback = FindNodeByKind(payload.Roots[0], "Unsupported");

        Assert.NotNull(fallback);
        Assert.Equal("Unsupported", fallback.Kind);
    }

    [Fact]
    public void Build_Payload_SetsSummaryAndDebugText()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;

        var payload = ExpressionNodeBuilder.Build(expr);

        Assert.False(string.IsNullOrEmpty(payload.Summary));
        Assert.False(string.IsNullOrEmpty(payload.DebugText));
        Assert.False(string.IsNullOrEmpty(payload.ReadableText));
    }

    [Fact]
    public void Build_Nodes_HaveCorrectPaths()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;

        var payload = ExpressionNodeBuilder.Build(expr);
        var root = payload.Roots[0];

        Assert.Equal("0", root.Path);
        Assert.Equal("0/0", root.Children[0].Path);
        Assert.Equal("0/0/0", root.Children[0].Children[0].Path);
        Assert.Equal("0/0/1", root.Children[0].Children[1].Path);
    }

    #endregion

    #region Snapshot tests

    [Fact]
    public Task Build_Predicate_Snapshot()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20 && x.Age < 30;
        var payload = ExpressionNodeBuilder.Build(expr);
        return Verify(payload);
    }

    [Fact]
    public Task Build_MethodCall_Snapshot()
    {
        Expression<Func<User, bool>> expr = x => x.Name.StartsWith("A");
        var payload = ExpressionNodeBuilder.Build(expr);
        return Verify(payload);
    }

    [Fact]
    public Task Build_MemberInit_Snapshot()
    {
        var param = Expression.Parameter(typeof(User), "x");
        var newExpr = Expression.New(typeof(UserDto));
        var nameBinding = Expression.Bind(
            typeof(UserDto).GetProperty(nameof(UserDto.FullName))!,
            Expression.Property(param, nameof(User.Name)));
        var ageBinding = Expression.Bind(
            typeof(UserDto).GetProperty(nameof(UserDto.Age))!,
            Expression.Property(param, nameof(User.Age)));
        var memberInit = Expression.MemberInit(newExpr, nameBinding, ageBinding);
        var lambda = Expression.Lambda<Func<User, UserDto>>(memberInit, param);

        var payload = ExpressionNodeBuilder.Build(lambda);
        return Verify(payload);
    }

    [Fact]
    public Task Build_UnaryConvert_Snapshot()
    {
        Expression<Func<User, double>> expr = x => (double)x.Age;
        var payload = ExpressionNodeBuilder.Build(expr);
        return Verify(payload);
    }

    #endregion

    #region Helpers

    private static ExpressionNode? FindNodeByKind(ExpressionNode node, string kind)
    {
        if (node.Kind == kind)
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var found = FindNodeByKind(child, kind);
            if (found != null)
            {
                return found;
            }
        }
        return null;
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
