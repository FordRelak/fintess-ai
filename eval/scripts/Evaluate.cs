using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;

try
{
    var cli = CliOptions.Parse(args);
    if (cli.ShowHelp)
    {
        Console.WriteLine(
            "Usage: dotnet run Evaluate.cs -- [--scenario <directory>] [--result <file>] " +
            "[--ground-truth <file>] [--result-schema <file>] " +
            "[--ground-truth-schema <file>] [--output <file>]");
        return 0;
    }

    var scenarioDirectory = Path.GetFullPath(cli.ScenarioDirectory);
    Ensure(Directory.Exists(scenarioDirectory),
        $"Scenario directory does not exist: {scenarioDirectory}");

    var resultPath = ResolveInputPath(
        cli.ResultPath,
        scenarioDirectory,
        "result.json");
    var groundTruthPath = ResolveInputPath(
        cli.GroundTruthPath,
        scenarioDirectory,
        "ground-truth.json");
    var resultSchemaPath = ResolveSchemaPath(
        cli.ResultSchemaPath,
        scenarioDirectory,
        "result-schema.json");
    var groundTruthSchemaPath = ResolveSchemaPath(
        cli.GroundTruthSchemaPath,
        scenarioDirectory,
        "ground-truth-schema.json");
    var outputPath = Path.GetFullPath(
        cli.OutputPath ?? Path.Combine(scenarioDirectory, "evaluation.json"));

    var resultNode = ReadJsonNode(resultPath);
    var groundTruthNode = ReadJsonNode(groundTruthPath);
    var resultSchema = ReadJsonNode(resultSchemaPath);
    var groundTruthSchema = ReadJsonNode(groundTruthSchemaPath);

    ValidateAgainstSchema(resultNode, resultSchema, "result.json");
    ValidateAgainstSchema(groundTruthNode, groundTruthSchema, "ground-truth.json");

    var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    var result = Deserialize<AnalysisResult>(resultNode, "result.json", jsonOptions);
    var groundTruth = Deserialize<GroundTruth>(groundTruthNode, "ground-truth.json", jsonOptions);

    ValidateResultReferences(result);
    ValidateGroundTruth(groundTruth);
    var evaluation = Evaluate(result, groundTruth);

    var outputDirectory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrEmpty(outputDirectory))
    {
        Directory.CreateDirectory(outputDirectory);
    }

    var outputOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    File.WriteAllText(outputPath, evaluation.ToJsonString(outputOptions) + Environment.NewLine);
    Console.WriteLine($"Created {outputPath}");
    Console.WriteLine($"Evaluation status: {evaluation["status"]!.GetValue<string>()}");

    return evaluation["status"]!.GetValue<string>() switch
    {
        "passed" => 0,
        "failed" => 2,
        "manual_review" => 3,
        _ => 1
    };
}
catch (Exception exception) when (exception is IOException
                                   or JsonException
                                   or InvalidDataException
                                   or ArgumentException
                                   or InvalidOperationException)
{
    Console.Error.WriteLine($"Evaluate failed: {exception.Message}");
    return 1;
}

static JsonNode ReadJsonNode(string path)
{
    Ensure(File.Exists(path), $"Input file does not exist: {path}");

    try
    {
        return JsonNode.Parse(File.ReadAllText(path))
               ?? throw new InvalidDataException($"{Path.GetFileName(path)} contains JSON null.");
    }
    catch (JsonException exception)
    {
        throw new InvalidDataException(
            $"Invalid JSON in {Path.GetFileName(path)} at {exception.Path ?? "unknown path"}: {exception.Message}",
            exception);
    }
}

static T Deserialize<T>(JsonNode node, string name, JsonSerializerOptions options) where T : class
{
    try
    {
        return node.Deserialize<T>(options)
               ?? throw new InvalidDataException($"{name} contains JSON null.");
    }
    catch (JsonException exception)
    {
        throw new InvalidDataException($"Could not read {name}: {exception.Message}", exception);
    }
}

static string ResolveInputPath(string? explicitPath, string scenarioDirectory, string fileName)
{
    return explicitPath is null
        ? Path.Combine(scenarioDirectory, fileName)
        : Path.GetFullPath(explicitPath);
}

