using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

namespace ExpressionTreeExplorer.Core;

public sealed class SpanFormatterResult
{
    public SpanFormatterResult(string text, IReadOnlyList<SourceSpan> spans)
    {
        Text = text;
        Spans = spans;
    }

    public string Text { get; }
    public IReadOnlyList<SourceSpan> Spans { get; }
}

public static class SpanTrackingFormatter
{
    public static SpanFormatterResult Format(Expression expression)
    {
        var ctx = new FormatContext();
        FormatNode(expression, "0", ctx);
        return new SpanFormatterResult(ctx.Builder.ToString(), ctx.Spans);
    }

    private sealed class FormatContext
    {
        public StringBuilder Builder { get; } = new();
        public List<SourceSpan> Spans { get; } = new();
        public int IndentLevel { get; set; }

        public int Position => Builder.Length;

        public void Append(string text) => Builder.Append(text);
        public void Append(char c) => Builder.Append(c);
        public void AppendLine()
        {
            Builder.Append('\n');
            Builder.Append(' ', IndentLevel * 4);
        }

        public SpanTracker TrackSpan(string path) => new(this, path);
    }

    private sealed class SpanTracker : IDisposable
    {
        private readonly FormatContext _ctx;
        private readonly string _path;
        private readonly int _start;

        public SpanTracker(FormatContext ctx, string path)
        {
            _ctx = ctx;
            _path = path;
            _start = ctx.Position;
        }

        public void Dispose()
        {
            _ctx.Spans.Add(new SourceSpan
            {
                Path = _path,
                Start = _start,
                Length = _ctx.Position - _start
            });
        }
    }

    private static void FormatNode(Expression expr, string path, FormatContext ctx)
    {
        using (ctx.TrackSpan(path))
        {
            switch (expr)
            {
                case LambdaExpression lambda:
                    FormatLambda(lambda, path, ctx);
                    break;
                case BinaryExpression binary:
                    FormatBinary(binary, path, ctx);
                    break;
                case ConditionalExpression conditional:
                    FormatConditional(conditional, path, ctx);
                    break;
                case MemberExpression member:
                    FormatMember(member, path, ctx);
                    break;
                case ConstantExpression constant:
                    FormatConstant(constant, ctx);
                    break;
                case ParameterExpression parameter:
                    FormatParameter(parameter, ctx);
                    break;
                case MethodCallExpression methodCall:
                    FormatMethodCall(methodCall, path, ctx);
                    break;
                case UnaryExpression unary:
                    FormatUnary(unary, path, ctx);
                    break;
                case MemberInitExpression memberInit:
                    FormatMemberInit(memberInit, path, ctx);
                    break;
                case ListInitExpression listInit:
                    FormatListInit(listInit, path, ctx);
                    break;
                case NewExpression newExpr:
                    FormatNew(newExpr, path, ctx);
                    break;
                case NewArrayExpression newArray:
                    FormatNewArray(newArray, path, ctx);
                    break;
                case TypeBinaryExpression typeBinary:
                    FormatTypeBinary(typeBinary, path, ctx);
                    break;
                case InvocationExpression invocation:
                    FormatInvocation(invocation, path, ctx);
                    break;
                case IndexExpression index:
                    FormatIndex(index, path, ctx);
                    break;
                case DefaultExpression defaultExpr:
                    FormatDefault(defaultExpr, ctx);
                    break;
                case BlockExpression block:
                    FormatBlock(block, path, ctx);
                    break;
                case TryExpression tryExpr:
                    FormatTry(tryExpr, path, ctx);
                    break;
                case SwitchExpression switchExpr:
                    FormatSwitch(switchExpr, path, ctx);
                    break;
                case GotoExpression gotoExpr:
                    FormatGoto(gotoExpr, path, ctx);
                    break;
                case LabelExpression label:
                    FormatLabel(label, path, ctx);
                    break;
                case LoopExpression loop:
                    FormatLoop(loop, path, ctx);
                    break;
                default:
                    FormatFallback(expr, ctx);
                    break;
            }
        }
    }

    // Lambda: body=0 (params are NOT children, matching BuildLambda)
    private static void FormatLambda(LambdaExpression lambda, string path, FormatContext ctx)
    {
        if (lambda.Parameters.Count == 1)
        {
            ctx.Append(lambda.Parameters[0].Name ?? "param");
        }
        else
        {
            ctx.Append('(');
            for (var i = 0; i < lambda.Parameters.Count; i++)
            {
                if (i > 0)
                {
                    ctx.Append(", ");
                }

                ctx.Append(lambda.Parameters[i].Name ?? "param");
            }
            ctx.Append(')');
        }

        ctx.Append(" => ");

        if (lambda.Body is BlockExpression)
        {
            ctx.AppendLine();
        }

        FormatNode(lambda.Body, $"{path}/0", ctx);
    }

