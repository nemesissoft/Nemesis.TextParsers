using System;
using System.Linq;
using System.Reflection.Emit;

namespace Benchmarks
{
    public static class DynamicMethodGenerator
    {
        public static DynamicMethodGenerator<TDelegate> Create<TDelegate>(string name)
            where TDelegate : Delegate
            => new DynamicMethodGenerator<TDelegate>(name);
    }

    public sealed class DynamicMethodGenerator<TDelegate> where TDelegate : Delegate
    {
        private readonly DynamicMethod _method;

        internal DynamicMethodGenerator(string name)
        {
            var invokeMethod = typeof(TDelegate).GetMethod(nameof(Action.Invoke));

            var parameterTypes = invokeMethod.GetParameters().Select(p => p.ParameterType).ToArray();

            _method = new DynamicMethod(name, invokeMethod.ReturnType,
                parameterTypes, restrictedSkipVisibility: true);
        }

        public ILGenerator GetMsilGenerator() => _method.GetILGenerator();

        public TDelegate Generate() => (TDelegate)_method.CreateDelegate(typeof(TDelegate));
    }
}
