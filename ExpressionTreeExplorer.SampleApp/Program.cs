using System.Linq.Expressions;

namespace ExpressionTreeExplorer.SampleApp;

internal class Program
{
    static void Main(string[] args)
    {
        Expression<Func<User, bool>> expr =
            x => x.Age > 20 && x.Age < 30;

        Console.WriteLine(expr);
        Console.ReadLine();
    }

    private class User
    {
        public int Age { get; set; }
    }
}
