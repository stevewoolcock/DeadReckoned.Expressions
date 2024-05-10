# DeadReckoned.Expressions
Expressions is a fast, lightweight expression evaluation engine for C#.

- Extremely fast and simple expression evaluation engine using C# strings or `ReadOnlyMemory<char>`.
- Supports pre-compiling expressions for re-use.
- Supports parameterized expressions.
- Supports functions, as well as defining custom functions.
- GC friendly: evaluting pre-compiled expressions allocates no memory for general use.
- Optimized for use with `ReadOnlyMemory<>` and `ReadOnlySpan<>` wherever possible.

The expression engine comprises of three major components:
- The parser, which tokenizes your expression string;
- The compiler, which generates optimized bytecode from the tokens;
- And the evaluator, which executes the bytecode and generates a return value.

Executing an expression couldn't be simpler. Here's a "Hello World" example:
```csharp
ExpressionEngine engine = new();
Value output = engine.Evaluate("10 + 20 / 2.5 * 360");

// Output: 2890
Console.WriteLine(output);

// Parenthesis are supported, of course...
output = engine.Evaluate("(5 * (2 - 4)) / 5");

// Output: -2
Console.WriteLine(output);
```

# Table of Contents
1. [Syntax](#syntax)
    1. [Operators](#syntax_operators)
    2. [Grouping](#syntax_grouping)
    3. [Functions](#syntax_functions)
    4. [Parameters](#syntax_parameters)
2. [Values](#values)
3. [Pre-Compiling](#pre-compiling)
4. [Parameters](#parameters)
5. [Functions](#functions)
    1. [Built-in Functions](#functions_builtin)
    1. [User Functions](#functions_user)
6. [Output](#output)
    1. [Handling Failure](#output_errors)
7. [REPL](#repl)
8. [Unity](#unity)
    1. [Install via Git URL](#unity_giturl)

## Syntax <a name="syntax"></a>
A short overview of the syntax and supported operators.

#### Operators <a name="syntax_operators"></a>
```
Binary: expression operator expression
Unary: operator expression
```
| Arithmetic ||
|------------|------------
| `x + x`    | Binary numeric addition
| `x - x`    | Binary numeric subtraction
| `x * x`    | Binary numeric multiplication
| `x / x`    | Binary numeric division
| `x % x`    | Binary numeric remainder
| `-x`       | Unary numeric negation

| Logic      ||
|------------|------------
| `!x`       | Unary logical negation
| `x & x`    | Binary logical AND
| `x \| x`   | Binary logical OR
| `x ^ x`    | Binary logical exclusive OR (XOR)

| Equality   ||
|------------|------------
| `x = x`    | Equality
| `x != x`   | Inequality

| Comparison ||
|------------|------------
| `x < x`        | Less than
| `x > x`        | Greater than
| `x <= x`       | Less than or equal
| `x >= x`       | Greater than or equal

#### Grouping <a name="syntax_grouping"></a>
```
( expression1 ( expression2 ) )
```
- Expressions can be grouped using parenthesis.
- Grouping controls the order-of-operations.
- Groups can be nested.

#### Functions <a name="syntax_functions"></a>
```
FUNCTION(expression, expression, ...expression)
```
- Functions are invoked using the call operator: `(...)`.
- Functions support up to 255 arguments.
- Arguments are separated by the `,` (comma) character.
- Function names must be known at compile time, so therefore must be registered with the engine before compiling an expression. It is an error to use a function that has not been defined.

#### Parameters <a name="syntax_parameters"></a>
```
$PARAMETER
```
- Paremeters are expanded at evaluation time.
- Parameters in the `ExpressionContext` shadow those in the `ExpressionEngine`, ie. parameters in the context with the same name as those in the engine take precedence.



## Values <a name="values"></a>
Expressions supports several diffent value types:
|||
|------------|------------
| `NULL`     | Represents no value. 
| `BOOL`     | Boolean type, either `TRUE` or `FALSE`.<br>Can be coerced as `FALSE` from `NULL` or a numeric value of zero, or `TRUE` from any non-null or non-zero numeric value.
| `INTEGER`  | Equivalent of C# `long`. An integer value. A whole number, with no decimal or fractional parts. 
| `DECIMAL`  | Equivalent to C# `double`. A decimal value, consisting of a whole and fractional part. Can be `NaN`.


## Pre-compiling <a name="pre-compiling"></a>
It's often useful to pre-compile expressions for repeated use. As the compilation is the slowest part of the process (it's still very fast), it makes a lot of sense to compile once and store the expression for later use.

To pre-compile an expression, simply use the `Compile()` method:
```csharp
ExpressionEngine engine = new();

// Compile the expression
Expression expression = engine.Compile("1 + 1");

// Evaluate it later...
Value output = engine.Evaluate(expression);
```

## Parameters <a name="parameters"></a>
Expressions can be parameterized, so their inputs can be supplied at evaluation. Parameters are looked up at evaluation time by name, so the compiler does not need to be aware of them when pre-compiling your expressions.

Expressions supports two levels of parameters: Global and Context
- Global parameters are stored on the engine itself, and are available to all expressions evaluated by the engine.
- Context parameters are stored in an `ExpressionContext`, which is passed to the `Evaluate()` method.

Context parameters take precedence over Global parameters when they share a name.

Parameters within an expression are denoted by the `$` token. See the following example:
```csharp
ExpressionEngine engine = new();

// Global parameters
// These are available to all expressions evaulated with this engine
engine.Params["FOO"] = 1234;

// Context parameters
// The context is passed to the Evaluate() function, and can contain its own parameters
// that take precedence over global parameters.
ExpressionContext context = new();
context.Params["BAR"] = 5678;

Value output = engine.Evaluate("$FOO + $BAR", context);

// Output: 6912
Console.WriteLine(output);
```

## Functions <a name="functions"></a>
Expresions comes with a library of useful functions, as well as the ability to define user functions.

### Built-in Functions <a name="functions_builtin"></a>
The following functions are defined by the engine when constructed.

| Types            ||
|------------------|------------
| **Type coercion**  |
| `BOOL(x)`        | Converts `x` to a boolean type
| `INTEGER(x)`     | Converts `x` to an integer type
| `DECIMAL(x)`     | Converts `x` to a decimal type
| **Type checking**  |
| `IS_BOOL(x)`     | Returns `TRUE` if `x` is a `BOOL` type
| `IS_INTEGER(x)`  | Returns `TRUE` if `x` is an `INTEGER` type
| `IS_DECIMAL(x)`  | Returns `TRUE` if `x` is a `DECIMAL` type
| `IS_NAN(x)`      | Returns `TRUE` if `x` is `NaN` (Not a Number)
| `IS_NULL(x)`     | Returns `TRUE` if `x` is `NULL`
| `IS_NUMBER(x)`   | Returns `TRUE` if `x` is an `INTEGER` or `DECIMAL` type

| Logic            ||
|------------------|------------
| `AND(x, y)`      | Returns `TRUE` if `x` and `y` are truthy
| `IF(cond, x, y)` | Returns `x` if `cond` is truthy, otherwise returns `y`
| `OR(x, y)`       | Returns `TRUE` if `x` or `y` is truthy
| `XOR(x, y)`      | Returns `1` if `x` is equal to `y`, otherwise returns `0`

| Math             ||
|------------------|------------
| `E()`            | Returns the natural logarithmic base `e` (`2.7182818284590451`)
| `ABS(x)`         | Returns the absolute value of `x`
| `CEIL(x)`        | Returns the smallest integral value that is greater than or equal to `x`
| `CLAMP(x, min, max)` | Returns value clamped to the inclusive range of min and max
| `EXP(x)`         | Returns e raised to the power of `x`
| `FLOOR(x)`       | Returns the smallest integral value that is less than or equal to `x`
| `LOG(x)`         | Returns the natural (base e) logarithm of `x`
| `LOG10(x)`       | Returns the base 10 logarithm of `x`
| `LOG2(x)`        | Returns the base 2 logarithm of `x`
| `MAX(x, y, ...)` | Returns the largest value of all arguments
| `MIN(x, y, ...)` | Returns the smallest value of all arguments
| `POW(x, y)`      | Returns `x` raised to the power of `y`
| `ROUND(x)`       | Rounds `x` to the nearest integer
| `SIGN(x)`        | Returns 1 if `x` is positive, `-1` if `x` is negative, or `0` if `x = 0`
| `SQRT(x)`        | Returns the square root of `x`
| `SUM(x, y, ...)` | Returns the sum of all arguments
| `TRUNC(x)`       | Returns the integral part of `x`

### User Functions <a name="functions_user"></a>
User functions can be defined on the engine. They are regular C# methods or delegates, and accept a single `FunctionCall` value, which contains all contextual information about the function being invoked as well as the arguments passed to it.
```csharp
ExpressionEngine engine = new();

// Custom function to multiply an argument by 10
engine.SetFunction("MUL10", call =>
{
    // Expect exactly 1 argument
    call.Args.EnsureArgCount(1);

    // Fetch the first argument and multiply it by 10
    return call.Args[0].Number * 10;
});

// Execute it!
Expression expression = engine.Compile("MUL10(2)");
Value output = engine.Evaluate(expression, context);

// Output: 20
Console.WriteLine(output);
```



## Output <a name="output"></a>
More detailed output can be retrieved when both compiling and evaluating.

### Handling Failure <a name="output_errors"></a>
By default, both `Compile()` and `Evaluate()` will throw exceptions if they fail.
- `Compile()` throws `ExpressionCompileException`
    - The compiler exception message contain detailed error information, including the column number and a visualisation.
- `Evaluate()` throws `ExpressionRuntimeException`
    - These are far less common and generally only occur if a parameter is not found.

It may be desirable to avoid throwing these exceptions and handling failure in other ways. By passing `throwOnFailure: false` as an argument to either method, the exceptions are generated but not thrown.

```csharp
ExpressionEngine engine = new();

// Compile() returns a result value which contains the exception that was
// generated if compilation of the expression fails.

// This expression will fail due to an undefined function being invoked, and a malformed binary operation.
CompileResult compileResult = engine.Compile("NOT_DEFINED(10 + $UNDEF) *", throwOnFailure: false);
if (!compileResult.IsSuccess)
{
    Console.WriteLine($"Compilation failed: {compileResult.Exception.Message}");
    return;
}

// Evaluate() returns a result value which contains the exception that was
// generated if evaluation of the expression fails.

// Try evaluating an expression with a missing parameter
EvaluateResult evalResult = engine.Evaluate(compileResult.Expression, throwOnFailure: false);
if (!evalResult.IsSuccess)
{
    Console.WriteLine($"Evaluation failed: {evalResult.Exception.Message}");
    return;
}

Value output = evalResult.Value;

// Output: 2
Console.WriteLine(output);
```

## REPL <a name="output_errors"></a>
The repository includes a basic REPL application that can be used to test the output of the system. Simply set `DeadReckoned.Expressions.REPL` as your startup project in Visual Studio and run it. Type expressions into the console application and hit enter to see the output.


## Unity <a name="unity"></a>
Expressions is fully compatible with the latest versions of Unity. You can compile the `DeadReckoned.Expressions` library yourself and copy it directly into your project, or you can install the pre-built UPM package.

Some basic unit tests are included in the Unity package.

### Install via Git URL <a name="unity_giturl"></a>
Use the following URL to install the Expressions library in UPM:

`https://github.com/stevewoolcock/DeadReckoned.Expressions.git/?path=Expressions.Unity/Packages/com.deadreckoned.expressions`