static string ResolveSchemaPath(string? explicitPath, string scenarioDirectory, string fileName)
{
    if (explicitPath is not null)
    {
        return Path.GetFullPath(explicitPath);
    }

    var candidates = new[]
    {
        Path.Combine(scenarioDirectory, fileName),
        Path.Combine(scenarioDirectory, "schemas", fileName),
        Path.Combine(scenarioDirectory, "..", "..", "schemas", fileName)
    };

    return candidates
        .Select(Path.GetFullPath)
        .FirstOrDefault(File.Exists)
        ?? Path.GetFullPath(candidates[0]);
}

static void ValidateAgainstSchema(JsonNode instance, JsonNode schema, string instanceName)
{
    var errors = new List<string>();
    JsonSchemaSubsetValidator.Validate(instance, schema, schema, "$", errors);

    if (errors.Count == 0)
    {
        return;
    }

    var shown = errors.Take(20).Select(error => $"  - {error}");
    var suffix = errors.Count > 20 ? $"\n  - ... and {errors.Count - 20} more" : string.Empty;
    throw new InvalidDataException(
        $"{instanceName} does not match its schema:\n{string.Join("\n", shown)}{suffix}");
}

static void ValidateResultReferences(AnalysisResult result)
{
    var observationIds = new HashSet<string>(StringComparer.Ordinal);
    foreach (var observation in result.Observations)
    {
        Ensure(observationIds.Add(observation.Id),
            $"Duplicate observation id '{observation.Id}'.");
        Ensure(observation.OnsetWeek <= observation.ThroughWeek,
            $"Observation '{observation.Id}' has onsetWeek after throughWeek.");
        Ensure(observation.Scope == "exercise" || observation.ExerciseId is null,
            $"Observation '{observation.Id}' has exerciseId outside exercise scope.");

        foreach (var evidence in observation.Evidence)
        {
            Ensure(evidence.FromWeek <= evidence.ThroughWeek,
                $"Observation '{observation.Id}' contains evidence with fromWeek after throughWeek.");
            Ensure(evidence.FromWeek >= observation.OnsetWeek && evidence.ThroughWeek <= observation.ThroughWeek,
                $"Observation '{observation.Id}' contains evidence outside its observation interval.");
            Ensure(observation.Scope == "program" || evidence.ExerciseId is null,
                $"Observation '{observation.Id}' has exercise-specific evidence outside program scope.");
        }
    }

    var hypothesisIds = new HashSet<string>(StringComparer.Ordinal);
    foreach (var hypothesis in result.Hypotheses)
    {
        Ensure(hypothesisIds.Add(hypothesis.Id),
            $"Duplicate hypothesis id '{hypothesis.Id}'.");

        foreach (var reference in hypothesis.BasedOn)
        {
            Ensure(observationIds.Contains(reference),
                $"Hypothesis '{hypothesis.Id}' references unknown observation '{reference}'.");
        }
    }

    foreach (var recommendation in result.Recommendations)
    {
        foreach (var reference in recommendation.BasedOn)
        {
            Ensure(observationIds.Contains(reference) || hypothesisIds.Contains(reference),
                $"Recommendation '{recommendation.Type}' references unknown item '{reference}'.");
        }
    }
}

static void ValidateGroundTruth(GroundTruth groundTruth)
{
    foreach (var observation in groundTruth.RequiredObservations)
    {
        Ensure(observation.Scope == "exercise" || observation.ExerciseId is null,
            "Ground truth required observation has exerciseId outside exercise scope.");
        if (observation.OnsetWeek is not null)
        {
            Ensure(observation.OnsetWeek.Min <= observation.OnsetWeek.Max,
                "Ground truth required observation has an invalid onsetWeek range.");
        }

        foreach (var evidence in observation.RequiredEvidence)
        {
            Ensure(observation.Scope == "program" || evidence.ExerciseId is null,
                "Ground truth evidence matcher has exerciseId outside program scope.");
        }
    }

    foreach (var observation in groundTruth.ForbiddenObservations)
    {
        Ensure(observation.Scope == "exercise" || observation.ExerciseId is null,
            "Ground truth forbidden observation has exerciseId outside exercise scope.");
    }
}

