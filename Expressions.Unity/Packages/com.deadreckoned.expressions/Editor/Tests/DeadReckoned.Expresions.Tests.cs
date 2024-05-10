using DeadReckoned.Expressions;
using NUnit.Framework;

namespace DeadReckoned.Expresions
{
    public class ExpresionsTests
    {
        [Test]
        public void SimpleExpression()
        {
            ExpressionEngine engine = new();
            Expression expr = engine.Compile("((10.0 * 5) / 2.0) + 5");
            Value result = engine.Evaluate(expr);

            Assert.IsTrue(result.IsNumber);
            Assert.AreEqual(30.0, result.Number);
        }

        [Test]
        public void ComplexExpression()
        {
            ExpressionEngine engine = new();
            engine.SetFunction("MUL", c =>
            {
                c.Args.EnsureCount(2);
                return c.Args[0].Number * c.Args[1].Number;
            });

            Expression expr = engine.Compile("(MUL(10, 2.5) = 50) | IF(MUL(1, 2) = 2, TRUE, SUM(2, 2) = 4)");
            Value result = engine.Evaluate(expr);

            Assert.IsTrue(result.IsBool);
            Assert.AreEqual(true, result.Bool);
        }

        [Test]
        public void CustomFunction()
        {
            ExpressionEngine engine = new();
            engine.SetFunction("MUL10", c =>
            {
                c.Args.EnsureCount(1);
                return c.Args[0].Number * 10.0;
            });

            Expression expr = engine.Compile("MUL10(20)");
            Value result = engine.Evaluate(expr);

            Assert.IsTrue(result.IsNumber);
            Assert.AreEqual(20 * 10, result.Number);
        }

        [Test]
        public void Parameters()
        {
            ExpressionEngine engine = new();

            Expression expr = engine.Compile("$FOO + $BAR = 20");
            ExpressionContext ctx = new();
            ctx.Params["FOO"] = 12;
            ctx.Params["BAR"] = 8;

            Value result = engine.Evaluate(expr, ctx);

            Assert.IsTrue(result.IsBool);
            Assert.AreEqual(true, result.Bool);
        }
    }
}