    // Binary: left=0, right=1
    private static void FormatBinary(BinaryExpression binary, string path, FormatContext ctx)
    {
        var needsParens = binary.Left is BinaryExpression || binary.Right is BinaryExpression;

        if (needsParens && binary.Left is BinaryExpression)
        {
            ctx.Append('(');
            FormatNode(binary.Left, $"{path}/0", ctx);
            ctx.Append(')');
        }
        else
        {
            FormatNode(binary.Left, $"{path}/0", ctx);
        }

        ctx.Append($" {GetBinaryOperator(binary.NodeType)} ");

        if (needsParens && binary.Right is BinaryExpression)
        {
            ctx.Append('(');
            FormatNode(binary.Right, $"{path}/1", ctx);
            ctx.Append(')');
        }
        else
        {
            FormatNode(binary.Right, $"{path}/1", ctx);
        }
    }

    // Member: expression=0 (if exists)
    private static void FormatMember(MemberExpression member, string path, FormatContext ctx)
    {
        if (member.Expression is ConstantExpression ce
            && ce.Type.IsDefined(typeof(CompilerGeneratedAttribute), false))
        {
            ctx.Append(member.Member.Name);
        }
        else if (member.Expression != null)
        {
            FormatNode(member.Expression, $"{path}/0", ctx);
            ctx.Append('.');
            ctx.Append(member.Member.Name);
        }
        else
        {
            ctx.Append(ExpressionHelpers.SimplifyType(member.Member.DeclaringType ?? member.Type));
            ctx.Append('.');
            ctx.Append(member.Member.Name);
        }
    }

    // Constant: no children
    private static void FormatConstant(ConstantExpression constant, FormatContext ctx)
    {
        if (constant.Type.IsDefined(typeof(CompilerGeneratedAttribute), false))
        {
            ctx.Append("<closure>");
            return;
        }
        ctx.Append(ExpressionHelpers.FormatConstant(constant));
    }

    // Parameter: no children
    private static void FormatParameter(ParameterExpression parameter, FormatContext ctx)
    {
        ctx.Append(parameter.Name ?? "param");
    }

    // MethodCall: object=0 (if exists), args sequentially
    private static void FormatMethodCall(MethodCallExpression mc, string path, FormatContext ctx)
    {
        var index = 0;
        if (mc.Object != null)
        {
            FormatNode(mc.Object, $"{path}/{index++}", ctx);
            ctx.Append('.');
        }
        else if (mc.Method.DeclaringType != null)
        {
            ctx.Append(ExpressionHelpers.SimplifyType(mc.Method.DeclaringType));
            ctx.Append('.');
        }

        ctx.Append(mc.Method.Name);
        ctx.Append('(');
        for (var i = 0; i < mc.Arguments.Count; i++)
        {
            if (i > 0)
            {
                ctx.Append(", ");
            }

            FormatNode(mc.Arguments[i], $"{path}/{index++}", ctx);
        }
        ctx.Append(')');
    }

    // Unary: operand=0
    private static void FormatUnary(UnaryExpression u, string path, FormatContext ctx)
    {
        switch (u.NodeType)
        {
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
                ctx.Append($"({ExpressionHelpers.SimplifyType(u.Type)})");
                FormatNode(u.Operand, $"{path}/0", ctx);
                break;
            case ExpressionType.Not:
                ctx.Append('!');
                FormatNode(u.Operand, $"{path}/0", ctx);
                break;
            case ExpressionType.Negate:
            case ExpressionType.NegateChecked:
                ctx.Append('-');
                FormatNode(u.Operand, $"{path}/0", ctx);
                break;
            case ExpressionType.TypeAs:
                FormatNode(u.Operand, $"{path}/0", ctx);
                ctx.Append($" as {ExpressionHelpers.SimplifyType(u.Type)}");
                break;
            case ExpressionType.Quote:
                FormatNode(u.Operand, $"{path}/0", ctx);
                break;
            case ExpressionType.ArrayLength:
                FormatNode(u.Operand, $"{path}/0", ctx);
                ctx.Append(".Length");
                break;
            case ExpressionType.UnaryPlus:
                ctx.Append('+');
                FormatNode(u.Operand, $"{path}/0", ctx);
                break;
            default:
                ctx.Append($"/* {u.NodeType} */ ");
                FormatNode(u.Operand, $"{path}/0", ctx);
                break;
        }
    }

