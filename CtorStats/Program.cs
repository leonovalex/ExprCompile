using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace CtorStats
{
    class Program
    {
        private const int Cycles = 100000;
        public static volatile CtorSource Value;
        private static readonly Expression<Func<int, double, CtorSource, CtorSource>> Expr;
        private static readonly Func<int, double, CtorSource, CtorSource> LambdaExpr;
        private static readonly Func<int, double, CtorSource, CtorSource> CompiledExpr;
        private static readonly Func<int, double, CtorSource, CtorSource> CompiledMethod;
        private static readonly ModuleBuilder ModuleBuilder;

        static Program()
        {
            LambdaExpr = (i, d, cl) => new CtorSource(i, d, cl);
            Expr = (i, d, cl) => new CtorSource(i, d, cl);
            CompiledExpr = Expr.Compile();

            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("MyAssembly_" + Guid.NewGuid().ToString("N")),
                AssemblyBuilderAccess.Run);
            ModuleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            var typeBuilder = ModuleBuilder.DefineType("MyType_" + Guid.NewGuid().ToString("N"),
                TypeAttributes.Public);
            var methodBuilder = typeBuilder.DefineMethod("MyMethod",
              MethodAttributes.Public | MethodAttributes.Static);
            Expr.CompileToMethod(methodBuilder);
            var resultingType = typeBuilder.CreateType();

            CompiledMethod = (Func<int, double, CtorSource, CtorSource>)Delegate.CreateDelegate(Expr.Type, resultingType.GetMethod("MyMethod"));
        }

        public enum Tests
        {
            Zero,
            Lambda,
            Compile,
            Compiled,
            Reflection,
            CompileMethod,
            CompiledMethod,
        }

        static void Main(string[] args)
        {
            var tests = new[]
            {
                new KeyValuePair<Tests,Func<CtorSource>>(Tests.Zero, ZeroLevel),
                new KeyValuePair<Tests,Func<CtorSource>>(Tests.Lambda, Lambda),
                new KeyValuePair<Tests,Func<CtorSource>>(Tests.Compiled, Compiled),
                new KeyValuePair<Tests,Func<CtorSource>>(Tests.CompiledMethod, CompiledMethodMethod),
                new KeyValuePair<Tests,Func<CtorSource>>(Tests.Reflection, Reflection),
                new KeyValuePair<Tests,Func<CtorSource>>(Tests.Compile, Compile),
                //new KeyValuePair<Tests,Func<CtorSource>>(Tests.CompileMethod, CompileMethodMethod),
            };

            foreach (var value in tests)
            {
                Test(value.Value, 1);
            }

            foreach (var test in tests)
            {
                var time = Test(test.Value, Cycles);
                Console.WriteLine("{0}: {1}", test.Key, time.TotalMilliseconds);
            }

            Console.ReadKey();
        }

        public static CtorSource ZeroLevel()
        {
            return new CtorSource(1, 1.0, null);
        }

        public static CtorSource Lambda()
        {
            return LambdaExpr(1, 1.0, null);
        }

        public static CtorSource Compile()
        {
            var func = Expr.Compile();
            return func(1, 1.0, null);
        }

        public static CtorSource Compiled()
        {
            return CompiledExpr(1, 1.0, null);
        }

        public static CtorSource CompiledMethodMethod()
        {
            return CompiledMethod(1, 1.0, null);
        }

        public static CtorSource CompileMethodMethod()
        {
            var typeBuilder = ModuleBuilder.DefineType("MyType_" + Guid.NewGuid().ToString("N"),
                TypeAttributes.Public);

            var methodBuilder = typeBuilder.DefineMethod("MyMethod",
              MethodAttributes.Public | MethodAttributes.Static);

            Expr.CompileToMethod(methodBuilder);

            var resultingType = typeBuilder.CreateType();

            var func = (Func<int, double, CtorSource, CtorSource>)Delegate.CreateDelegate(Expr.Type, resultingType.GetMethod("MyMethod"));
            return func(1, 1.0, null);
        }

        public static CtorSource Reflection()
        {
            return (CtorSource)Activator.CreateInstance(typeof(CtorSource), 1, 1.0, null);
        }

        public static TimeSpan Test(Func<CtorSource> func, int count)
        {
            var sw = new Stopwatch();
            sw.Start();
            for (var i = 0; i < count; i++)
            {
                Value = func();
            }
            sw.Stop();
            return sw.Elapsed;
        }
    }

    public sealed class CtorSource
    {
        public CtorSource(int i, double level, CtorSource parent)
        {
        }
    }
}
