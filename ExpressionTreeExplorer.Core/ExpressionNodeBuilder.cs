using System;
using System.Collections.Generic;
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
            MethodCallExpression methodCall => BuildMethodCall(methodCall, path),
            UnaryExpression unary => BuildUnary(unary, path),
            MemberInitExpression memberInit => BuildMemberInit(memberInit, path),

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

    private static ExpressionNode BuildMethodCall(MethodCallExpression mc, string path)
    {
        var children = new List<ExpressionNode>();
        var index = 0;

        if (mc.Object != null)
        {
            children.Add(BuildNode(mc.Object, $"{path}/{index++}"));
        }

        foreach (var arg in mc.Arguments)
        {
            children.Add(BuildNode(arg, $"{path}/{index++}"));
        }

        return new ExpressionNode
        {
            Path = path,
            Kind = "MethodCall",
            NodeType = mc.NodeType.ToString(),
            Display = mc.Method.Name,
            TypeDisplay = mc.Type.ToString(),
            Details =
            [
                Detail("Method", mc.Method.Name),
                Detail("DeclaringType", mc.Method.DeclaringType?.ToString() ?? "-"),
                Detail("ArgumentsCount", mc.Arguments.Count.ToString())
            ],
            Children = children
        };
    }

    private static ExpressionNode BuildUnary(UnaryExpression u, string path)
    {
        var display = u.NodeType switch
        {
            ExpressionType.Convert => $"Convert → {u.Type.Name}",
            ExpressionType.Quote => "Quote",
            ExpressionType.Not => "Not",
            ExpressionType.TypeAs => $"TypeAs → {u.Type.Name}",
            _ => u.NodeType.ToString()
        };

        return new ExpressionNode
        {
            Path = path,
            Kind = "Unary",
            NodeType = u.NodeType.ToString(),
            Display = display,
            TypeDisplay = u.Type.ToString(),
            Details =
            [
                Detail("Method", u.Method?.Name ?? "-")
            ],
            Children = [BuildNode(u.Operand, $"{path}/0")]
        };
    }

    private static ExpressionNode BuildMemberInit(MemberInitExpression mi, string path)
    {
        var children = new List<ExpressionNode>
    {
        BuildNode(mi.NewExpression, $"{path}/0")
    };

        var index = 1;

        foreach (var binding in mi.Bindings)
        {
            if (binding is MemberAssignment assignment)
            {
                var currentIndex = index++;

                children.Add(new ExpressionNode
                {
                    Path = $"{path}/{currentIndex}",
                    Kind = "Binding",
                    NodeType = "MemberAssignment",
                    Display = binding.Member.Name,
                    TypeDisplay = assignment.Expression.Type.ToString(),
                    Details =
                    [
                        Detail("Member", binding.Member.Name)
                    ],
                    Children =
                    [
                        BuildNode(assignment.Expression, $"{path}/{currentIndex}/0")
                    ]
                });
            }
        }

        return new ExpressionNode
        {
            Path = path,
            Kind = "MemberInit",
            NodeType = mi.NodeType.ToString(),
            Display = "MemberInit",
            TypeDisplay = mi.Type.ToString(),
            Details =
            [
                Detail("BindingsCount", mi.Bindings.Count.ToString())
            ],
            Children = children
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

    private static NodeDetail Detail(string name, string value)
    {
        return new NodeDetail
        {
            Name = name,
            Value = value
        };
    }
}
