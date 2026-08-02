using System.Diagnostics;
using System.Text.Json.Nodes;

try
{
    var repositoryRoot = Directory.GetCurrentDirectory();
    var scenariosRoot = Path.Combine(repositoryRoot, "eval", "scenarios");
    var normalizeScript = Path.Combine(repositoryRoot, "eval", "scripts", "Normalize.cs");
    var evaluateScript = Path.Combine(repositoryRoot, "eval", "scripts", "Evaluate.cs");
    var scenarios = Directory.GetDirectories(scenariosRoot, "scenario-*")
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToList();

    if (scenarios.Count == 0)
    {
        throw new InvalidOperationException($"No scenario directories found in {scenariosRoot}.");
    }

    var temporaryRoot = Path.Combine(Path.GetTempPath(), "fitness-ai-verify-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(temporaryRoot);
    try
    {
        foreach (var scenario in scenarios)
        {
            var scenarioName = Path.GetFileName(scenario);
            var temporaryMetrics = Path.Combine(temporaryRoot, scenarioName + "-metrics.json");
            var temporaryEvaluation = Path.Combine(temporaryRoot, scenarioName + "-evaluation.json");

            RunDotnet(normalizeScript, scenario, temporaryMetrics);
            AssertJsonEqual(Path.Combine(scenario, "metrics.json"), temporaryMetrics, scenarioName, "metrics.json");

            RunDotnet(evaluateScript, scenario, temporaryEvaluation);
            AssertJsonEqual(Path.Combine(scenario, "evaluation.json"), temporaryEvaluation, scenarioName, "evaluation.json");

            var evaluation = JsonNode.Parse(File.ReadAllText(temporaryEvaluation))!.AsObject();
            var failed = evaluation["summary"]!["failed"]!.GetValue<int>();
            if (failed != 0)
            {
                throw new InvalidOperationException($"{scenarioName}: evaluator reported {failed} failed check(s).");
            }

            Console.WriteLine($"Verified {scenarioName}");
        }
    }
    finally
    {
        Directory.Delete(temporaryRoot, recursive: true);
    }

    return 0;
}
catch (Exception exception) when (exception is IOException or InvalidOperationException or System.Text.Json.JsonException)
{
    Console.Error.WriteLine($"VerifyAll failed: {exception.Message}");
    return 1;
}

static void RunDotnet(string script, string scenario, string output)
{
    var start = new ProcessStartInfo("dotnet")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    start.ArgumentList.Add("run");
    start.ArgumentList.Add(script);
    start.ArgumentList.Add("--");
    start.ArgumentList.Add("--scenario");
    start.ArgumentList.Add(scenario);
    start.ArgumentList.Add("--output");
    start.ArgumentList.Add(output);

    using var process = Process.Start(start)
        ?? throw new InvalidOperationException($"Could not start dotnet for {Path.GetFileName(script)}.");
    var standardOutput = process.StandardOutput.ReadToEnd();
    var standardError = process.StandardError.ReadToEnd();
    process.WaitForExit();
    if (process.ExitCode != 0
        && !(Path.GetFileName(script) == "Evaluate.cs" && process.ExitCode == 3))
    {
        throw new InvalidOperationException(
            $"{Path.GetFileName(script)} failed for {Path.GetFileName(scenario)}: {standardError}{standardOutput}");
    }
}

static void AssertJsonEqual(string committedPath, string generatedPath, string scenarioName, string artifactName)
{
    if (!File.Exists(committedPath))
    {
        throw new InvalidOperationException($"{scenarioName}: committed {artifactName} is missing.");
    }

    var committed = JsonNode.Parse(File.ReadAllText(committedPath));
    var generated = JsonNode.Parse(File.ReadAllText(generatedPath));
    if (!JsonNode.DeepEquals(committed, generated))
    {
        throw new InvalidOperationException($"{scenarioName}: committed {artifactName} differs from regenerated output.");
    }
}