static JsonObject Evaluate(AnalysisResult result, GroundTruth groundTruth)
{
    var checks = new JsonArray();
    var matchedObservationIds = new HashSet<string>(StringComparer.Ordinal);

    for (var index = 0; index < groundTruth.RequiredObservations.Count; index++)
    {
        var expected = groundTruth.RequiredObservations[index];
        var match = result.Observations.FirstOrDefault(actual =>
            !matchedObservationIds.Contains(actual.Id)
            && MatchesRequiredObservation(actual, expected));
        if (match is not null)
        {
            matchedObservationIds.Add(match.Id);
        }

        AddCheck(
            checks,
            $"required-observation-{index + 1:00}",
            "required_observation",
            match is null ? "failed" : "passed",
            match is null
                ? $"Missing required observation: {DescribeRequiredObservation(expected)}."
                : $"Matched required observation with id '{match.Id}'.");
    }

    for (var index = 0; index < groundTruth.ForbiddenObservations.Count; index++)
    {
        var forbidden = groundTruth.ForbiddenObservations[index];
        var match = result.Observations.FirstOrDefault(actual =>
            MatchesForbiddenObservation(actual, forbidden));

        AddCheck(
            checks,
            $"forbidden-observation-{index + 1:00}",
            "forbidden_observation",
            match is null ? "passed" : "failed",
            match is null
                ? $"Forbidden observation absent: {DescribeForbiddenObservation(forbidden)}."
                : $"Forbidden observation found with id '{match.Id}'.");
    }

    for (var index = 0; index < result.Hypotheses.Count; index++)
    {
        var hypothesis = result.Hypotheses[index];
        var allowed = groundTruth.HypothesisPolicy.Allowed.Any(policy =>
            policy.Type == hypothesis.Type && policy.Confidence.Contains(hypothesis.Confidence));

        AddCheck(
            checks,
            $"hypothesis-policy-{index + 1:00}",
            "hypothesis_policy",
            allowed ? "passed" : "failed",
            allowed
                ? $"Hypothesis '{hypothesis.Id}' is allowed at confidence '{hypothesis.Confidence}'."
                : $"Hypothesis '{hypothesis.Id}' has forbidden type/confidence combination " +
                  $"'{hypothesis.Type}/{hypothesis.Confidence}'.");
    }

    for (var index = 0; index < groundTruth.RecommendationPolicy.Required.Count; index++)
    {
        var expected = groundTruth.RecommendationPolicy.Required[index];
        var match = result.Recommendations.FirstOrDefault(actual =>
            MatchesRecommendation(actual, expected));

        AddCheck(
            checks,
            $"required-recommendation-{index + 1:00}",
            "required_recommendation",
            match is null ? "failed" : "passed",
            match is null
                ? $"Missing required recommendation: {DescribeRecommendation(expected)}."
                : $"Matched required recommendation '{match.Type}'.");
    }

    for (var index = 0; index < groundTruth.RecommendationPolicy.Forbidden.Count; index++)
    {
        var forbidden = groundTruth.RecommendationPolicy.Forbidden[index];
        var match = result.Recommendations.FirstOrDefault(actual =>
            MatchesRecommendation(actual, forbidden));

        AddCheck(
            checks,
            $"forbidden-recommendation-{index + 1:00}",
            "forbidden_recommendation",
            match is null ? "passed" : "failed",
            match is null
                ? $"Forbidden recommendation absent: {DescribeRecommendation(forbidden)}."
                : $"Forbidden recommendation found: '{match.Type}'.");
    }

    for (var index = 0; index < groundTruth.RequiredLimitations.Count; index++)
    {
        var requiredType = groundTruth.RequiredLimitations[index];
        var present = result.Limitations.Any(actual => actual.Type == requiredType);

        AddCheck(
            checks,
            $"required-limitation-{index + 1:00}",
            "required_limitation",
            present ? "passed" : "failed",
            present
                ? $"Required limitation '{requiredType}' is present."
                : $"Required limitation '{requiredType}' is missing.");
    }

    for (var index = 0; index < groundTruth.ClaimPolicy.ForbiddenClaims.Count; index++)
    {
        AddCheck(
            checks,
            $"forbidden-claim-{index + 1:00}",
            "semantic_claim",
            "manual_review",
            groundTruth.ClaimPolicy.ForbiddenClaims[index]);
    }

    var failedCount = checks
        .OfType<JsonObject>()
        .Count(check => check["status"]!.GetValue<string>() == "failed");
    var manualReviewCount = checks
        .OfType<JsonObject>()
        .Count(check => check["status"]!.GetValue<string>() == "manual_review");
    var passedCount = checks.Count - failedCount - manualReviewCount;

    var status = failedCount > 0
        ? "failed"
        : manualReviewCount > 0
            ? "manual_review"
            : "passed";

    return new JsonObject
    {
        ["schemaVersion"] = "1.0",
        ["scenarioId"] = groundTruth.ScenarioId,
        ["status"] = status,
        ["summary"] = new JsonObject
        {
            ["total"] = checks.Count,
            ["passed"] = passedCount,
            ["failed"] = failedCount,
            ["manualReview"] = manualReviewCount
        },
        ["checks"] = checks
    };
}

