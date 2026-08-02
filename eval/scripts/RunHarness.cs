using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

try
{
    var options = CliOptions.Parse(args);
    if (options.ShowHelp)
    {
        Console.WriteLine(
            "Usage: dotnet run RunHarness.cs -- --skill-id <id> --skill-path <path> --model <model> " +
            "[--opencode <command>] [--result-source <scenario-root>] [--evaluate-script <path>] [--contract-script <path>]");
        return 0;
    }

    var root = Directory.GetCurrentDirectory();
    var scenariosRoot = Path.Combine(root, "eval", "scenarios");
    Ensure(Directory.Exists(scenariosRoot), $"Scenario directory does not exist: {scenariosRoot}");

    var scenarios = Directory.GetDirectories(scenariosRoot, "scenario-*")
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToList();
    Ensure(scenarios.Count > 0, $"No scenario directories found in {scenariosRoot}.");

    var run = CreateRun(root, options);
    Console.WriteLine($"Harness run: {run.Root}");

    var failed = false;
    foreach (var sourceScenario in scenarios)
    {
        var scenarioName = Path.GetFileName(sourceScenario);
        var runScenario = Path.Combine(run.Root, "scenarios", scenarioName);
        Directory.CreateDirectory(runScenario);

        var runMetrics = Path.Combine(runScenario, "metrics.json");
        var normalizeExit = RunNormalization(root, sourceScenario, runMetrics, runScenario);
        if (normalizeExit != 0 || !File.Exists(runMetrics))
        {
            Console.Error.WriteLine($"{scenarioName}: normalization failed or metrics.json is missing (exit {normalizeExit}).");
            failed = true;
            continue;
        }

        var modelExit = RunModel(options, sourceScenario, runScenario, runMetrics);
        if (modelExit != 0 || !File.Exists(Path.Combine(runScenario, "result.json")))
        {
            Console.Error.WriteLine($"{scenarioName}: model run failed or result.json is missing (exit {modelExit}).");
            failed = true;
            continue;
        }

        var evaluationExit = RunEvaluation(root, options, sourceScenario, runScenario);
        if (evaluationExit != 0)
        {
            failed = true;
        }
    }

    var contractExit = RunContracts(root, options, run.Root);
    if (contractExit != 0)
    {
        failed = true;
    }

    Console.WriteLine($"Harness run preserved at {run.Root}");
    return failed ? 1 : 0;
}
catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException or Win32Exception)
{
    Console.Error.WriteLine($"RunHarness failed: {exception.Message}");
    return 1;
}

