using System.Diagnostics;
using System.Text.Json.Nodes;

try
{
    var cli = CliOptions.Parse(args);
    var root = Directory.GetCurrentDirectory();
    var evaluate = Path.Combine(root, "eval", "scripts", "Evaluate.cs");
    var scenarios = Path.Combine(root, "eval", "scenarios");
    var temporaryRoot = Path.Combine(Path.GetTempPath(), "fitness-ai-contracts-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(temporaryRoot);
    try
    {
        var declineSource = Path.Combine(scenarios, "scenario-008-sustained-performance-decline");
        var declineScenario = cli.RunRoot is null
            ? declineSource
            : Path.Combine(cli.RunRoot, "scenarios", Path.GetFileName(declineSource));
        var validExitCode = RunEvaluate(evaluate, declineScenario, null, temporaryRoot, Path.Combine(declineSource, "ground-truth.json"));
        Ensure(validExitCode is 0 or 3, "performance_decline scenario must pass automated checks.");

        var legacyVersion = ReadResult(declineScenario);
        legacyVersion["schemaVersion"] = "1.0";
        Ensure(RunEvaluate(evaluate, declineScenario, WriteTemporary(temporaryRoot, "legacy-version", legacyVersion), temporaryRoot, Path.Combine(declineSource, "ground-truth.json")) == 1,
            "result schema version 1.0 must be rejected.");

        var invalidInterval = ReadResult(declineScenario);
        invalidInterval["observations"]![0]! ["evidence"]![0]! ["throughWeek"] = 7;
        Ensure(RunEvaluate(evaluate, declineScenario, WriteTemporary(temporaryRoot, "invalid-interval", invalidInterval), temporaryRoot, Path.Combine(declineSource, "ground-truth.json")) == 1,
            "evidence outside an observation interval must be rejected.");

        var insufficientSource = Path.Combine(scenarios, "scenario-002-insufficient-history");
        var insufficientHistory = ResolveRunScenario(cli.RunRoot, insufficientSource);
        Ensure(RunEvaluate(evaluate, insufficientHistory, null, temporaryRoot, Path.Combine(insufficientSource, "ground-truth.json")) is 0 or 3,
            "empty requiredObservations must be accepted.");

        var programSource = Path.Combine(scenarios, "scenario-007-program-wide-plateau");
        var programPlateau = ResolveRunScenario(cli.RunRoot, programSource);
        var duplicateMatch = ReadResult(programPlateau);
        var observations = duplicateMatch["observations"]!.AsArray();
        while (observations.Count > 2)
        {
            observations.RemoveAt(observations.Count - 1);
        }
        duplicateMatch["hypotheses"] = new JsonArray();
        duplicateMatch["recommendations"] = new JsonArray();
        Ensure(RunEvaluate(evaluate, programPlateau, WriteTemporary(temporaryRoot, "duplicate-match", duplicateMatch), temporaryRoot, Path.Combine(programSource, "ground-truth.json")) == 2,
            "one observation must not satisfy multiple required observations.");

        var sparseSource = Path.Combine(scenarios, "scenario-005-sparse-history");
        var sparseMetricsPath = cli.RunRoot is null
            ? Path.Combine(temporaryRoot, "sparse-metrics.json")
            : Path.Combine(cli.RunRoot, "scenarios", Path.GetFileName(sparseSource), "metrics.json");
        if (cli.RunRoot is null)
        {
            Ensure(RunNormalize(sparseSource, sparseMetricsPath) == 0, "sparse-history normalization must pass.");
        }

        var sparseMetrics = JsonNode.Parse(File.ReadAllText(sparseMetricsPath))!;
        var weekly = sparseMetrics["bodyWeight"]!["weekly"]!.AsArray();
        var gapFollowedByMeasurement = weekly
            .Select((week, index) => (week: week!.AsObject(), index))
            .First(item => item.index > 0
                           && item.week["averageKg"] is not null
                           && weekly[item.index - 1]! ["averageKg"] is null)
            .week;
        Ensure(gapFollowedByMeasurement["changeFromPreviousWeekKg"] is null,
            "body-weight delta after a missing calendar week must be null.");
    }
    finally
    {
        Directory.Delete(temporaryRoot, recursive: true);
    }

    Console.WriteLine("Verified contract checks");
    return 0;
}
catch (Exception exception) when (exception is IOException or InvalidOperationException or System.Text.Json.JsonException)
{
    Console.Error.WriteLine($"VerifyContracts failed: {exception.Message}");
    return 1;
}

static string ResolveRunScenario(string? runRoot, string sourceScenario) =>
    runRoot is null
        ? sourceScenario
        : Path.Combine(runRoot, "scenarios", Path.GetFileName(sourceScenario));

static JsonObject ReadResult(string scenario) =>
    JsonNode.Parse(File.ReadAllText(Path.Combine(scenario, "result.json")))!.AsObject();

static string WriteTemporary(string temporaryRoot, string name, JsonNode node)
{
    var path = Path.Combine(temporaryRoot, name + ".json");
    File.WriteAllText(path, node.ToJsonString());
    return path;
}

static int RunEvaluate(string script, string scenario, string? result, string temporaryRoot, string? groundTruth = null)
{
    var start = new ProcessStartInfo("dotnet") { UseShellExecute = false };
    start.ArgumentList.Add("run");
    start.ArgumentList.Add("--no-cache");
    start.ArgumentList.Add("--artifacts-path");
    start.ArgumentList.Add(Path.Combine(temporaryRoot, "artifacts-" + Guid.NewGuid().ToString("N")));
    start.ArgumentList.Add(script);
    start.ArgumentList.Add("--");
    start.ArgumentList.Add("--scenario");
    start.ArgumentList.Add(scenario);
    start.ArgumentList.Add("--result-schema");
    start.ArgumentList.Add(Path.Combine(Directory.GetCurrentDirectory(), "eval", "schemas", "result-schema.json"));
    start.ArgumentList.Add("--ground-truth-schema");
    start.ArgumentList.Add(Path.Combine(Directory.GetCurrentDirectory(), "eval", "schemas", "ground-truth-schema.json"));
    start.ArgumentList.Add("--output");
    start.ArgumentList.Add(Path.Combine(temporaryRoot, Guid.NewGuid().ToString("N") + ".json"));
    if (groundTruth is not null)
    {
        start.ArgumentList.Add("--ground-truth");
        start.ArgumentList.Add(groundTruth);
    }
    if (result is not null)
    {
        start.ArgumentList.Add("--result");
        start.ArgumentList.Add(result);
    }

    using var process = Process.Start(start)
        ?? throw new InvalidOperationException("Could not start evaluator.");
    process.WaitForExit();
    return process.ExitCode;
}

static int RunNormalize(string scenario, string output)
{
    var start = new ProcessStartInfo("dotnet") { UseShellExecute = false };
    start.ArgumentList.Add("run");
    start.ArgumentList.Add("--no-cache");
    start.ArgumentList.Add("--artifacts-path");
    start.ArgumentList.Add(Path.Combine(Path.GetDirectoryName(output)!, ".dotnet-normalize"));
    start.ArgumentList.Add(Path.Combine(Directory.GetCurrentDirectory(), "eval", "scripts", "Normalize.cs"));
    start.ArgumentList.Add("--");
    start.ArgumentList.Add("--scenario");
    start.ArgumentList.Add(scenario);
    start.ArgumentList.Add("--output");
    start.ArgumentList.Add(output);
    using var process = Process.Start(start)
        ?? throw new InvalidOperationException("Could not start normalizer.");
    process.WaitForExit();
    return process.ExitCode;
}

static void Ensure(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed class CliOptions
{
    public string? RunRoot { get; private set; }

    public static CliOptions Parse(string[] args)
    {
        var result = new CliOptions();
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--run":
                    if (index + 1 >= args.Length) throw new ArgumentException("--run requires a value.");
                    result.RunRoot = Path.GetFullPath(args[++index]);
                    break;
                case "--help":
                    Console.WriteLine("Usage: dotnet run VerifyContracts.cs -- [--run <harness-run-directory>]");
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{args[index]}'.");
            }
        }

        return result;
    }
}