static bool MatchesRequiredObservation(Observation actual, RequiredObservation expected)
{
    if (actual.Type != expected.Type
        || actual.Scope != expected.Scope
        || actual.ExerciseId != expected.ExerciseId
        || actual.ThroughWeek != expected.ThroughWeek
        || !expected.Confidence.Contains(actual.Confidence))
    {
        return false;
    }

    if (expected.OnsetWeek is not null
        && (actual.OnsetWeek < expected.OnsetWeek.Min
            || actual.OnsetWeek > expected.OnsetWeek.Max))
    {
        return false;
    }

    return expected.RequiredEvidence.All(required =>
        actual.Evidence.Any(evidence =>
            evidence.Metric == required.Metric
            && required.Trend.Contains(evidence.Trend)
            && (required.ExerciseId is null || evidence.ExerciseId == required.ExerciseId)));
}

static bool MatchesForbiddenObservation(Observation actual, ForbiddenObservation forbidden)
{
    return actual.Type == forbidden.Type
           && actual.Scope == forbidden.Scope
           && (forbidden.ExerciseId is null || actual.ExerciseId == forbidden.ExerciseId);
}

static bool MatchesRecommendation(Recommendation actual, RecommendationMatcher matcher)
{
    IEnumerable<string> allowedTypes = matcher.Type is not null
        ? new[] { matcher.Type }
        : matcher.TypeAnyOf;

    if (!allowedTypes.Contains(actual.Type)
        || !matcher.PriorityAnyOf.Contains(actual.Priority))
    {
        return false;
    }

    return matcher.ExerciseId.ValueKind switch
    {
        JsonValueKind.Undefined => true,
        JsonValueKind.Null => actual.ExerciseId is null,
        JsonValueKind.String => actual.ExerciseId == matcher.ExerciseId.GetString(),
        _ => false
    };
}

static string DescribeRequiredObservation(RequiredObservation observation)
{
    var exercise = observation.ExerciseId is null ? string.Empty : $", exerciseId={observation.ExerciseId}";
    return $"type={observation.Type}, scope={observation.Scope}{exercise}";
}

static string DescribeForbiddenObservation(ForbiddenObservation observation)
{
    var exercise = observation.ExerciseId is null ? string.Empty : $", exerciseId={observation.ExerciseId}";
    return $"type={observation.Type}, scope={observation.Scope}{exercise}";
}

static string DescribeRecommendation(RecommendationMatcher matcher)
{
    var types = matcher.Type is not null ? matcher.Type : string.Join("|", matcher.TypeAnyOf);
    var exercise = matcher.ExerciseId.ValueKind switch
    {
        JsonValueKind.Undefined => string.Empty,
        JsonValueKind.Null => ", exerciseId=<absent>",
        _ => $", exerciseId={matcher.ExerciseId.GetString()}"
    };
    return $"type={types}, priority={string.Join("|", matcher.PriorityAnyOf)}{exercise}";
}

static void AddCheck(
    JsonArray checks,
    string id,
    string category,
    string status,
    string message)
{
    checks.Add(new JsonObject
    {
        ["id"] = id,
        ["category"] = category,
        ["status"] = status,
        ["message"] = message
    });
}

static void Ensure(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidDataException(message);
    }
}