    // MemberInit: new=0, bindings from 1, each binding child at {path}/{i}/0
    private static void FormatMemberInit(MemberInitExpression mi, string path, FormatContext ctx)
    {
        FormatNode(mi.NewExpression, $"{path}/0", ctx);
        ctx.AppendLine();
        ctx.Append('{');
        ctx.IndentLevel++;

        var index = 1;
        foreach (var binding in mi.Bindings)
        {
            if (binding is MemberAssignment assignment)
            {
                var currentIndex = index++;
                ctx.AppendLine();
                using (ctx.TrackSpan($"{path}/{currentIndex}"))
                {
                    ctx.Append(binding.Member.Name);
                    ctx.Append(" = ");
                    FormatNode(assignment.Expression, $"{path}/{currentIndex}/0", ctx);
                }

                if (currentIndex < mi.Bindings.Count)
                {
                    ctx.Append(',');
                }
            }
        }

        ctx.IndentLevel--;
        ctx.AppendLine();
        ctx.Append('}');
    }

    // Conditional: test=0, ifTrue=1, ifFalse=2
    private static void FormatConditional(ConditionalExpression c, string path, FormatContext ctx)
    {
        FormatNode(c.Test, $"{path}/0", ctx);
        ctx.Append(" ? ");
        FormatNode(c.IfTrue, $"{path}/1", ctx);
        ctx.Append(" : ");
        FormatNode(c.IfFalse, $"{path}/2", ctx);
    }

    // New: args 0..n-1
    private static void FormatNew(NewExpression n, string path, FormatContext ctx)
    {
        ctx.Append($"new {ExpressionHelpers.SimplifyType(n.Type)}(");
        for (var i = 0; i < n.Arguments.Count; i++)
        {
            if (i > 0)
            {
                ctx.Append(", ");
            }

            FormatNode(n.Arguments[i], $"{path}/{i}", ctx);
        }
        ctx.Append(')');
    }

    // TypeBinary: expression=0
    private static void FormatTypeBinary(TypeBinaryExpression t, string path, FormatContext ctx)
    {
        FormatNode(t.Expression, $"{path}/0", ctx);
        ctx.Append($" is {ExpressionHelpers.SimplifyType(t.TypeOperand)}");
    }

    // Invocation: expression=0, args from 1
    private static void FormatInvocation(InvocationExpression inv, string path, FormatContext ctx)
    {
        FormatNode(inv.Expression, $"{path}/0", ctx);
        ctx.Append('(');
        for (var i = 0; i < inv.Arguments.Count; i++)
        {
            if (i > 0)
            {
                ctx.Append(", ");
            }

            FormatNode(inv.Arguments[i], $"{path}/{i + 1}", ctx);
        }
        ctx.Append(')');
    }

    // NewArray: items 0..n-1
    private static void FormatNewArray(NewArrayExpression na, string path, FormatContext ctx)
    {
        var elementType = na.Type.GetElementType();

        if (na.NodeType == ExpressionType.NewArrayInit)
        {
            ctx.Append($"new {ExpressionHelpers.SimplifyType(elementType ?? na.Type)}[] {{ ");
            for (var i = 0; i < na.Expressions.Count; i++)
            {
                if (i > 0)
                {
                    ctx.Append(", ");
                }

                FormatNode(na.Expressions[i], $"{path}/{i}", ctx);
            }
            ctx.Append(" }");
        }
        else
        {
            ctx.Append($"new {ExpressionHelpers.SimplifyType(elementType ?? na.Type)}[");
            for (var i = 0; i < na.Expressions.Count; i++)
            {
                if (i > 0)
                {
                    ctx.Append(", ");
                }

                FormatNode(na.Expressions[i], $"{path}/{i}", ctx);
            }
            ctx.Append(']');
        }
    }

