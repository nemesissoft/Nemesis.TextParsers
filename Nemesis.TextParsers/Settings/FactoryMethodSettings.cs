using System;

namespace Nemesis.TextParsers.Settings
{
    public sealed record FactoryMethodSettings(string FactoryMethodName, string EmptyPropertyName, string NullPropertyName) : ISettings
    {
        public static FactoryMethodSettings Default { get; } = new("FromText", "Empty", "Null");

        public override string ToString() =>
            $"Parsed by {FactoryMethodName} Empty: {EmptyPropertyName} Null: {NullPropertyName}";

        public void Validate()
        {
            if (string.IsNullOrEmpty(FactoryMethodName))
                throw new ArgumentException("FactoryMethodName cannot be null or empty.", nameof(FactoryMethodName));
            if (string.IsNullOrEmpty(EmptyPropertyName))
                throw new ArgumentException("EmptyPropertyName cannot be null or empty.", nameof(EmptyPropertyName));
            if (string.IsNullOrEmpty(NullPropertyName))
                throw new ArgumentException("NullPropertyName cannot be null or empty.", nameof(NullPropertyName));
        }
    }
}