static class JsonSchemaSubsetValidator
{
    public static void Validate(
        JsonNode? instance,
        JsonNode schemaNode,
        JsonNode rootSchema,
        string path,
        List<string> errors)
    {
        if (schemaNode is not JsonObject schema)
        {
            return;
        }

        if (schema["$ref"] is JsonValue referenceValue)
        {
            var reference = referenceValue.GetValue<string>();
            var resolved = ResolveReference(rootSchema, reference);
            Validate(instance, resolved, rootSchema, path, errors);
        }

        if (schema["type"] is JsonNode typeNode && !MatchesType(instance, typeNode))
        {
            errors.Add($"{path}: expected type {typeNode.ToJsonString()}, got {DescribeType(instance)}.");
            return;
        }

        if (schema["const"] is JsonNode constant && !JsonNode.DeepEquals(instance, constant))
        {
            errors.Add($"{path}: value must equal {constant.ToJsonString()}.");
        }

        if (schema["enum"] is JsonArray allowed
            && !allowed.Any(value => JsonNode.DeepEquals(instance, value)))
        {
            errors.Add($"{path}: value is not in enum {allowed.ToJsonString()}.");
        }

        ApplyCompositions(instance, schema, rootSchema, path, errors);

        if (instance is JsonObject objectValue)
        {
            ValidateObject(objectValue, schema, rootSchema, path, errors);
        }

        if (instance is JsonArray arrayValue)
        {
            ValidateArray(arrayValue, schema, rootSchema, path, errors);
        }

        if (TryGetString(instance, out var stringValue))
        {
            ValidateString(stringValue, schema, path, errors);
        }

        if (TryGetDecimal(instance, out var numberValue)
            && schema["minimum"] is JsonValue minimumValue
            && TryGetDecimal(minimumValue, out var minimum)
            && numberValue < minimum)
        {
            errors.Add($"{path}: number must be at least {minimum.ToString(CultureInfo.InvariantCulture)}.");
        }
    }

    private static void ApplyCompositions(
        JsonNode? instance,
        JsonObject schema,
        JsonNode rootSchema,
        string path,
        List<string> errors)
    {
        if (schema["allOf"] is JsonArray allOf)
        {
            foreach (var child in allOf.OfType<JsonNode>())
            {
                Validate(instance, child, rootSchema, path, errors);
            }
        }

        if (schema["if"] is JsonNode ifSchema)
        {
            var branch = Matches(instance, ifSchema, rootSchema)
                ? schema["then"]
                : schema["else"];

            if (branch is not null)
            {
                Validate(instance, branch, rootSchema, path, errors);
            }
        }

        if (schema["oneOf"] is JsonArray oneOf)
        {
            var matchCount = oneOf
                .OfType<JsonNode>()
                .Count(child => Matches(instance, child, rootSchema));
            if (matchCount != 1)
            {
                errors.Add($"{path}: expected exactly one oneOf branch to match, got {matchCount}.");
            }
        }

        if (schema["not"] is JsonNode notSchema && Matches(instance, notSchema, rootSchema))
        {
            errors.Add($"{path}: value matches a forbidden 'not' schema.");
        }
    }

    private static void ValidateObject(
        JsonObject instance,
        JsonObject schema,
        JsonNode rootSchema,
        string path,
        List<string> errors)
    {
        if (schema["required"] is JsonArray required)
        {
            foreach (var propertyNode in required)
            {
                var property = propertyNode!.GetValue<string>();
                if (!instance.ContainsKey(property))
                {
                    errors.Add($"{path}: missing required property '{property}'.");
                }
            }
        }

        var properties = schema["properties"] as JsonObject;
        if (properties is not null)
        {
            foreach (var (name, propertySchema) in properties)
            {
                if (propertySchema is not null && instance.TryGetPropertyValue(name, out var value))
                {
                    Validate(value, propertySchema, rootSchema, AppendProperty(path, name), errors);
                }
            }
        }

        if (schema["additionalProperties"] is JsonValue additionalProperties
            && additionalProperties.TryGetValue<bool>(out var allowAdditional)
            && !allowAdditional)
        {
            var known = properties?.Select(property => property.Key).ToHashSet(StringComparer.Ordinal)
                        ?? new HashSet<string>(StringComparer.Ordinal);
            foreach (var name in instance.Select(property => property.Key).Where(name => !known.Contains(name)))
            {
                errors.Add($"{path}: unknown property '{name}'.");
            }
        }
    }

    private static void ValidateArray(
        JsonArray instance,
        JsonObject schema,
        JsonNode rootSchema,
        string path,
        List<string> errors)
    {
        if (schema["minItems"] is JsonValue minItemsNode
            && minItemsNode.TryGetValue<int>(out var minItems)
            && instance.Count < minItems)
        {
            errors.Add($"{path}: expected at least {minItems} items, got {instance.Count}.");
        }

        if (schema["uniqueItems"] is JsonValue uniqueNode
            && uniqueNode.TryGetValue<bool>(out var uniqueItems)
            && uniqueItems)
        {
            var serialized = instance.Select(item => item?.ToJsonString() ?? "null").ToList();
            if (serialized.Distinct(StringComparer.Ordinal).Count() != serialized.Count)
            {
                errors.Add($"{path}: array items must be unique.");
            }
        }

        if (schema["items"] is JsonNode itemSchema)
        {
            for (var index = 0; index < instance.Count; index++)
            {
                Validate(instance[index], itemSchema, rootSchema, $"{path}[{index}]", errors);
            }
        }
    }

