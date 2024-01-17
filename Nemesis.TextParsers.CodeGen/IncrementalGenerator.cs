using Nemesis.TextParsers.CodeGen.Utils;

namespace Nemesis.TextParsers.CodeGen;
public abstract class IncrementalGenerator : IIncrementalGenerator
{
    protected const string INPUTS = "Inputs";

    protected abstract string GetAttributeName();

    public abstract void Initialize(IncrementalGeneratorInitializationContext context);

    public GeneratorDriver CreateGeneratorDriver(Compilation compilation) =>
        CSharpGeneratorDriver.Create(
            generators: new ISourceGenerator[] { this.AsSourceGenerator() },
            parseOptions: (CSharpParseOptions)compilation.SyntaxTrees.First().Options,
            driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: true));

    public GeneratorRunResult RunIncrementalGenerator(Compilation compilation)
    {
        GeneratorDriver driver = CreateGeneratorDriver(compilation);

        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult().Results.Single();
    }

    public IReadOnlyList<string> RunIncrementalGeneratorAndGetGeneratedSources(Compilation compilation, int requiredCardinality = 1)
    {
        var result = RunIncrementalGenerator(compilation);
        return GetGeneratedOutput(result, requiredCardinality);
    }

    public (IReadOnlyList<string> Sources, IReadOnlyList<TInput> Inputs) RunIncrementalGeneratorAndCaptureInputs<TInput>(Compilation compilation, int requiredCardinality = 1)
    {
        var result = RunIncrementalGenerator(compilation);
        var generatedSources = GetGeneratedOutput(result, requiredCardinality);

        if (result.TrackedSteps.TryGetValue(INPUTS, out var metaValue))
        {
            var stepResults = metaValue.Single().Outputs
                .Select(o => (
                    Result: (Result<TInput, Diagnostic>)o.Value,
                    o.Reason
                ))
                .ToList();


            if (stepResults.Any(r => r.Reason != IncrementalStepRunReason.New))
                throw new NotSupportedException($"All generation steps are expected to be new");

            if (stepResults.Any(r => !r.Result.IsSuccess))
                throw new NotSupportedException($"All generation steps are expected to be succesful");

            var meta = new List<TInput>(stepResults.Count);
            stepResults.ForEach(r => r.Result.Invoke(meta.Add));

            return (generatedSources, meta);
        }

        return (generatedSources, []);
    }

    private IReadOnlyList<string> GetGeneratedOutput(GeneratorRunResult result, int requiredCardinality)
    {
        string attributeNameToRemove = GetAttributeName();

        if (result.Diagnostics.Length > 0)
            throw new NotSupportedException($"Expected no diagnostics but was {result.Diagnostics.Length}");

        if (result.Exception is not null)
            throw new NotSupportedException($"Not expected exception: {result.Exception}");

        if (result.GeneratedSources.Length < 1)
            throw new NotSupportedException("No generated sources");

        var generatedSources = result.GeneratedSources
            .Where(gen => !gen.HintName.Equals($"{attributeNameToRemove}.g.cs"))
            .Select(gen => gen.SourceText.ToString())
            .ToList();

        if (generatedSources.Count != requiredCardinality)
            throw new NotSupportedException($"Expected cardinality of generated sources is {requiredCardinality} but was {generatedSources.Count}");

        return generatedSources;
    }
}
