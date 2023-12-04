namespace Nemesis.TextParsers.Settings;

public sealed class FactoryMethodSettings(string factoryMethodName, string emptyPropertyName, string nullPropertyName) : ISettings
{
    public string FactoryMethodName { get; private set; } = factoryMethodName;
    public string EmptyPropertyName { get; private set; } = emptyPropertyName;
    public string NullPropertyName { get; private set; } = nullPropertyName;

    public static FactoryMethodSettings Default { get; } = new("FromText", "Empty", "Null");

    public override string ToString() =>
        $"Parsed by {FactoryMethodName} Empty: {EmptyPropertyName} Null: {NullPropertyName}";
}