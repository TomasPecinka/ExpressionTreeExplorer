using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace ExpressionTreeExplorer.Core;

public static class ExpressionNodeBuilder
{
    public static ExpressionPayload Build(Expression expr)
    {
        return new ExpressionPayload
        {
            Roots = [BuildNode(expr, "0")],
            ReadableText = expr.ToString(),
            DebugText = expr.ToString(),
            Summary = $"{expr.NodeType} : {expr.Type}",
        };
    }

    private static ExpressionNode BuildNode(Expression expr, string path)
    {
        return expr switch
        {
            LambdaExpression lambda => BuildLambda(lambda, path),
            BinaryExpression binary => BuildBinary(binary, path),
            MemberExpression member => BuildMember(member, path),
            ConstantExpression constant => BuildConstant(constant, path),
            ParameterExpression parameter => BuildParameter(parameter, path),

            _ => BuildFallback(expr, path)
        };
    }

    private static ExpressionNode BuildLambda(LambdaExpression lambda, string path)
    {
        return new ExpressionNode
        {
            Path = path,
            Display = $"Lambda ({string.Join(", ", lambda.Parameters.Select(p => p.Name))})",
            TypeDisplay = SimplifyType(lambda.Type),
            Kind = "Lambda",
            NodeType = lambda.NodeType.ToString(),
            Children =
            [
                BuildNode(lambda.Body, $"{path}/0")
            ]
        };
    }

    private static ExpressionNode BuildBinary(BinaryExpression binary, string path)
    {
        return new ExpressionNode
        {
            Path = path,
            Display = binary.NodeType.ToString(),
            TypeDisplay = binary.Type.Name,
            Kind = "Binary",
            NodeType = binary.NodeType.ToString(),
            Children =
            [
                BuildNode(binary.Left, $"{path}/0"),
                BuildNode(binary.Right, $"{path}/1")
            ]
        };
    }

    private static ExpressionNode BuildMember(MemberExpression member, string path)
    {
        var node = new ExpressionNode
        {
            Path = path,
            Display = member.Member.Name,
            TypeDisplay = member.Type.Name,
            Kind = "Member",
            NodeType = member.NodeType.ToString(),
        };

        if (member.Expression is not null)
        {
            node.Children =
            [
                BuildNode(member.Expression, $"{path}/0")
            ];
        }

        return node;
    }

    private static ExpressionNode BuildConstant(ConstantExpression constant, string path)
    {
        return new ExpressionNode
        {
            Path = path,
            Display = FormatConstant(constant),
            TypeDisplay = constant.Type.Name,
            Kind = "Constant",
            NodeType = constant.NodeType.ToString(),
        };
    }

    private static ExpressionNode BuildParameter(ParameterExpression parameter, string path)
    {
        return new ExpressionNode
        {
            Path = path,
            Display = parameter.Name ?? "param",
            TypeDisplay = parameter.Type.Name,
            Kind = "Parameter",
            NodeType = parameter.NodeType.ToString(),
        };
    }

    private static ExpressionNode BuildFallback(Expression expr, string path)
    {
        return new ExpressionNode
        {
            Path = path,
            Display = expr.NodeType.ToString(),
            TypeDisplay = expr.Type.Name,
            Kind = "Unsupported",
            NodeType = expr.NodeType.ToString(),
        };
    }

    private static string SimplifyType(Type type)
    {
        if (type == typeof(int))
        {
            return "int";
        }

        if (type == typeof(bool))
        {
            return "bool";
        }

        if (type.IsGenericType)
        {
            var tickIndex = type.Name.IndexOf('`');
            var genericName = tickIndex > 0
                ? type.Name.Substring(0, tickIndex)
                : type.Name;

            var args = type.GetGenericArguments().Select(SimplifyType);

            return $"{genericName}<{string.Join(", ", args)}>";
        }

        return type.Name;
    }

    private static string FormatConstant(ConstantExpression constant)
    {
        return constant.Value switch
        {
            null => "null",
            string s => $"\"{s}\"",
            char c => $"'{c}'",
            int or long or short or byte or float or double or decimal or bool => Convert.ToString(constant.Value, CultureInfo.InvariantCulture) ?? constant.Type.Name,
            _ => $"<{constant.Value.GetType().Name}>",
        };
    }
}
