using System.Diagnostics;
using System.Text.Json.Nodes;

try
{
    var root = Directory.GetCurrentDirectory();
    var evaluate = Path.Combine(root, "eval", "scripts", "Evaluate.cs");
    var scenarios = Path.Combine(root, "eval", "scenarios");
    var temporaryRoot = Path.Combine(Path.GetTempPath(), "fitness-ai-contracts-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(temporaryRoot);
    try
    {
        var declineScenario = Path.Combine(scenarios, "scenario-008-sustained-performance-decline");
        var validExitCode = RunEvaluate(evaluate, declineScenario, null, temporaryRoot);
        Ensure(validExitCode is 0 or 3, "performance_decline scenario must pass automated checks.");

        var legacyVersion = ReadResult(declineScenario);
        legacyVersion["schemaVersion"] = "1.0";
        Ensure(RunEvaluate(evaluate, declineScenario, WriteTemporary(temporaryRoot, "legacy-version", legacyVersion), temporaryRoot) == 1,
            "result schema version 1.0 must be rejected.");

        var invalidInterval = ReadResult(declineScenario);
        invalidInterval["observations"]![0]! ["evidence"]![0]! ["throughWeek"] = 7;
        Ensure(RunEvaluate(evaluate, declineScenario, WriteTemporary(temporaryRoot, "invalid-interval", invalidInterval), temporaryRoot) == 1,
            "evidence outside an observation interval must be rejected.");

        var insufficientHistory = Path.Combine(scenarios, "scenario-002-insufficient-history");
        Ensure(RunEvaluate(evaluate, insufficientHistory, null, temporaryRoot) is 0 or 3,
            "empty requiredObservations must be accepted.");

        var programPlateau = Path.Combine(scenarios, "scenario-007-program-wide-plateau");
        var duplicateMatch = ReadResult(programPlateau);
        var observations = duplicateMatch["observations"]!.AsArray();
        while (observations.Count > 2)
        {
            observations.RemoveAt(observations.Count - 1);
        }
        duplicateMatch["hypotheses"] = new JsonArray();
        duplicateMatch["recommendations"] = new JsonArray();
        Ensure(RunEvaluate(evaluate, programPlateau, WriteTemporary(temporaryRoot, "duplicate-match", duplicateMatch), temporaryRoot) == 2,
            "one observation must not satisfy multiple required observations.");

        var sparseMetrics = JsonNode.Parse(File.ReadAllText(Path.Combine(scenarios, "scenario-005-sparse-history", "metrics.json")))!;
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

static JsonObject ReadResult(string scenario) =>
    JsonNode.Parse(File.ReadAllText(Path.Combine(scenario, "result.json")))!.AsObject();

static string WriteTemporary(string temporaryRoot, string name, JsonNode node)
{
    var path = Path.Combine(temporaryRoot, name + ".json");
    File.WriteAllText(path, node.ToJsonString());
    return path;
}

static int RunEvaluate(string script, string scenario, string? result, string temporaryRoot)
{
    var start = new ProcessStartInfo("dotnet") { UseShellExecute = false };
    start.ArgumentList.Add("run");
    start.ArgumentList.Add(script);
    start.ArgumentList.Add("--");
    start.ArgumentList.Add("--scenario");
    start.ArgumentList.Add(scenario);
    start.ArgumentList.Add("--output");
    start.ArgumentList.Add(Path.Combine(temporaryRoot, Guid.NewGuid().ToString("N") + ".json"));
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

static void Ensure(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
