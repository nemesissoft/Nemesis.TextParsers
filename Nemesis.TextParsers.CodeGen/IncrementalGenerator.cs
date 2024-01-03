using Nemesis.TextParsers.CodeGen.Utils;

namespace Nemesis.TextParsers.CodeGen;
public abstract class IncrementalGenerator : IIncrementalGenerator
{
    protected const string INPUTS = "Inputs";

    protected abstract string GetAttributeName();

    public abstract void Initialize(IncrementalGeneratorInitializationContext context);

    public GeneratorRunResult RunIncrementalGenerator(Compilation compilation)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new ISourceGenerator[] { this.AsSourceGenerator() },
            parseOptions: (CSharpParseOptions)compilation.SyntaxTrees.First().Options,
            driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: true));

        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult().Results.Single();
    }

    public IReadOnlyList<string> RunIncrementalGeneratorAndGetGeneratedSources(Compilation compilation, int requiredCardinality = 1)
    {
        var result = RunIncrementalGenerator(compilation);
        return GetGeneratedOutput(result, requiredCardinality);
    }

    public (IReadOnlyList<string> Sources, IReadOnlyList<TMeta> Meta) RunIncrementalGeneratorAndCaptureInputs<TMeta>(Compilation compilation, int requiredCardinality = 1)
          where TMeta : struct
    {
        var result = RunIncrementalGenerator(compilation);
        var generatedSources = GetGeneratedOutput(result, requiredCardinality);

        IReadOnlyList<TMeta> meta = [];
        if (result.TrackedSteps.TryGetValue(INPUTS, out var metaValue))
        {
            var stepResults = metaValue.Single().Outputs
                .Select(o => (
                    Result: (Result<TMeta?, Diagnostic>)o.Value,
                    o.Reason
                ))
                .ToList();


            if (stepResults.Any(r => r.Reason != IncrementalStepRunReason.New))
                throw new NotSupportedException($"All generation steps are expected to be new");
            if (stepResults.Any(r => r.Result.IsSuccess == false || r.Result.Value is null))
                throw new NotSupportedException($"All generation steps are expected to be succesful");

            meta = stepResults.Select(s => s.Result.Value!.Value!).ToList();
        }

        return (generatedSources, meta);
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