    // ListInit: new=0, inits from 1, each init args at {path}/{i+1}/{j}
    private static void FormatListInit(ListInitExpression li, string path, FormatContext ctx)
    {
        FormatNode(li.NewExpression, $"{path}/0", ctx);
        ctx.Append(" { ");

        for (var i = 0; i < li.Initializers.Count; i++)
        {
            if (i > 0)
            {
                ctx.Append(", ");
            }

            var init = li.Initializers[i];

            using (ctx.TrackSpan($"{path}/{i + 1}"))
            {
                if (init.Arguments.Count == 1)
                {
                    FormatNode(init.Arguments[0], $"{path}/{i + 1}/0", ctx);
                }
                else
                {
                    ctx.Append("{ ");
                    for (var j = 0; j < init.Arguments.Count; j++)
                    {
                        if (j > 0)
                        {
                            ctx.Append(", ");
                        }

                        FormatNode(init.Arguments[j], $"{path}/{i + 1}/{j}", ctx);
                    }
                    ctx.Append(" }");
                }
            }
        }

        ctx.Append(" }");
    }

    // Index: object=0 (if exists), args sequentially
    private static void FormatIndex(IndexExpression ix, string path, FormatContext ctx)
    {
        var index = 0;
        if (ix.Object != null)
        {
            FormatNode(ix.Object, $"{path}/{index++}", ctx);
        }

        ctx.Append('[');
        for (var i = 0; i < ix.Arguments.Count; i++)
        {
            if (i > 0)
            {
                ctx.Append(", ");
            }

            FormatNode(ix.Arguments[i], $"{path}/{index++}", ctx);
        }
        ctx.Append(']');
    }

    // Default: no children
    private static void FormatDefault(DefaultExpression d, FormatContext ctx)
    {
        ctx.Append($"default({ExpressionHelpers.SimplifyType(d.Type)})");
    }

    // Block: vars 0..m-1, exprs m..m+n-1 (shared counter)
    private static void FormatBlock(BlockExpression b, string path, FormatContext ctx)
    {
        ctx.Append('{');
        ctx.IndentLevel++;
        var index = 0;

        foreach (var variable in b.Variables)
        {
            ctx.AppendLine();
            FormatNode(variable, $"{path}/{index++}", ctx);
            ctx.Append(';');
        }

        for (var i = 0; i < b.Expressions.Count; i++)
        {
            ctx.AppendLine();
            FormatNode(b.Expressions[i], $"{path}/{index++}", ctx);
            ctx.Append(';');
        }

        ctx.IndentLevel--;
        ctx.AppendLine();
        ctx.Append('}');
    }

    // Try: body=0, handlers from 1 (synthetic CatchBlock nodes), finally/fault at end
    private static void FormatTry(TryExpression t, string path, FormatContext ctx)
    {
        ctx.Append("try");
        ctx.AppendLine();
        ctx.Append('{');
        ctx.IndentLevel++;
        ctx.AppendLine();
        FormatNode(t.Body, $"{path}/0", ctx);
        ctx.Append(';');
        ctx.IndentLevel--;
        ctx.AppendLine();
        ctx.Append('}');

        var index = 1;
        foreach (var handler in t.Handlers)
        {
            using (ctx.TrackSpan($"{path}/{index}"))
            {
                ctx.AppendLine();
                ctx.Append($"catch ({ExpressionHelpers.SimplifyType(handler.Test)}");
                if (handler.Variable != null)
                {
                    ctx.Append($" {handler.Variable.Name}");
                }
                ctx.Append(')');
                ctx.AppendLine();
                ctx.Append('{');
                ctx.IndentLevel++;

                var handlerIndex = 0;
                if (handler.Filter != null)
                {
                    ctx.AppendLine();
                    ctx.Append("when (");
                    FormatNode(handler.Filter, $"{path}/{index}/{handlerIndex++}", ctx);
                    ctx.Append(')');
                }

                ctx.AppendLine();
                FormatNode(handler.Body, $"{path}/{index}/{handlerIndex}", ctx);
                ctx.Append(';');
                ctx.IndentLevel--;
                ctx.AppendLine();
                ctx.Append('}');
            }
            index++;
        }

        if (t.Finally != null)
        {
            ctx.AppendLine();
            ctx.Append("finally");
            ctx.AppendLine();
            ctx.Append('{');
            ctx.IndentLevel++;
            ctx.AppendLine();
            FormatNode(t.Finally, $"{path}/{index++}", ctx);
            ctx.Append(';');
            ctx.IndentLevel--;
            ctx.AppendLine();
            ctx.Append('}');
        }

        if (t.Fault != null)
        {
            ctx.AppendLine();
            ctx.Append("fault");
            ctx.AppendLine();
            ctx.Append('{');
            ctx.IndentLevel++;
            ctx.AppendLine();
            FormatNode(t.Fault, $"{path}/{index}", ctx);
            ctx.Append(';');
            ctx.IndentLevel--;
            ctx.AppendLine();
            ctx.Append('}');
        }
    }

