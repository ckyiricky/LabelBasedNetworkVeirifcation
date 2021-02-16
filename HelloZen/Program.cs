using System;
using ZenLib;
using static ZenLib.Language;

namespace HelloZen
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Zen<int> Multi(Zen<int> x, Zen<int> y)
            {
                return 3 * x + y;
            }
            ZenFunction<int, int, int> function = Function<int, int, int>(Multi);
            function.Compile();
            var output = function.Evaluate(3, 2); // output = 11
            var input = function.Find((x, y, result) => And(x <= 0, result == 11));
            Console.WriteLine(input.ToString());
        }
    }
}