    private static void ValidateString(
        string value,
        JsonObject schema,
        string path,
        List<string> errors)
    {
        if (schema["minLength"] is JsonValue minLengthNode
            && minLengthNode.TryGetValue<int>(out var minLength)
            && value.Length < minLength)
        {
            errors.Add($"{path}: string length must be at least {minLength}.");
        }

        if (schema["pattern"] is JsonValue patternNode)
        {
            var pattern = patternNode.GetValue<string>();
            if (!Regex.IsMatch(value, pattern, RegexOptions.CultureInvariant))
            {
                errors.Add($"{path}: string does not match pattern '{pattern}'.");
            }
        }
    }

    private static bool Matches(JsonNode? instance, JsonNode schema, JsonNode rootSchema)
    {
        var errors = new List<string>();
        Validate(instance, schema, rootSchema, "$", errors);
        return errors.Count == 0;
    }

    private static JsonNode ResolveReference(JsonNode rootSchema, string reference)
    {
        Ensure(reference.StartsWith("#/", StringComparison.Ordinal),
            $"Only local JSON Schema references are supported, got '{reference}'.");

        JsonNode? current = rootSchema;
        foreach (var rawSegment in reference[2..].Split('/'))
        {
            var segment = rawSegment.Replace("~1", "/", StringComparison.Ordinal)
                .Replace("~0", "~", StringComparison.Ordinal);
            current = current?[segment];
            Ensure(current is not null, $"JSON Schema reference '{reference}' cannot be resolved.");
        }

        return current!;
    }

    private static bool MatchesType(JsonNode? instance, JsonNode typeNode)
    {
        if (typeNode is JsonArray types)
        {
            return types.Any(type => MatchesSingleType(instance, type!.GetValue<string>()));
        }

        return MatchesSingleType(instance, typeNode.GetValue<string>());
    }

    private static bool MatchesSingleType(JsonNode? instance, string expected)
    {
        return expected switch
        {
            "null" => instance is null,
            "object" => instance is JsonObject,
            "array" => instance is JsonArray,
            "string" => TryGetString(instance, out _),
            "integer" => TryGetDecimal(instance, out var integer) && decimal.Truncate(integer) == integer,
            "number" => TryGetDecimal(instance, out _),
            "boolean" => instance is JsonValue value && value.TryGetValue<bool>(out _),
            _ => throw new InvalidDataException($"Unsupported JSON Schema type '{expected}'.")
        };
    }

    private static bool TryGetString(JsonNode? node, out string value)
    {
        if (node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var text))
        {
            value = text;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryGetDecimal(JsonNode? node, out decimal value)
    {
        if (node is JsonValue jsonValue && jsonValue.TryGetValue<decimal>(out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    private static string DescribeType(JsonNode? node)
    {
        if (node is null) return "null";
        if (node is JsonObject) return "object";
        if (node is JsonArray) return "array";
        if (TryGetString(node, out _)) return "string";
        if (node is JsonValue boolean && boolean.TryGetValue<bool>(out _)) return "boolean";
        if (TryGetDecimal(node, out _)) return "number";
        return "unknown";
    }

    private static string AppendProperty(string path, string property)
    {
        return Regex.IsMatch(property, "^[A-Za-z_][A-Za-z0-9_]*$")
            ? $"{path}.{property}"
            : $"{path}['{property}']";
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidDataException(message);
        }
    }
}

sealed class CliOptions
{
    public string ScenarioDirectory { get; private set; } = ".";
    public string? ResultPath { get; private set; }
    public string? GroundTruthPath { get; private set; }
    public string? ResultSchemaPath { get; private set; }
    public string? GroundTruthSchemaPath { get; private set; }
    public string? OutputPath { get; private set; }
    public bool ShowHelp { get; private set; }

    public static CliOptions Parse(string[] args)
    {
        var result = new CliOptions();

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "-h" or "--help":
                    result.ShowHelp = true;
                    break;
                case "--scenario":
                    result.ScenarioDirectory = NextValue(args, ref index, argument);
                    break;
                case "--result":
                    result.ResultPath = NextValue(args, ref index, argument);
                    break;
                case "--ground-truth":
                    result.GroundTruthPath = NextValue(args, ref index, argument);
                    break;
                case "--result-schema":
                    result.ResultSchemaPath = NextValue(args, ref index, argument);
                    break;
                case "--ground-truth-schema":
                    result.GroundTruthSchemaPath = NextValue(args, ref index, argument);
                    break;
                case "--output":
                    result.OutputPath = NextValue(args, ref index, argument);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{argument}'. Use --help for usage.");
            }
        }

        return result;
    }

    private static string NextValue(string[] args, ref int index, string argument)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Argument '{argument}' requires a value.");
        }

