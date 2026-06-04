using System.Linq.Expressions;

using ExpressionTreeExplorer.Core;

namespace ExpressionTreeExplorer.SampleApp;

internal class Program
{
    static void Main(string[] args)
    {
        // simple predicate (binary, member, constant, parameter)
        Expression<Func<User, bool>> predicate =
            x => x.Age > 20 && x.Age < 30;

        // conditional (ternary)
        Expression<Func<User, string>> conditional =
            x => x.Age >= 18 ? x.Name : "minor";

        // method call + string operations
        Expression<Func<User, bool>> methodCall =
            x => x.Name.StartsWith("A") && x.Name.Length > 3;

        // object initialization (,emberInit + new + bindings)
        var p = Expression.Parameter(typeof(User), "x");
        var memberInit = Expression.Lambda<Func<User, UserDto>>(
            Expression.MemberInit(
                Expression.New(typeof(UserDto)),
                Expression.Bind(typeof(UserDto).GetProperty(nameof(UserDto.FullName))!,
                    Expression.Property(p, nameof(User.Name))),
                Expression.Bind(typeof(UserDto).GetProperty(nameof(UserDto.Age))!,
                    Expression.Property(p, nameof(User.Age)))),
            p);

        // newArrayInit
        var p2 = Expression.Parameter(typeof(User), "x");
        var newArray = Expression.Lambda<Func<User, int[]>>(
            Expression.NewArrayInit(typeof(int),
                Expression.Property(p2, nameof(User.Age)),
                Expression.Constant(100),
                Expression.Add(Expression.Property(p2, nameof(User.Age)), Expression.Constant(10))),
            p2);

        // typeBinary (is check)
        var objParam = Expression.Parameter(typeof(object), "obj");
        var typeCheck = Expression.Lambda<Func<object, bool>>(
            Expression.TypeIs(objParam, typeof(User)),
            objParam);

        // block + variable + assign + loop + goto + label
        var i = Expression.Variable(typeof(int), "i");
        var sum = Expression.Variable(typeof(int), "sum");
        var breakLabel = Expression.Label(typeof(int), "result");
        var loopBody = Expression.Block(
            Expression.IfThen(
                Expression.GreaterThanOrEqual(i, Expression.Property(p, nameof(User.Age))),
                Expression.Break(breakLabel, sum)),
            Expression.AddAssign(sum, i),
            Expression.PostIncrementAssign(i));
        var loop = Expression.Loop(loopBody, breakLabel);
        var block = Expression.Block(
            new[] { i, sum },
            Expression.Assign(i, Expression.Constant(0)),
            Expression.Assign(sum, Expression.Constant(0)),
            loop);
        var blockLambda = Expression.Lambda<Func<User, int>>(block,
            Expression.Parameter(typeof(User), "x"));

        // try catch finally
        var exParam = Expression.Parameter(typeof(Exception), "ex");
        var tryCatch = Expression.Lambda<Func<User, string>>(
            Expression.TryCatchFinally(
                Expression.Property(p, nameof(User.Name)),
                Expression.Empty(),
                Expression.Catch(exParam,
                    Expression.Property(exParam, nameof(Exception.Message)))),
            p);

        // switch
        var p3 = Expression.Parameter(typeof(User), "x");
        var switchExpr = Expression.Lambda<Func<User, string>>(
            Expression.Switch(
                Expression.Property(p3, nameof(User.Age)),
                Expression.Constant("other"),
                Expression.SwitchCase(Expression.Constant("child"), Expression.Constant(10)),
                Expression.SwitchCase(Expression.Constant("teen"), Expression.Constant(15)),
                Expression.SwitchCase(Expression.Constant("adult"), Expression.Constant(18)),
                Expression.SwitchCase(Expression.Constant("senior"), Expression.Constant(65))),
            p3);

        // ListInit
        var listInit = Expression.Lambda<Func<User, List<int>>>(
            Expression.ListInit(
                Expression.New(typeof(List<int>)),
                Expression.Constant(1),
                Expression.Constant(2),
                Expression.Constant(3)),
            Expression.Parameter(typeof(User), "x"));

        // index (dict access)
        var dictParam = Expression.Parameter(typeof(Dictionary<string, int>), "dict");
        var indexExpr = Expression.Lambda<Func<Dictionary<string, int>, int>>(
            Expression.MakeIndex(dictParam,
                typeof(Dictionary<string, int>).GetProperty("Item")!,
                new[] { Expression.Constant("key") }),
            dictParam);

        // invocation (calling delegate)
        var funcParam = Expression.Parameter(typeof(Func<int, bool>), "f");
        var invocation = Expression.Lambda<Func<Func<int, bool>, bool>>(
            Expression.Invoke(funcParam, Expression.Constant(42)),
            funcParam);

        // default
        var p4 = Expression.Parameter(typeof(User), "x");
        var withDefault = Expression.Lambda<Func<User, int>>(
            Expression.Condition(
                Expression.Equal(Expression.Property(p4, nameof(User.Name)), Expression.Constant(null, typeof(string))),
                Expression.Default(typeof(int)),
                Expression.Property(p4, nameof(User.Age))),
            p4);

        // --- print all expressions ---
        var expressions = new (string Name, Expression Expr)[]
        {
            ("Predicate", predicate),
            ("Conditional", conditional),
            ("MethodCall", methodCall),
            ("MemberInit", memberInit),
            ("NewArray", newArray),
            ("TypeCheck", typeCheck),
            ("Block+Loop+Goto+Label", blockLambda),
            ("TryCatchFinally", tryCatch),
            ("Switch", switchExpr),
            ("ListInit", listInit),
            ("Index", indexExpr),
            ("Invocation", invocation),
            ("Default", withDefault),
        };

        foreach (var (name, expr) in expressions)
        {
            Console.WriteLine($"\n=== {name} ===");
            Console.WriteLine(expr);

            var payload = ExpressionNodeBuilder.Build(expr);
            Print(payload.Roots[0], 0);
        }

        // Set a breakpoint here to inspect expressions in the debugger
        Console.WriteLine("\nDone. Set a breakpoint here to inspect expressions.");
        Console.ReadLine();

        static void Print(ExpressionNode node, int indent)
        {
            var details = node.Details.Count > 0
                ? $" [{string.Join(", ", node.Details.Select(d => $"{d.Name}={d.Value}"))}]"
                : "";
            Console.WriteLine($"{new string(' ', indent * 2)}[{node.Kind}] {node.Display} ({node.TypeDisplay}){details}");

            foreach (var child in node.Children)
            {
                Print(child, indent + 1);
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
}
