#nullable enable
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.Parsers;

public class TransformableHandler(ITransformerStore transformerStore) : ITransformerHandler
{
    public ITransformer<TTransformable> CreateTransformer<TTransformable>()
    {
        var transformable = typeof(TTransformable);
        var transformer = transformable.GetCustomAttribute<TransformerAttribute>()?.TransformerType;

        if (transformer == null ||
            !IsTransformerSupported(transformable, transformer)
           )
            throw new NotSupportedException($"{transformable.GetFriendlyName()} is not supported by {nameof(TransformableHandler)}");

        transformer = PrepareGenericTransformer(transformable, transformer);

        return (ITransformer<TTransformable>) CreateTransformer(transformer, transformerStore);
    }

    private static ITransformer CreateTransformer(Type type, ITransformerStore store)
    {
        var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (ctors.Length != 1)
            throw new NotSupportedException($"Only single constructor is supported for automatic transformer creation: {type.GetFriendlyName()}");

        var ctor = ctors[0];
        var @params = ctor.GetParameters();

        return (@params.Length == 0
                ? (ITransformer?) Activator.CreateInstance(type, true)
                : (ITransformer?) Activator.CreateInstance(type, @params.Select(p => GetArguments(p.ParameterType, store)).ToArray())
            ) ?? throw new NotSupportedException($"Type '{type.GetFriendlyName()}' cannot be created using reflection");


        static object GetArguments(Type parameterType, ITransformerStore store)
        {
            if (parameterType == typeof(ITransformerStore))
                return store;

            else if (typeof(ISettings).IsAssignableFrom(parameterType))
                return store.SettingsStore.GetSettingsFor(parameterType);

            else if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(NumberTransformer<>))
            {
                var elementType = parameterType.GenericTypeArguments[0];
                return store.GetTransformer(elementType);
            }

            else if (parameterType.DerivesOrImplementsGeneric(typeof(ITransformer<>)))
            {
                var realization = TypeMeta.GetGenericRealization(parameterType, typeof(ITransformer<>))
                                  ?? throw new NotSupportedException($"Generic realization for '{parameterType.GetFriendlyName()}' as {typeof(ITransformer<>)} cannot be obtained");
                var elementType = realization.GenericTypeArguments[0];

                return store.GetTransformer(elementType);
            }

            else
                throw new NotSupportedException(
                    $"Only supported parameters for auto-injection to transformers marked with {nameof(TransformerAttribute)} are {nameof(ITransformerStore)}, implementations of {nameof(ISettings)}, generic {nameof(NumberTransformer<int>)} or generic {nameof(ITransformer)} implementations");
        }
    }

    private static Type PrepareGenericTransformer(in Type transformable, in Type transformer)
    {
        if (transformer.IsGenericTypeDefinition)
            return transformable.IsGenericType
                ? transformer.MakeGenericType(transformable.GenericTypeArguments)
                : transformer.MakeGenericType(transformable);

        return transformer;
    }


    public bool CanHandle(Type transformable) =>
        transformable.GetCustomAttribute<TransformerAttribute>()?.TransformerType switch
        {
            null => false,
            { } transformer when !IsTransformerSupported(transformable, transformer) =>
                throw new NotSupportedException(
                    $"Transformer registered via {nameof(TransformerAttribute)}.{nameof(TransformerAttribute.TransformerType)} has to implement {nameof(ITransformer<int>)}<>"
                ),
            _ => true
        };

    private static bool IsTransformerSupported(in Type transformable, in Type transformer)
    {
        if (!transformer.DerivesOrImplementsGeneric(typeof(ITransformer<>)))
            return false;
        else
        {
            return TypeMeta.TryGetGenericRealization(
                       PrepareGenericTransformer(transformable, transformer),
                       typeof(ITransformer<>), out var realization
                   ) &&
                   realization?.GenericTypeArguments is [{ } transformedElement] &&
                   transformable.DerivesOrImplementsGeneric(transformedElement)
                ;
        }
    }

    public sbyte Priority => 15;

    public override string ToString() =>
        $"Create transformer based on {nameof(TransformerAttribute)}.{nameof(TransformerAttribute.TransformerType)}";

    string ITransformerHandler.DescribeHandlerMatch() => $"Type decorated with {nameof(TransformerAttribute)} pointing to valid transformer";
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum, Inherited = true, AllowMultiple = false)]
public sealed class TransformerAttribute(Type transformerType) : Attribute
{
    public Type TransformerType { get; } = transformerType ?? throw new ArgumentNullException(nameof(transformerType));
}