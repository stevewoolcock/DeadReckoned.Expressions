using DeadReckoned.Expressions.Plugins;
using System.Diagnostics;

namespace DeadReckoned.Expressions
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ExpressionEngine engine = new(new ExpressionEngineConfig()
            {
                //NumericMode = NumericMode.Decimal,
                Plugins = new IExpressionEnginePlugin[]
                {
                    new TrigonometryPlugin(),
                }
            });

            engine.Params["TAU"] = Math.Tau;

            engine.SetFunction("CUSTOM_FUNC", c =>
            {
                return 2;
            });

            engine.SetFunction("MUL", c =>
            {
                c.Args.EnsureCount(2);
                return c.Args[0].Number * c.Args[1].Number;
            });

            engine.SetFunction("MUL10", c =>
            {
                c.Args.EnsureCount(1);
                return c.Args[0].Number * 10;
            });

            ExpressionContext context = new();
            context.Params["FOO"] = 5;
            context.Params["BAR"] = 2;

            //string exprSource = "IF(MUL(1, 2) = 2, true, SUM(2, 2) = 4)";
            string exprSource = "12 + (56 / 0.5) * 5 + (100 * 0.25)";

            // Transient
            try
            {
                // Warm up
                for (int i = 0; i < 4; i++)
                {
                    Evaluate(engine, exprSource, context);
                }

                (Value result, TimeSpan evalTime) = Evaluate(engine, exprSource, context);

                Console.WriteLine($"Transient");
                Console.WriteLine($"---------");
                Console.WriteLine($"Compiled + evaluated in {evalTime.TotalMilliseconds:0.0000}ms");
                Console.WriteLine($"{result.Type} -> {result}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                return;
            }

            // Pre-compiled
            try
            {
                // Warm up
                for (int i = 0; i < 4; i++)
                {
                    (Expression e, _) = Compile(engine, exprSource);
                    Evaluate(engine, e, context);
                }

                (Expression expr, TimeSpan compTime) = Compile(engine, exprSource);
                (Value result, TimeSpan evalTime) = Evaluate(engine, expr, context);

                Console.WriteLine($"Pre-compiled");
                Console.WriteLine($"------------");
                Console.WriteLine($"Compiled in {compTime.TotalMilliseconds:0.0000}ms");
                Console.WriteLine($"Evaluated in {evalTime.TotalMilliseconds:0.0000}ms");
                Console.WriteLine($"{result.Type} -> {result}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                return;
            }
        }

        static (Expression expr, TimeSpan time) Compile(ExpressionEngine engine, string source)
        {
            var sw = Stopwatch.StartNew();

            Expression expr = engine.Compile(source);

            sw.Stop();
            return (expr, sw.Elapsed);
        }

        static (Value result, TimeSpan time) Evaluate(ExpressionEngine engine, Expression expr, ExpressionContext ctx)
        {
            var sw = Stopwatch.StartNew();

            Value result = engine.Evaluate(expr, ctx);

            sw.Stop();
            return (result, sw.Elapsed);
        }

        static (Value result, TimeSpan time) Evaluate(ExpressionEngine engine, string source, ExpressionContext ctx)
        {
            var sw = Stopwatch.StartNew();

            Value result = engine.Evaluate(source, ctx);

            sw.Stop();
            return (result, sw.Elapsed);
        }
    }
}
