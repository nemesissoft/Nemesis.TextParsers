using System;
using System.Reflection;

namespace Nemesis.TextParsers.Utils
{
    internal static class ReflectionUtils
    {
        public static object GetInstanceOrCreate(Type type, Type returnType)
        {
            const BindingFlags PUB_STAT_FLAGS = BindingFlags.Public | BindingFlags.Static;
            
            if (type.GetProperty("Instance", PUB_STAT_FLAGS) is { } singletonProperty &&
                singletonProperty.GetMethod != null &&
                returnType.IsAssignableFrom(singletonProperty.PropertyType)
            )
                return singletonProperty.GetValue(null);

            else if (type.GetField("Instance", PUB_STAT_FLAGS) is { } singletonField &&
                     returnType.IsAssignableFrom(singletonField.FieldType)
            )
                return singletonField.GetValue(null);

            else
                return Activator.CreateInstance(type, false);
        }
    }
}
