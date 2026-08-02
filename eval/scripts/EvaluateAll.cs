using System.ComponentModel;
using System.Diagnostics;

try
{
    var repositoryRoot = Directory.GetCurrentDirectory();
    var scenariosRoot = Path.Combine(repositoryRoot, "eval", "scenarios");
    if (!Directory.Exists(scenariosRoot))
    {
        throw new InvalidOperationException($"Scenario directory does not exist: {scenariosRoot}");
    }

    var scenarios = Directory.GetDirectories(scenariosRoot, "scenario-*")
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToList();
    if (scenarios.Count == 0)
    {
        throw new InvalidOperationException($"No scenario directories found in {scenariosRoot}.");
    }

    var evaluateScript = Path.Combine(repositoryRoot, "eval", "scripts", "Evaluate.cs");
    foreach (var scenario in scenarios)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false
        };
        start.ArgumentList.Add("run");
        start.ArgumentList.Add(evaluateScript);
        start.ArgumentList.Add("--");
        start.ArgumentList.Add("--scenario");
        start.ArgumentList.Add(scenario);

        using var process = Process.Start(start)
            ?? throw new InvalidOperationException($"Could not start evaluator for {Path.GetFileName(scenario)}.");
        process.WaitForExit();
    }

    return 0;
}
catch (Exception exception) when (exception is IOException or InvalidOperationException or Win32Exception)
{
    Console.Error.WriteLine($"EvaluateAll failed: {exception.Message}");
    return 1;
}