    // Switch: value=0, cases from 1 (synthetic SwitchCase nodes), default at end
    private static void FormatSwitch(SwitchExpression s, string path, FormatContext ctx)
    {
        ctx.Append("switch (");
        FormatNode(s.SwitchValue, $"{path}/0", ctx);
        ctx.Append(')');
        ctx.AppendLine();
        ctx.Append('{');
        ctx.IndentLevel++;

        var index = 1;
        foreach (var c in s.Cases)
        {
            using (ctx.TrackSpan($"{path}/{index}"))
            {
                var caseIndex = 0;
                foreach (var testValue in c.TestValues)
                {
                    ctx.AppendLine();
                    ctx.Append("case ");
                    FormatNode(testValue, $"{path}/{index}/{caseIndex++}", ctx);
                    ctx.Append(':');
                }
                ctx.IndentLevel++;
                ctx.AppendLine();
                FormatNode(c.Body, $"{path}/{index}/{caseIndex}", ctx);
                ctx.Append(';');
                ctx.IndentLevel--;
            }
            index++;
        }

        if (s.DefaultBody != null)
        {
            ctx.AppendLine();
            ctx.Append("default:");
            ctx.IndentLevel++;
            ctx.AppendLine();
            FormatNode(s.DefaultBody, $"{path}/{index}", ctx);
            ctx.Append(';');
            ctx.IndentLevel--;
        }

        ctx.IndentLevel--;
        ctx.AppendLine();
        ctx.Append('}');
    }

    // Goto: value=0 (if exists)
    private static void FormatGoto(GotoExpression g, string path, FormatContext ctx)
    {
        switch (g.Kind)
        {
            case GotoExpressionKind.Return:
                ctx.Append("return");
                break;
            case GotoExpressionKind.Break:
                ctx.Append("break");
                break;
            case GotoExpressionKind.Continue:
                ctx.Append("continue");
                break;
            default:
                ctx.Append($"goto {g.Target.Name}");
                break;
        }

        if (g.Value != null)
        {
            ctx.Append(' ');
            FormatNode(g.Value, $"{path}/0", ctx);
        }
    }

    // Label: defaultValue=0 (if exists)
    private static void FormatLabel(LabelExpression l, string path, FormatContext ctx)
    {
        ctx.Append($"{l.Target.Name}:");
        if (l.DefaultValue != null)
        {
            ctx.Append(' ');
            FormatNode(l.DefaultValue, $"{path}/0", ctx);
        }
    }

    // Loop: body=0
    private static void FormatLoop(LoopExpression l, string path, FormatContext ctx)
    {
        ctx.Append("while (true)");
        ctx.AppendLine();
        ctx.Append('{');
        ctx.IndentLevel++;
        ctx.AppendLine();
        FormatNode(l.Body, $"{path}/0", ctx);
        ctx.Append(';');
        ctx.IndentLevel--;
        ctx.AppendLine();
        ctx.Append('}');
    }

    private static void FormatFallback(Expression expr, FormatContext ctx)
    {
        ctx.Append($"/* {expr.NodeType} */");
    }

    private static string GetBinaryOperator(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Add or ExpressionType.AddChecked => "+",
            ExpressionType.Subtract or ExpressionType.SubtractChecked => "-",
            ExpressionType.Multiply or ExpressionType.MultiplyChecked => "*",
            ExpressionType.Divide => "/",
            ExpressionType.Modulo => "%",
            ExpressionType.Equal => "==",
            ExpressionType.NotEqual => "!=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.LessThan => "<",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.AndAlso => "&&",
            ExpressionType.OrElse => "||",
            ExpressionType.And => "&",
            ExpressionType.Or => "|",
            ExpressionType.ExclusiveOr => "^",
            ExpressionType.LeftShift => "<<",
            ExpressionType.RightShift => ">>",
            ExpressionType.Assign => "=",
            ExpressionType.Coalesce => "??",
            ExpressionType.AddAssign or ExpressionType.AddAssignChecked => "+=",
            ExpressionType.SubtractAssign or ExpressionType.SubtractAssignChecked => "-=",
            ExpressionType.MultiplyAssign or ExpressionType.MultiplyAssignChecked => "*=",
            ExpressionType.DivideAssign => "/=",
            ExpressionType.ModuloAssign => "%=",
            ExpressionType.Power => "**",
            _ => nodeType.ToString()
        };
    }
}
