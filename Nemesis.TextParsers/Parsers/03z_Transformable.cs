using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers.Parsers;

[UsedImplicitly]
public class TransformableCreator : ICanCreateTransformer
{
    private readonly ITransformerStore _transformerStore;
    public TransformableCreator(ITransformerStore transformerStore) => _transformerStore = transformerStore;

    public ITransformer<TTransformable> CreateTransformer<TTransformable>()
    {
        var transformable = typeof(TTransformable);
        var transformer = transformable.GetCustomAttribute<TransformerAttribute>()?.TransformerType;

        if (transformer == null ||
            !IsTransformerSupported(transformable, transformer)
        )
            throw new NotSupportedException($"{transformable.GetFriendlyName()} is not supported by {nameof(TransformableCreator)}");

        transformer = PrepareGenericTransformer(transformable, transformer);

        return (ITransformer<TTransformable>)CreateTransformer(transformer, _transformerStore);
    }

    private static ITransformer CreateTransformer(Type type, ITransformerStore store)
    {
        var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (ctors.Length != 1)
            throw new NotSupportedException($"Only single constructor is supported for automatic transformer creation: {type.GetFriendlyName()}");

        var ctor = ctors[0];
        var @params = ctor.GetParameters();

        return @params.Length switch
        {
            0 => (ITransformer)Activator.CreateInstance(type, true),
            1 => @params[0].ParameterType == typeof(ITransformerStore)
                ? (ITransformer)Activator.CreateInstance(type, store)
                : throw new NotSupportedException(
                    $"Among single parameter constructors only the one that takes {nameof(ITransformerStore)} is supported"),
            _ => throw new NotSupportedException("Only constructors of arity 0..1 are supported")
        };
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
            var preparedTransformer = PrepareGenericTransformer(transformable, transformer);

            return TypeMeta
                       .TryGetGenericRealization(preparedTransformer, typeof(ITransformer<>), out var realization) &&
                   realization.GenericTypeArguments is { } genArgs && genArgs.Length == 1 &&
                   genArgs[0] is { } transformedElement &&
                   transformable.DerivesOrImplementsGeneric(transformedElement)
                ;
        }
    }

    public sbyte Priority => 15;

    public override string ToString() =>
        $"Create transformer based on {nameof(TransformerAttribute)}.{nameof(TransformerAttribute.TransformerType)}";
}

// ReSharper disable RedundantAttributeUsageProperty
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
// ReSharper restore RedundantAttributeUsageProperty
[PublicAPI]
public sealed class TransformerAttribute : Attribute
{
    public Type TransformerType { get; }

    public TransformerAttribute([NotNull] Type transformerType) =>
        TransformerType = transformerType ?? throw new ArgumentNullException(nameof(transformerType));
}
