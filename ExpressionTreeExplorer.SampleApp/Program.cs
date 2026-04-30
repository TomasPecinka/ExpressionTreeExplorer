using System.Linq.Expressions;

using ExpressionTreeExplorer.Core;

namespace ExpressionTreeExplorer.SampleApp;

internal class Program
{
    static void Main(string[] args)
    {
        Expression<Func<User, bool>> expr =
            x => x.Age > 20 && x.Age < 30;

        Console.WriteLine(expr);
        //Console.ReadLine();

        var payload = ExpressionNodeBuilder.Build(expr);

        Print(payload.Roots[0], 0);

        static void Print(ExpressionNode node, int indent)
        {
            Console.WriteLine($"{new string(' ', indent * 2)}{node.Display} ({node.TypeDisplay})");

            foreach (var child in node.Children)
            {
                Print(child, indent + 1);
            }
        }
    }

    private class User
    {
        public int Age { get; set; }
    }
}
