using System.Linq.Expressions;
using System.Runtime.CompilerServices;

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
        var runtimeVars = Expression.RuntimeVariables(param);
        var lambda = Expression.Lambda<Func<User, IRuntimeVariables>>(runtimeVars, param);

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

    [Fact]
    public void Build_Conditional_HasThreeChildren()
    {
        var param = Expression.Parameter(typeof(User), "x");
        var test = Expression.GreaterThan(Expression.Property(param, nameof(User.Age)), Expression.Constant(0));
        var ifTrue = Expression.Property(param, nameof(User.Name));
        var ifFalse = Expression.Constant("default");
        var cond = Expression.Condition(test, ifTrue, ifFalse);
        var lambda = Expression.Lambda<Func<User, string>>(cond, param);

        var payload = ExpressionNodeBuilder.Build(lambda);
        var node = FindNodeByKind(payload.Roots[0], "Conditional");

        Assert.NotNull(node);
        Assert.Equal("Conditional", node.Kind);
        Assert.Equal(3, node.Children.Count);
    }

    [Fact]
    public void Build_New_SetsTypeName()
    {
        var param = Expression.Parameter(typeof(User), "x");
        var newExpr = Expression.New(typeof(UserDto));
        var lambda = Expression.Lambda<Func<User, UserDto>>(newExpr, param);

        var payload = ExpressionNodeBuilder.Build(lambda);
        var node = FindNodeByKind(payload.Roots[0], "New");

        Assert.NotNull(node);
        Assert.Equal("New", node.Kind);
        Assert.Contains("UserDto", node.Display);
    }

    [Fact]
    public void Build_TypeBinary_SetsTypeOperand()
    {
        var param = Expression.Parameter(typeof(object), "x");
        var typeIs = Expression.TypeIs(param, typeof(User));
        var lambda = Expression.Lambda<Func<object, bool>>(typeIs, param);

        var payload = ExpressionNodeBuilder.Build(lambda);
        var node = FindNodeByKind(payload.Roots[0], "TypeBinary");

        Assert.NotNull(node);
        Assert.Equal("TypeBinary", node.Kind);
        Assert.Contains("User", node.Display);
        Assert.Single(node.Children);
    }

    [Fact]
    public void Build_NewArrayInit_HasElements()
    {
        var param = Expression.Parameter(typeof(User), "x");
        var arrayExpr = Expression.NewArrayInit(typeof(int), Expression.Constant(1), Expression.Constant(2));
        var lambda = Expression.Lambda<Func<User, int[]>>(arrayExpr, param);

        var payload = ExpressionNodeBuilder.Build(lambda);
        var node = FindNodeByKind(payload.Roots[0], "NewArray");

        Assert.NotNull(node);
        Assert.Equal("NewArray", node.Kind);
        Assert.Equal(2, node.Children.Count);
    }

    [Fact]
    public void Build_Default_SetsTypeName()
    {
        var param = Expression.Parameter(typeof(User), "x");
        var defaultExpr = Expression.Default(typeof(int));
        var lambda = Expression.Lambda<Func<User, int>>(defaultExpr, param);

        var payload = ExpressionNodeBuilder.Build(lambda);
        var node = FindNodeByKind(payload.Roots[0], "Default");

        Assert.NotNull(node);
        Assert.Equal("Default", node.Kind);
        Assert.Contains("int", node.Display);
        Assert.Empty(node.Children);
    }

    [Fact]
    public void Build_Block_HasExpressions()
    {
        var variable = Expression.Variable(typeof(int), "temp");
        var assign = Expression.Assign(variable, Expression.Constant(42));
        var block = Expression.Block(new[] { variable }, assign, variable);
        var param = Expression.Parameter(typeof(User), "x");
        var lambda = Expression.Lambda<Func<User, int>>(block, param);

        var payload = ExpressionNodeBuilder.Build(lambda);
        var node = FindNodeByKind(payload.Roots[0], "Block");

        Assert.NotNull(node);
        Assert.Equal("Block", node.Kind);
        Assert.Equal(3, node.Children.Count);
    }

    [Fact]
    public void Build_TryCatch_HasHandlers()
    {
        var body = Expression.Constant(1);
        var catchBody = Expression.Constant(0);
        var handler = Expression.Catch(typeof(Exception), catchBody);
        var tryExpr = Expression.TryCatch(body, handler);
        var param = Expression.Parameter(typeof(User), "x");
        var lambda = Expression.Lambda<Func<User, int>>(tryExpr, param);

        var payload = ExpressionNodeBuilder.Build(lambda);
        var node = FindNodeByKind(payload.Roots[0], "Try");

        Assert.NotNull(node);
        Assert.Equal("Try", node.Kind);
        Assert.Equal(2, node.Children.Count);
        Assert.Equal("CatchBlock", node.Children[1].Kind);
    }

    [Fact]
    public void Build_Switch_HasCases()
    {
        var param = Expression.Parameter(typeof(User), "x");
        var switchValue = Expression.Property(param, nameof(User.Age));
        var case1 = Expression.SwitchCase(Expression.Constant("young"), Expression.Constant(18));
        var case2 = Expression.SwitchCase(Expression.Constant("old"), Expression.Constant(65));
        var switchExpr = Expression.Switch(switchValue, Expression.Constant("other"), case1, case2);
        var lambda = Expression.Lambda<Func<User, string>>(switchExpr, param);

        var payload = ExpressionNodeBuilder.Build(lambda);
        var node = FindNodeByKind(payload.Roots[0], "Switch");

        Assert.NotNull(node);
        Assert.Equal("Switch", node.Kind);
        Assert.Equal(4, node.Children.Count);
        Assert.Equal("SwitchCase", node.Children[1].Kind);
    }

    [Fact]
    public void Build_Goto_SetsTargetName()
    {
        var label = Expression.Label(typeof(int), "result");
        var gotoExpr = Expression.Goto(label, Expression.Constant(42));
        var labelExpr = Expression.Label(label, Expression.Constant(0));
        var block = Expression.Block(gotoExpr, labelExpr);
        var param = Expression.Parameter(typeof(User), "x");
        var lambda = Expression.Lambda<Func<User, int>>(block, param);

        var payload = ExpressionNodeBuilder.Build(lambda);
        var node = FindNodeByKind(payload.Roots[0], "Goto");

        Assert.NotNull(node);
        Assert.Equal("Goto", node.Kind);
        Assert.Contains("result", node.Display);
    }

    [Fact]
    public void Build_Label_SetsTargetName()
    {
        var label = Expression.Label(typeof(int), "result");
        var labelExpr = Expression.Label(label, Expression.Constant(0));
        var param = Expression.Parameter(typeof(User), "x");
        var lambda = Expression.Lambda<Func<User, int>>(labelExpr, param);

        var payload = ExpressionNodeBuilder.Build(lambda);
        var node = FindNodeByKind(payload.Roots[0], "Label");

        Assert.NotNull(node);
        Assert.Equal("Label", node.Kind);
        Assert.Contains("result", node.Display);
    }

    [Fact]
    public void Build_Loop_HasBody()
    {
        var breakLabel = Expression.Label("break");
        var loop = Expression.Loop(Expression.Break(breakLabel), breakLabel);
        var param = Expression.Parameter(typeof(User), "x");
        var block = Expression.Block(loop, Expression.Default(typeof(int)));
        var lambda = Expression.Lambda<Func<User, int>>(block, param);

        var payload = ExpressionNodeBuilder.Build(lambda);
        var node = FindNodeByKind(payload.Roots[0], "Loop");

        Assert.NotNull(node);
        Assert.Equal("Loop", node.Kind);
        Assert.Single(node.Children);
    }

    [Fact]
    public void Build_Lambda_HasReturnTypeDetail()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 0;
        var payload = ExpressionNodeBuilder.Build(expr);
        var root = payload.Roots[0];

        Assert.Contains(root.Details, d => d.Name == "ReturnType" && d.Value == "bool");
        Assert.Contains(root.Details, d => d.Name == "ParameterCount" && d.Value == "1");
    }

    [Fact]
    public void Build_Binary_HasIsLiftedDetail()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 0;
        var payload = ExpressionNodeBuilder.Build(expr);
        var binary = FindNodeByKind(payload.Roots[0], "Binary");

        Assert.NotNull(binary);
        Assert.Contains(binary.Details, d => d.Name == "IsLifted");
        Assert.Contains(binary.Details, d => d.Name == "Method");
    }

    [Fact]
    public void Build_Predicate_CollectsParameterEndNodes()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;

        var payload = ExpressionNodeBuilder.Build(expr);
        var parameters = payload.EndNodes.Where(e => e.Category == "Parameter").ToList();

        Assert.Contains(parameters, p => p.Name == "x" && p.TypeDisplay == "User");
    }

    [Fact]
    public void Build_Predicate_CollectsConstantEndNodes()
    {
        Expression<Func<User, bool>> expr = x => x.Age > 20;

        var payload = ExpressionNodeBuilder.Build(expr);
        var constants = payload.EndNodes.Where(e => e.Category == "Constant").ToList();

        Assert.Contains(constants, c => c.Name == "20");
    }

    [Fact]
    public void Build_Closure_CollectsClosedOverEndNodes()
    {
        int threshold = 5;
        Expression<Func<int, bool>> expr = x => x > threshold;

        var payload = ExpressionNodeBuilder.Build(expr);
        var closedOver = payload.EndNodes.Where(e => e.Category == "ClosedOver").ToList();

        Assert.Contains(closedOver, c => c.Name == "threshold");
    }

    [Fact]
    public void Build_Default_CollectsDefaultEndNodes()
    {
        var param = Expression.Parameter(typeof(User), "x");
        var defaultExpr = Expression.Default(typeof(int));
        var lambda = Expression.Lambda<Func<User, int>>(defaultExpr, param);

        var payload = ExpressionNodeBuilder.Build(lambda);
        var defaults = payload.EndNodes.Where(e => e.Category == "Default").ToList();

        Assert.Single(defaults);
        Assert.Contains("int", defaults[0].Name);
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
