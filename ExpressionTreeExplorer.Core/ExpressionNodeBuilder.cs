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
            ConditionalExpression conditional => BuildConditional(conditional, path),
            MemberExpression member => BuildMember(member, path),
            ConstantExpression constant => BuildConstant(constant, path),
            ParameterExpression parameter => BuildParameter(parameter, path),
            MethodCallExpression methodCall => BuildMethodCall(methodCall, path),
            UnaryExpression unary => BuildUnary(unary, path),
            MemberInitExpression memberInit => BuildMemberInit(memberInit, path),
            ListInitExpression listInit => BuildListInit(listInit, path),
            NewExpression newExpr => BuildNew(newExpr, path),
            NewArrayExpression newArray => BuildNewArray(newArray, path),
            TypeBinaryExpression typeBinary => BuildTypeBinary(typeBinary, path),
            InvocationExpression invocation => BuildInvocation(invocation, path),
            IndexExpression index => BuildIndex(index, path),
            DefaultExpression defaultExpr => BuildDefault(defaultExpr, path),
            BlockExpression block => BuildBlock(block, path),
            TryExpression tryExpr => BuildTry(tryExpr, path),
            SwitchExpression switchExpr => BuildSwitch(switchExpr, path),
            GotoExpression gotoExpr => BuildGoto(gotoExpr, path),
            LabelExpression label => BuildLabel(label, path),
            LoopExpression loop => BuildLoop(loop, path),

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
            Details =
            [
                Detail("ReturnType", SimplifyType(lambda.ReturnType)),
                Detail("ParameterCount", lambda.Parameters.Count.ToString())
            ],
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
            Details =
            [
                Detail("IsLifted", binary.IsLifted.ToString()),
                Detail("Method", binary.Method?.Name ?? "-")
            ],
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
            Details =
            [
                Detail("DeclaringType", member.Member.DeclaringType?.Name ?? "-"),
                Detail("MemberType", member.Member.MemberType.ToString())
            ],
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
            Details =
            [
                Detail("Value", FormatConstant(constant)),
                Detail("ClrType", constant.Type.Name)
            ],
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
            Details =
            [
                Detail("IsByRef", parameter.IsByRef.ToString())
            ],
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

    private static ExpressionNode BuildConditional(ConditionalExpression c, string path)
    {
        return new ExpressionNode
        {
            Path = path,
            Kind = "Conditional",
            NodeType = c.NodeType.ToString(),
            Display = "Conditional",
            TypeDisplay = SimplifyType(c.Type),
            Details =
            [
                Detail("Type", SimplifyType(c.Type))
            ],
            Children =
            [
                BuildNode(c.Test, $"{path}/0"),
                BuildNode(c.IfTrue, $"{path}/1"),
                BuildNode(c.IfFalse, $"{path}/2")
            ]
        };
    }

    private static ExpressionNode BuildNew(NewExpression n, string path)
    {
        var children = new List<ExpressionNode>();
        for (var i = 0; i < n.Arguments.Count; i++)
        {
            children.Add(BuildNode(n.Arguments[i], $"{path}/{i}"));
        }

        return new ExpressionNode
        {
            Path = path,
            Kind = "New",
            NodeType = n.NodeType.ToString(),
            Display = $"new {SimplifyType(n.Type)}",
            TypeDisplay = SimplifyType(n.Type),
            Details =
            [
                Detail("Constructor", n.Constructor?.ToString() ?? "-"),
                Detail("ArgumentsCount", n.Arguments.Count.ToString())
            ],
            Children = children
        };
    }

    private static ExpressionNode BuildTypeBinary(TypeBinaryExpression t, string path)
    {
        return new ExpressionNode
        {
            Path = path,
            Kind = "TypeBinary",
            NodeType = t.NodeType.ToString(),
            Display = $"{t.NodeType} {SimplifyType(t.TypeOperand)}",
            TypeDisplay = SimplifyType(t.Type),
            Details =
            [
                Detail("TypeOperand", SimplifyType(t.TypeOperand))
            ],
            Children =
            [
                BuildNode(t.Expression, $"{path}/0")
            ]
        };
    }

    private static ExpressionNode BuildInvocation(InvocationExpression inv, string path)
    {
        var children = new List<ExpressionNode>
        {
            BuildNode(inv.Expression, $"{path}/0")
        };
        for (var i = 0; i < inv.Arguments.Count; i++)
        {
            children.Add(BuildNode(inv.Arguments[i], $"{path}/{i + 1}"));
        }

        return new ExpressionNode
        {
            Path = path,
            Kind = "Invocation",
            NodeType = inv.NodeType.ToString(),
            Display = "Invoke",
            TypeDisplay = SimplifyType(inv.Type),
            Details =
            [
                Detail("ArgumentsCount", inv.Arguments.Count.ToString())
            ],
            Children = children
        };
    }

    private static ExpressionNode BuildNewArray(NewArrayExpression na, string path)
    {
        var children = new List<ExpressionNode>();
        for (var i = 0; i < na.Expressions.Count; i++)
        {
            children.Add(BuildNode(na.Expressions[i], $"{path}/{i}"));
        }

        var elementType = na.Type.GetElementType();

        return new ExpressionNode
        {
            Path = path,
            Kind = "NewArray",
            NodeType = na.NodeType.ToString(),
            Display = $"{na.NodeType} {SimplifyType(elementType ?? na.Type)}",
            TypeDisplay = SimplifyType(na.Type),
            Details =
            [
                Detail("ElementType", SimplifyType(elementType ?? na.Type))
            ],
            Children = children
        };
    }

    private static ExpressionNode BuildListInit(ListInitExpression li, string path)
    {
        var children = new List<ExpressionNode>
        {
            BuildNode(li.NewExpression, $"{path}/0")
        };

        for (var i = 0; i < li.Initializers.Count; i++)
        {
            var init = li.Initializers[i];
            var initChildren = new List<ExpressionNode>();
            for (var j = 0; j < init.Arguments.Count; j++)
            {
                initChildren.Add(BuildNode(init.Arguments[j], $"{path}/{i + 1}/{j}"));
            }

            children.Add(new ExpressionNode
            {
                Path = $"{path}/{i + 1}",
                Kind = "ElementInit",
                NodeType = "ElementInit",
                Display = init.AddMethod.Name,
                TypeDisplay = "",
                Details =
                [
                    Detail("AddMethod", init.AddMethod.Name),
                    Detail("ArgumentsCount", init.Arguments.Count.ToString())
                ],
                Children = initChildren
            });
        }

        return new ExpressionNode
        {
            Path = path,
            Kind = "ListInit",
            NodeType = li.NodeType.ToString(),
            Display = "ListInit",
            TypeDisplay = SimplifyType(li.Type),
            Details =
            [
                Detail("InitializersCount", li.Initializers.Count.ToString())
            ],
            Children = children
        };
    }

    private static ExpressionNode BuildIndex(IndexExpression ix, string path)
    {
        var children = new List<ExpressionNode>();
        var index = 0;

        if (ix.Object != null)
        {
            children.Add(BuildNode(ix.Object, $"{path}/{index++}"));
        }

        foreach (var arg in ix.Arguments)
        {
            children.Add(BuildNode(arg, $"{path}/{index++}"));
        }

        return new ExpressionNode
        {
            Path = path,
            Kind = "Index",
            NodeType = ix.NodeType.ToString(),
            Display = ix.Indexer?.Name ?? "Item",
            TypeDisplay = SimplifyType(ix.Type),
            Details =
            [
                Detail("Indexer", ix.Indexer?.Name ?? "-")
            ],
            Children = children
        };
    }

    private static ExpressionNode BuildDefault(DefaultExpression d, string path)
    {
        return new ExpressionNode
        {
            Path = path,
            Kind = "Default",
            NodeType = d.NodeType.ToString(),
            Display = $"default({SimplifyType(d.Type)})",
            TypeDisplay = SimplifyType(d.Type),
            Details =
            [
                Detail("Type", SimplifyType(d.Type))
            ],
        };
    }

    private static ExpressionNode BuildBlock(BlockExpression b, string path)
    {
        var children = new List<ExpressionNode>();
        var index = 0;

        foreach (var variable in b.Variables)
        {
            children.Add(BuildNode(variable, $"{path}/{index++}"));
        }

        foreach (var expr in b.Expressions)
        {
            children.Add(BuildNode(expr, $"{path}/{index++}"));
        }

        return new ExpressionNode
        {
            Path = path,
            Kind = "Block",
            NodeType = b.NodeType.ToString(),
            Display = "Block",
            TypeDisplay = SimplifyType(b.Type),
            Details =
            [
                Detail("ExpressionsCount", b.Expressions.Count.ToString()),
                Detail("VariablesCount", b.Variables.Count.ToString())
            ],
            Children = children
        };
    }

    private static ExpressionNode BuildTry(TryExpression t, string path)
    {
        var children = new List<ExpressionNode>
        {
            BuildNode(t.Body, $"{path}/0")
        };

        var index = 1;
        foreach (var handler in t.Handlers)
        {
            var handlerChildren = new List<ExpressionNode>();
            var handlerIndex = 0;

            if (handler.Filter != null)
            {
                handlerChildren.Add(BuildNode(handler.Filter, $"{path}/{index}/{handlerIndex++}"));
            }

            handlerChildren.Add(BuildNode(handler.Body, $"{path}/{index}/{handlerIndex}"));

            children.Add(new ExpressionNode
            {
                Path = $"{path}/{index}",
                Kind = "CatchBlock",
                NodeType = "CatchBlock",
                Display = $"catch ({SimplifyType(handler.Test)})",
                TypeDisplay = SimplifyType(handler.Test),
                Details =
                [
                    Detail("ExceptionType", SimplifyType(handler.Test)),
                    Detail("Variable", handler.Variable?.Name ?? "-")
                ],
                Children = handlerChildren
            });
            index++;
        }

        if (t.Finally != null)
        {
            children.Add(BuildNode(t.Finally, $"{path}/{index++}"));
        }

        if (t.Fault != null)
        {
            children.Add(BuildNode(t.Fault, $"{path}/{index}"));
        }

        return new ExpressionNode
        {
            Path = path,
            Kind = "Try",
            NodeType = t.NodeType.ToString(),
            Display = "Try",
            TypeDisplay = SimplifyType(t.Type),
            Details =
            [
                Detail("HandlersCount", t.Handlers.Count.ToString()),
                Detail("HasFinally", (t.Finally != null).ToString()),
                Detail("HasFault", (t.Fault != null).ToString())
            ],
            Children = children
        };
    }

    private static ExpressionNode BuildSwitch(SwitchExpression s, string path)
    {
        var children = new List<ExpressionNode>
        {
            BuildNode(s.SwitchValue, $"{path}/0")
        };

        var index = 1;
        foreach (var c in s.Cases)
        {
            var caseChildren = new List<ExpressionNode>();
            var caseIndex = 0;

            foreach (var testValue in c.TestValues)
            {
                caseChildren.Add(BuildNode(testValue, $"{path}/{index}/{caseIndex++}"));
            }

            caseChildren.Add(BuildNode(c.Body, $"{path}/{index}/{caseIndex}"));

            children.Add(new ExpressionNode
            {
                Path = $"{path}/{index}",
                Kind = "SwitchCase",
                NodeType = "SwitchCase",
                Display = "case",
                TypeDisplay = "",
                Details =
                [
                    Detail("TestValuesCount", c.TestValues.Count.ToString())
                ],
                Children = caseChildren
            });
            index++;
        }

        if (s.DefaultBody != null)
        {
            children.Add(BuildNode(s.DefaultBody, $"{path}/{index}"));
        }

        return new ExpressionNode
        {
            Path = path,
            Kind = "Switch",
            NodeType = s.NodeType.ToString(),
            Display = "Switch",
            TypeDisplay = SimplifyType(s.Type),
            Details =
            [
                Detail("CasesCount", s.Cases.Count.ToString()),
                Detail("HasDefault", (s.DefaultBody != null).ToString())
            ],
            Children = children
        };
    }

    private static ExpressionNode BuildGoto(GotoExpression g, string path)
    {
        var children = new List<ExpressionNode>();
        if (g.Value != null)
        {
            children.Add(BuildNode(g.Value, $"{path}/0"));
        }

        return new ExpressionNode
        {
            Path = path,
            Kind = "Goto",
            NodeType = g.NodeType.ToString(),
            Display = $"{g.Kind} {g.Target.Name}",
            TypeDisplay = SimplifyType(g.Type),
            Details =
            [
                Detail("GotoKind", g.Kind.ToString()),
                Detail("TargetName", g.Target.Name)
            ],
            Children = children
        };
    }

    private static ExpressionNode BuildLabel(LabelExpression l, string path)
    {
        var children = new List<ExpressionNode>();
        if (l.DefaultValue != null)
        {
            children.Add(BuildNode(l.DefaultValue, $"{path}/0"));
        }

        return new ExpressionNode
        {
            Path = path,
            Kind = "Label",
            NodeType = l.NodeType.ToString(),
            Display = $"Label {l.Target.Name}",
            TypeDisplay = SimplifyType(l.Type),
            Details =
            [
                Detail("TargetName", l.Target.Name)
            ],
            Children = children
        };
    }

    private static ExpressionNode BuildLoop(LoopExpression l, string path)
    {
        return new ExpressionNode
        {
            Path = path,
            Kind = "Loop",
            NodeType = l.NodeType.ToString(),
            Display = "Loop",
            TypeDisplay = SimplifyType(l.Type),
            Details =
            [
                Detail("BreakLabel", l.BreakLabel?.Name ?? "-"),
                Detail("ContinueLabel", l.ContinueLabel?.Name ?? "-")
            ],
            Children =
            [
                BuildNode(l.Body, $"{path}/0")
            ]
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
