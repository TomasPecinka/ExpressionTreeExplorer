using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace ExpressionTreeExplorer.Core;

internal static class ExpressionHelpers
{
    internal static string SimplifyType(Type type)
    {
        // TODO: replace with dictionary lookup or smthng 

        if (type == typeof(int))
        {
            return "int";
        }

        if (type == typeof(bool))
        {
            return "bool";
        }

        if (type == typeof(string))
        {
            return "string";
        }

        if (type == typeof(double))
        {
            return "double";
        }

        if (type == typeof(float))
        {
            return "float";
        }

        if (type == typeof(long))
        {
            return "long";
        }

        if (type == typeof(short))
        {
            return "short";
        }

        if (type == typeof(byte))
        {
            return "byte";
        }

        if (type == typeof(char))
        {
            return "char";
        }

        if (type == typeof(decimal))
        {
            return "decimal";
        }

        if (type == typeof(object))
        {
            return "object";
        }

        if (type == typeof(void))
        {
            return "void";
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

    internal static string FormatConstant(ConstantExpression constant)
    {
        return FormatValue(constant.Value, constant.Type);
    }

    internal static string FormatValue(object value, Type fallbackType)
    {
        return value switch
        {
            null => "null",
            string s => $"\"{s}\"",
            char c => $"'{c}'",
            int or long or short or byte or float or double or decimal or bool => Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallbackType.Name,
            _ => $"<{value.GetType().Name}>",
        };
    }
}
