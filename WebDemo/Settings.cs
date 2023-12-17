namespace WebDemo;

sealed class ParsingSettings
{
    public CollectionSettings CollectionSettings { get; init; } = CollectionSettings.Default;
    public ArraySettings ArraySettings { get; init; } = ArraySettings.Default;
    public DictionarySettings DictionarySettings { get; init; } = DictionarySettings.Default;

    public EnumSettings EnumSettings { get; init; } = EnumSettings.Default;

    public FactoryMethodSettings FactoryMethodSettings { get; init; } = FactoryMethodSettings.Default;

    public ValueTupleSettings ValueTupleSettings { get; init; } = ValueTupleSettings.Default;
    public KeyValuePairSettings KeyValuePairSettings { get; init; } = KeyValuePairSettings.Default;
    public DeconstructableSettings DeconstructableSettings { get; init; } = DeconstructableSettings.Default;

    public ITransformerStore ToTransformerStore()
    {
        var settingsStore = SettingsStoreBuilder.GetDefault()
            .AddOrUpdate(CollectionSettings)
            .AddOrUpdate(ArraySettings)
            .AddOrUpdate(DictionarySettings)
            .AddOrUpdate(EnumSettings)
            .AddOrUpdate(FactoryMethodSettings)
            .AddOrUpdate(ValueTupleSettings)
            .AddOrUpdate(KeyValuePairSettings)
            .AddOrUpdate(DeconstructableSettings)
            .Build();

        return TextTransformer.GetDefaultStoreWith(settingsStore);
    }
}