        index++;
        return args[index];
    }
}

sealed class AnalysisResult
{
    public required string SchemaVersion { get; init; }
    public required List<Observation> Observations { get; init; }
    public required List<Hypothesis> Hypotheses { get; init; }
    public required List<Recommendation> Recommendations { get; init; }
    public required List<Limitation> Limitations { get; init; }
}

sealed class Observation
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string Scope { get; init; }
    public string? ExerciseId { get; init; }
    public required int OnsetWeek { get; init; }
    public required int ThroughWeek { get; init; }
    public required string Confidence { get; init; }
    public required List<Evidence> Evidence { get; init; }
    public required string Summary { get; init; }
}

sealed class Evidence
{
    public required string Metric { get; init; }
    public required int FromWeek { get; init; }
    public required int ThroughWeek { get; init; }
    public required string Trend { get; init; }
    public string? Details { get; init; }
    public string? ExerciseId { get; init; }
}

sealed class Hypothesis
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string Confidence { get; init; }
    public required List<string> BasedOn { get; init; }
    public required string Reasoning { get; init; }
}

sealed class Recommendation
{
    public required string Type { get; init; }
    public string? ExerciseId { get; init; }
    public required List<string> BasedOn { get; init; }
    public required string Priority { get; init; }
    public required string Action { get; init; }
}

sealed class Limitation
{
    public required string Type { get; init; }
    public required string Impact { get; init; }
}

sealed class GroundTruth
{
    public required string SchemaVersion { get; init; }
    public required string ScenarioId { get; init; }
    public required List<RequiredObservation> RequiredObservations { get; init; }
    public required List<ForbiddenObservation> ForbiddenObservations { get; init; }
    public required HypothesisPolicy HypothesisPolicy { get; init; }
    public required RecommendationPolicy RecommendationPolicy { get; init; }
    public required List<string> RequiredLimitations { get; init; }
    public required ClaimPolicy ClaimPolicy { get; init; }
}

sealed class RequiredObservation
{
    public required string Type { get; init; }
    public required string Scope { get; init; }
    public string? ExerciseId { get; init; }
    public WeekRange? OnsetWeek { get; init; }
    public required int ThroughWeek { get; init; }
    public required List<string> Confidence { get; init; }
    public List<EvidenceMatcher> RequiredEvidence { get; init; } = [];
}

sealed class ForbiddenObservation
{
    public required string Type { get; init; }
    public required string Scope { get; init; }
    public string? ExerciseId { get; init; }
}

sealed class WeekRange
{
    public required int Min { get; init; }
    public required int Max { get; init; }
}

sealed class EvidenceMatcher
{
    public required string Metric { get; init; }
    public required List<string> Trend { get; init; }
    public string? ExerciseId { get; init; }
}

sealed class HypothesisPolicy
{
    public required List<AllowedHypothesis> Allowed { get; init; }
}

sealed class AllowedHypothesis
{
    public required string Type { get; init; }
    public required List<string> Confidence { get; init; }
}

sealed class RecommendationPolicy
{
    public required List<RecommendationMatcher> Required { get; init; }
    public required List<RecommendationMatcher> Forbidden { get; init; }
}

sealed class RecommendationMatcher
{
    public string? Type { get; init; }
    public List<string> TypeAnyOf { get; init; } = [];
    public JsonElement ExerciseId { get; init; }
    public required List<string> PriorityAnyOf { get; init; }
}

sealed class ClaimPolicy
{
    public required List<string> ForbiddenClaims { get; init; }
}
