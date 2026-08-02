using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json.Nodes;

try
{
    var root = Directory.GetCurrentDirectory();
    var scenariosRoot = Path.Combine(root, "eval", "scenarios");
    var runsRoot = Path.Combine(root, ".harness-runs", "analyze-training-progress");
    var beforeStatus = RunGit(root, "status", "--short");
    var beforeSnapshot = Snapshot(scenariosRoot);
    var beforeRuns = Directory.Exists(runsRoot)
        ? Directory.GetDirectories(runsRoot).ToHashSet(StringComparer.Ordinal)
        : new HashSet<string>(StringComparer.Ordinal);

    var commands = Enumerable.Range(0, 2)
        .Select(_ => StartHarness(root, Path.Combine(root, "eval", "scenarios")))
        .ToArray();
    foreach (var process in commands)
    {
        process.WaitForExit();
        Ensure(process.ExitCode == 0, $"Harness subprocess failed with exit code {process.ExitCode}.");
    }

    var afterRuns = Directory.GetDirectories(runsRoot)
        .Where(path => !beforeRuns.Contains(path))
        .ToList();
    Ensure(afterRuns.Count == 2, "Parallel harness runs did not create exactly two new run directories.");

    foreach (var run in afterRuns)
    {
        var runName = Path.GetFileName(run);
        Ensure(runName.Length == 25 && runName[19] == 'Z' && runName[20] == '-',
            $"Invalid run ID format: {runName}");
        var metadata = JsonNode.Parse(File.ReadAllText(Path.Combine(run, "run.json")))!.AsObject();
        Ensure(metadata["skillId"]?.GetValue<string>() == "analyze-training-progress", "run.json has wrong skillId.");
        Ensure(metadata["model"]?.GetValue<string>() == "verify-model", "run.json has wrong model.");
        Ensure(metadata["repositoryState"] is not null, "run.json is missing repositoryState.");

        var scenarios = Directory.GetDirectories(Path.Combine(run, "scenarios"), "scenario-*");
        Ensure(scenarios.Length > 0, "Run contains no scenario directories.");
        foreach (var scenario in scenarios)
        {
            Ensure(File.Exists(Path.Combine(scenario, "metrics.json")), "Run scenario is missing metrics.json.");
            Ensure(File.Exists(Path.Combine(scenario, "result.json")), "Run scenario is missing result.json.");
            Ensure(File.Exists(Path.Combine(scenario, "evaluation.json")), "Run scenario is missing evaluation.json.");
        }
    }

    using (var failedProcess = StartHarness(root, Path.Combine(root, ".missing-result-source")))
    {
        failedProcess.WaitForExit();
        Ensure(failedProcess.ExitCode != 0, "Harness failure run unexpectedly succeeded.");
    }

    var failedRuns = Directory.GetDirectories(runsRoot)
        .Where(path => !beforeRuns.Contains(path) && !afterRuns.Contains(path))
        .ToList();
    Ensure(failedRuns.Count == 1, "Failed harness run was not preserved.");
    Ensure(Directory.GetDirectories(Path.Combine(failedRuns[0], "scenarios"), "scenario-*").Length > 0,
        "Failed harness run did not preserve processed scenarios.");

    Ensure(beforeStatus == RunGit(root, "status", "--short"), "Harness changed git status.");
    Ensure(beforeSnapshot == Snapshot(scenariosRoot), "Harness changed eval/scenarios.");
    Console.WriteLine("Verified isolated harness runs");
    return 0;
}
catch (Exception exception) when (exception is IOException or InvalidOperationException or System.Text.Json.JsonException)
{
    Console.Error.WriteLine($"VerifyHarness failed: {exception.Message}");
    return 1;
}

static Process StartHarness(string root, string resultSource)
{
    var start = new ProcessStartInfo("dotnet")
    {
        UseShellExecute = false,
        WorkingDirectory = root
    };
    start.ArgumentList.Add("run");
    start.ArgumentList.Add("--no-cache");
    start.ArgumentList.Add("--artifacts-path");
    start.ArgumentList.Add(Path.Combine(Path.GetTempPath(), "fitness-ai-harness-build-" + Guid.NewGuid().ToString("N")));
    start.ArgumentList.Add(Path.Combine(root, "eval", "scripts", "RunHarness.cs"));
    start.ArgumentList.Add("--");
    start.ArgumentList.Add("--skill-id");
    start.ArgumentList.Add("analyze-training-progress");
    start.ArgumentList.Add("--skill-path");
    start.ArgumentList.Add(".opencode/skills/analyze-training-progress");
    start.ArgumentList.Add("--model");
    start.ArgumentList.Add("verify-model");
    start.ArgumentList.Add("--result-source");
    start.ArgumentList.Add(resultSource);
    return Process.Start(start) ?? throw new InvalidOperationException("Could not start harness subprocess.");
}

static string Snapshot(string directory)
{
    using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal))
    {
        hash.AppendData(System.Text.Encoding.UTF8.GetBytes(Path.GetRelativePath(directory, file)));
        hash.AppendData(File.ReadAllBytes(file));
    }

    return Convert.ToHexString(hash.GetHashAndReset());
}

static string RunGit(string root, params string[] arguments)
{
    var start = new ProcessStartInfo("git")
    {
        UseShellExecute = false,
        WorkingDirectory = root,
        RedirectStandardOutput = true
    };
    foreach (var argument in arguments) start.ArgumentList.Add(argument);
    using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start git.");
    var output = process.StandardOutput.ReadToEnd().Trim();
    process.WaitForExit();
    return output;
}

static void Ensure(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