static RunMetadata CreateRun(string root, CliOptions options)
{
    var timestamp = DateTimeOffset.UtcNow;
    var runBase = Path.Combine(root, ".harness-runs", options.SkillId);
    Directory.CreateDirectory(runBase);

    string runId;
    string runRoot;
    while (true)
    {
        var suffixBytes = new byte[2];
        RandomNumberGenerator.Fill(suffixBytes);
        var suffix = Convert.ToHexString(suffixBytes).ToLowerInvariant();
        runId = $"{timestamp:yyyy-MM-dd'T'HH-mm-ss'Z'}-{suffix}";
        runRoot = Path.Combine(runBase, runId);
        var reservation = runRoot + ".lock";
        var reserved = false;
        try
        {
            using (new FileStream(reservation, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
            }
            reserved = true;
            Directory.CreateDirectory(runRoot);
            File.Delete(reservation);
            break;
        }
        catch (IOException)
        {
            if (reserved && File.Exists(reservation)) File.Delete(reservation);
            timestamp = DateTimeOffset.UtcNow;
        }
    }

    var git = ReadGitState(root);
    var skillPath = Path.GetRelativePath(root, Path.GetFullPath(options.SkillPath));
    var metadata = new JsonObject
    {
        ["runId"] = runId,
        ["skillId"] = options.SkillId,
        ["skillPath"] = skillPath,
        ["startedAt"] = timestamp.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
        ["repositoryCommit"] = git.Commit is null ? null : JsonValue.Create(git.Commit),
        ["repositoryDirty"] = git.Dirty,
        ["repositoryState"] = git.State,
        ["model"] = options.Model
    };
    File.WriteAllText(Path.Combine(runRoot, "run.json"), metadata.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
    return new RunMetadata(runId, runRoot);
}

static int RunModel(CliOptions options, string sourceScenario, string workingDirectory, string metricsPath)
{
    if (options.ResultSource is not null)
    {
        var sourceResult = Path.Combine(options.ResultSource, Path.GetFileName(sourceScenario), "result.json");
        if (!File.Exists(sourceResult)) return 1;
        File.Copy(sourceResult, Path.Combine(workingDirectory, "result.json"));
        return 0;
    }

    var start = new ProcessStartInfo(options.OpenCodeCommand)
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        WorkingDirectory = workingDirectory
    };
    start.ArgumentList.Add("run");
    start.ArgumentList.Add("--model");
    start.ArgumentList.Add(options.Model);
    start.ArgumentList.Add($"Use the /{options.SkillId} skill at {options.SkillPath}. Analyze only this exact metrics file: {metricsPath}. Write result.json next to it. Do not inspect ground-truth.json, evaluation.json, or any existing result.json.");

    try
    {
        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Could not start opencode.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        File.WriteAllText(Path.Combine(workingDirectory, "model.stdout.log"), stdout);
        File.WriteAllText(Path.Combine(workingDirectory, "model.stderr.log"), stderr);
        return process.ExitCode;
    }
    catch (Win32Exception exception)
    {
        File.WriteAllText(Path.Combine(workingDirectory, "model.stderr.log"), exception.Message);
        return 1;
    }
}

static int RunNormalization(string root, string sourceScenario, string outputPath, string workingDirectory)
{
    var start = new ProcessStartInfo("dotnet")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    start.ArgumentList.Add("run");
    start.ArgumentList.Add("--no-cache");
    start.ArgumentList.Add("--artifacts-path");
    start.ArgumentList.Add(Path.Combine(workingDirectory, ".dotnet-normalize"));
    start.ArgumentList.Add(Path.Combine(root, "eval", "scripts", "Normalize.cs"));
    start.ArgumentList.Add("--");
    start.ArgumentList.Add("--scenario");
    start.ArgumentList.Add(sourceScenario);
    start.ArgumentList.Add("--output");
    start.ArgumentList.Add(outputPath);

    using var process = Process.Start(start)
        ?? throw new InvalidOperationException("Could not start normalizer.");
    var stdout = process.StandardOutput.ReadToEnd();
    var stderr = process.StandardError.ReadToEnd();
    process.WaitForExit();
    File.WriteAllText(Path.Combine(workingDirectory, "normalize.stdout.log"), stdout);
    File.WriteAllText(Path.Combine(workingDirectory, "normalize.stderr.log"), stderr);
    return process.ExitCode;
}

static int RunEvaluation(string root, CliOptions options, string sourceScenario, string runScenario)
{
    var start = new ProcessStartInfo("dotnet") { UseShellExecute = false };
    start.ArgumentList.Add("run");
    start.ArgumentList.Add("--no-cache");
    start.ArgumentList.Add("--artifacts-path");
    start.ArgumentList.Add(Path.Combine(runScenario, ".dotnet-evaluate"));
    start.ArgumentList.Add(Path.GetFullPath(options.EvaluateScript));
    start.ArgumentList.Add("--");
    start.ArgumentList.Add("--scenario");
    start.ArgumentList.Add(runScenario);
    start.ArgumentList.Add("--result");
    start.ArgumentList.Add(Path.Combine(runScenario, "result.json"));
    start.ArgumentList.Add("--ground-truth");
    start.ArgumentList.Add(Path.Combine(sourceScenario, "ground-truth.json"));
    start.ArgumentList.Add("--result-schema");
    start.ArgumentList.Add(Path.Combine(root, "eval", "schemas", "result-schema.json"));
    start.ArgumentList.Add("--ground-truth-schema");
    start.ArgumentList.Add(Path.Combine(root, "eval", "schemas", "ground-truth-schema.json"));
    start.ArgumentList.Add("--output");
    start.ArgumentList.Add(Path.Combine(runScenario, "evaluation.json"));
    using var process = Process.Start(start)
        ?? throw new InvalidOperationException("Could not start evaluator.");
    process.WaitForExit();

    if (process.ExitCode == 3 && TryReadFailedChecks(Path.Combine(runScenario, "evaluation.json"), out var failed) && failed == 0)
    {
        return 0;
    }

    return process.ExitCode;
}

static int RunContracts(string root, CliOptions options, string runRoot)
{
    var start = new ProcessStartInfo("dotnet") { UseShellExecute = false };
    start.ArgumentList.Add("run");
    start.ArgumentList.Add("--no-cache");
    start.ArgumentList.Add("--artifacts-path");
    start.ArgumentList.Add(Path.Combine(runRoot, ".dotnet-contracts"));
    start.ArgumentList.Add(Path.GetFullPath(options.ContractScript));
    start.ArgumentList.Add("--");
    start.ArgumentList.Add("--run");
    start.ArgumentList.Add(runRoot);
    using var process = Process.Start(start)
        ?? throw new InvalidOperationException("Could not start contract checker.");
    process.WaitForExit();
    return process.ExitCode;
}

static bool TryReadFailedChecks(string path, out int failed)
{
    failed = -1;
    if (!File.Exists(path)) return false;
    var node = JsonNode.Parse(File.ReadAllText(path));
    var value = node?["summary"]?["failed"] as JsonValue;
    return value is not null && value.TryGetValue<int>(out failed);
}

static GitState ReadGitState(string root)
{
    var commit = RunGit(root, "rev-parse", "HEAD");
    if (commit.ExitCode != 0)
    {
        var unavailable = RunGit(root, "rev-parse", "--git-dir");
        return unavailable.ExitCode == 0
            ? new GitState(null, false, "unborn")
            : new GitState(null, false, "unavailable");
    }

    var status = RunGit(root, "status", "--porcelain");
    if (status.ExitCode != 0)
    {
        return new GitState(commit.Output, false, "unavailable");
    }

    var dirty = !string.IsNullOrWhiteSpace(status.Output);
    return new GitState(commit.Output, dirty, dirty ? "dirty" : "clean");
}

static (int ExitCode, string Output) RunGit(string root, params string[] arguments)
{
    var start = new ProcessStartInfo("git")
    {
        UseShellExecute = false,
        WorkingDirectory = root,
        RedirectStandardOutput = true
    };
    foreach (var argument in arguments) start.ArgumentList.Add(argument);
    Process? process;
    try
    {
        process = Process.Start(start);
    }
    catch (Win32Exception)
    {
        return (1, string.Empty);
    }

    if (process is null) return (1, string.Empty);
    var output = process.StandardOutput.ReadToEnd().Trim();
    process.WaitForExit();
    return (process.ExitCode, output);
}

static void Ensure(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed record RunMetadata(string Id, string Root);
sealed record GitState(string? Commit, bool Dirty, string State);

sealed class CliOptions
{
    public string SkillId { get; private set; } = string.Empty;
    public string SkillPath { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string OpenCodeCommand { get; private set; } = "opencode";
    public string? ResultSource { get; private set; }
    public string EvaluateScript { get; private set; } = "eval/scripts/Evaluate.cs";
    public string ContractScript { get; private set; } = "eval/scripts/VerifyContracts.cs";
    public bool ShowHelp { get; private set; }

    public static CliOptions Parse(string[] args)
    {
        var result = new CliOptions();
        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "--help" or "-h": result.ShowHelp = true; break;
                case "--skill-id": result.SkillId = Next(args, ref index, argument); break;
                case "--skill-path": result.SkillPath = Next(args, ref index, argument); break;
                case "--model": result.Model = Next(args, ref index, argument); break;
                case "--opencode": result.OpenCodeCommand = Next(args, ref index, argument); break;
                case "--result-source": result.ResultSource = Path.GetFullPath(Next(args, ref index, argument)); break;
                case "--evaluate-script": result.EvaluateScript = Next(args, ref index, argument); break;
                case "--contract-script": result.ContractScript = Next(args, ref index, argument); break;
                default: throw new ArgumentException($"Unknown argument '{argument}'. Use --help for usage.");
            }
        }

        if (!result.ShowHelp)
        {
            Require(!string.IsNullOrWhiteSpace(result.SkillId), "--skill-id is required.");
            Require(!string.IsNullOrWhiteSpace(result.SkillPath), "--skill-path is required.");
            Require(!string.IsNullOrWhiteSpace(result.Model), "--model is required.");
        }

        return result;
    }

    private static string Next(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length) throw new ArgumentException($"Argument '{option}' requires a value.");
        index++;
        return args[index];
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
