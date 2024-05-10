using DeadReckoned.Expressions.Plugins;

namespace DeadReckoned.Expressions.REPL
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Expression Engine REPL");
            Console.WriteLine("----------------------");
            Console.WriteLine();
            Console.Write("> ");

            ExpressionEngine engine = new(new ExpressionEngineConfig()
            {
                Plugins =
                [
                    new TrigonometryPlugin()
                ]
            });

            bool running = true;
            do
            {
                string? input = Console.ReadLine();
                EvaluateResult result = engine.Evaluate(input, null, throwOnFailure: false);

                if (result.Success)
                {
                    Console.WriteLine(result.Value);
                }
                else
                {
                    Console.WriteLine(result.Exception.Message);
                }

                Console.WriteLine();
                Console.Write("> ");
            }
            while (running);
        }
    }
}
