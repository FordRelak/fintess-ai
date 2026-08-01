using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

try
{
    var cli = CliOptions.Parse(args);
    if (cli.ShowHelp)
    {
        Console.WriteLine("Usage: dotnet run Normalize.cs -- [--scenario <directory>] [--output <file>]");
        return 0;
    }

    var scenarioDirectory = Path.GetFullPath(cli.ScenarioDirectory);
    Ensure(Directory.Exists(scenarioDirectory), $"Scenario directory does not exist: {scenarioDirectory}");

    var programPath = Path.Combine(scenarioDirectory, "program.json");
    var workoutsPath = Path.Combine(scenarioDirectory, "workouts.json");
    var measurementsPath = Path.Combine(scenarioDirectory, "measurements.json");
    var outputPath = Path.GetFullPath(cli.OutputPath ?? Path.Combine(scenarioDirectory, "metrics.json"));

    var inputOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    var program = ReadJson<ProgramDocument>(programPath, inputOptions);
    var workouts = ReadJson<WorkoutsDocument>(workoutsPath, inputOptions);
    var measurements = ReadJson<MeasurementsDocument>(measurementsPath, inputOptions);

    var programIndex = ValidateProgram(program);
    ValidateWorkouts(workouts, programIndex);
    var measurementByDate = ValidateMeasurements(measurements, programIndex);

    var metrics = BuildMetrics(programIndex, workouts, measurements, measurementByDate);

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

    File.WriteAllText(outputPath, metrics.ToJsonString(outputOptions) + Environment.NewLine);
    Console.WriteLine($"Created {outputPath}");
    return 0;
}
catch (Exception exception) when (exception is IOException or JsonException or InvalidDataException or ArgumentException)
{
    Console.Error.WriteLine($"Normalize failed: {exception.Message}");
    return 1;
}

static T ReadJson<T>(string path, JsonSerializerOptions options) where T : class
{
    Ensure(File.Exists(path), $"Input file does not exist: {path}");

    try
    {
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), options)
               ?? throw new InvalidDataException($"{Path.GetFileName(path)} contains JSON null.");
    }
    catch (JsonException exception)
    {
        throw new InvalidDataException(
            $"Invalid JSON in {Path.GetFileName(path)} at {exception.Path ?? "unknown path"}: {exception.Message}",
            exception);
    }
}

static ProgramIndex ValidateProgram(ProgramDocument program)
{
    Ensure(program.SchemaVersion == Contract.SupportedSchemaVersion,
        $"program.json schemaVersion must be '{Contract.SupportedSchemaVersion}'.");

    var programId = Required(program.ProgramId, "program.json.programId");
    var version = Required(program.Version, "program.json.version");
    var goal = Required(program.Goal, "program.json.goal");
    var nutritionPhase = Required(program.NutritionPhase, "program.json.nutritionPhase");
    var period = RequiredObject(program.Period, "program.json.period");

    Ensure(period.From != default, "program.json.period.from is required.");
    Ensure(period.To != default, "program.json.period.to is required.");
    Ensure(period.To >= period.From, "program.json.period.to must not precede period.from.");

    var inclusiveDayCount = period.To.DayNumber - period.From.DayNumber + 1;
    var weekCount = (inclusiveDayCount + 6) / 7;

    var days = RequiredList(program.Days, "program.json.days");
    var dayById = new Dictionary<string, ProgramDay>(StringComparer.Ordinal);
    var exerciseById = new Dictionary<string, IndexedExercise>(StringComparer.Ordinal);
    var orderedExercises = new List<IndexedExercise>();

    foreach (var day in days)
    {
        var dayId = Required(day.DayId, "program.json.days[].dayId");
        Ensure(dayById.TryAdd(dayId, day), $"Duplicate program dayId '{dayId}'.");
        Ensure(day.FrequencyPerWeek > 0, $"Program day '{dayId}' frequencyPerWeek must be positive.");

        var exercises = RequiredList(day.Exercises, $"program day '{dayId}'.exercises");
        EnsureUnique(exercises.Select(exercise => exercise.Order), $"exercise order in program day '{dayId}'");

        foreach (var exercise in exercises.OrderBy(exercise => exercise.Order))
        {
            Ensure(exercise.Order > 0, $"Exercise order in program day '{dayId}' must be positive.");
            var exerciseId = Required(exercise.ExerciseId, $"program day '{dayId}'.exerciseId");
            var resistanceMode = Required(exercise.ResistanceMode, $"program exercise '{exerciseId}'.resistanceMode");
            Ensure(resistanceMode is "external_load" or "bodyweight",
                $"Program exercise '{exerciseId}' has unsupported resistanceMode '{resistanceMode}'.");

            ValidateRange(RequiredObject(exercise.WorkingSets, $"program exercise '{exerciseId}'.workingSets"),
                $"program exercise '{exerciseId}'.workingSets");
            ValidateRange(RequiredObject(exercise.TargetReps, $"program exercise '{exerciseId}'.targetReps"),
                $"program exercise '{exerciseId}'.targetReps");

            var targetEffort = RequiredObject(exercise.TargetEffort, $"program exercise '{exerciseId}'.targetEffort");
            ValidateEffort(Required(targetEffort.Min, $"program exercise '{exerciseId}'.targetEffort.min"),
                $"program exercise '{exerciseId}'.targetEffort.min");
            ValidateEffort(Required(targetEffort.Max, $"program exercise '{exerciseId}'.targetEffort.max"),
                $"program exercise '{exerciseId}'.targetEffort.max");

            var indexed = new IndexedExercise(dayId, exerciseId, resistanceMode, exercise);
            Ensure(exerciseById.TryAdd(exerciseId, indexed),
                $"ExerciseId '{exerciseId}' occurs more than once in program.json; metrics v1 requires globally unique exerciseId values.");
            orderedExercises.Add(indexed);
        }
    }

    return new ProgramIndex(
        programId,
        version,
        goal,
        nutritionPhase,
        period.From,
        period.To,
        weekCount,
        dayById,
        exerciseById,
        orderedExercises);
}

static void ValidateWorkouts(WorkoutsDocument workouts, ProgramIndex program)
{
    Ensure(workouts.SchemaVersion == Contract.SupportedSchemaVersion,
        $"workouts.json schemaVersion must be '{Contract.SupportedSchemaVersion}'.");

    var items = RequiredList(workouts.Workouts, "workouts.json.workouts");
    var workoutIds = new HashSet<string>(StringComparer.Ordinal);

    foreach (var workout in items)
    {
        var workoutId = Required(workout.WorkoutId, "workouts.json.workouts[].workoutId");
        Ensure(workoutIds.Add(workoutId), $"Duplicate workoutId '{workoutId}'.");
        Ensure(workout.StartedAt != default, $"Workout '{workoutId}' startedAt is required.");
        Ensure(workout.DurationSeconds > 0, $"Workout '{workoutId}' durationSeconds must be positive.");

        var workoutDate = DateOnly.FromDateTime(workout.StartedAt.Date);
        Ensure(workoutDate >= program.From && workoutDate <= program.To,
            $"Workout '{workoutId}' date {FormatDate(workoutDate)} is outside the program period.");

        var reference = RequiredObject(workout.Program, $"workout '{workoutId}'.program");
        Ensure(reference.ProgramId == program.ProgramId,
            $"Workout '{workoutId}' refers to programId '{reference.ProgramId}', expected '{program.ProgramId}'.");
        Ensure(reference.Version == program.Version,
            $"Workout '{workoutId}' refers to program version '{reference.Version}', expected '{program.Version}'.");

        var dayId = Required(reference.DayId, $"workout '{workoutId}'.program.dayId");
        Ensure(program.DayById.ContainsKey(dayId), $"Workout '{workoutId}' refers to unknown dayId '{dayId}'.");

        var exercises = RequiredList(workout.Exercises, $"workout '{workoutId}'.exercises");
        EnsureUnique(exercises.Select(exercise => exercise.Order), $"exercise order in workout '{workoutId}'");
        EnsureUnique(exercises.Select(exercise => Required(exercise.ExerciseId, $"workout '{workoutId}'.exerciseId")),
            $"exerciseId in workout '{workoutId}'");

        foreach (var exercise in exercises)
        {
            Ensure(exercise.Order > 0, $"Exercise order in workout '{workoutId}' must be positive.");
            var exerciseId = Required(exercise.ExerciseId, $"workout '{workoutId}'.exerciseId");
            Ensure(program.ExerciseById.TryGetValue(exerciseId, out var expected),
                $"Workout '{workoutId}' contains unknown exerciseId '{exerciseId}'.");
            Ensure(expected.DayId == dayId,
                $"Exercise '{exerciseId}' does not belong to program day '{dayId}'.");
            Ensure(expected.Definition.Order == exercise.Order,
                $"Exercise '{exerciseId}' has order {exercise.Order} in workout '{workoutId}', expected {expected.Definition.Order}.");

            var sets = RequiredList(exercise.Sets, $"workout '{workoutId}', exercise '{exerciseId}'.sets");
            EnsureUnique(sets.Select(set => set.Order), $"set order for exercise '{exerciseId}' in workout '{workoutId}'");
            Ensure(sets.Any(set => set.SetType == "working"),
                $"Exercise '{exerciseId}' in workout '{workoutId}' has no working sets.");

            foreach (var set in sets)
            {
                Ensure(set.Order > 0, $"Set order for exercise '{exerciseId}' in workout '{workoutId}' must be positive.");
                Ensure(set.SetType is "warmup" or "working",
                    $"Exercise '{exerciseId}' in workout '{workoutId}' has unsupported setType '{set.SetType}'.");
                Ensure(set.ResistanceMode == expected.ResistanceMode,
                    $"Exercise '{exerciseId}' in workout '{workoutId}' has resistanceMode '{set.ResistanceMode}', expected '{expected.ResistanceMode}'.");
                Ensure(set.Reps > 0, $"Exercise '{exerciseId}' in workout '{workoutId}' has a set with non-positive reps.");
                ValidateEffort(Required(set.Effort, $"exercise '{exerciseId}' in workout '{workoutId}'.effort"),
                    $"exercise '{exerciseId}' in workout '{workoutId}'.effort");

                if (expected.ResistanceMode == "external_load")
                {
                    Ensure(set.WeightKg is > 0,
                        $"External-load exercise '{exerciseId}' in workout '{workoutId}' requires positive weightKg for every set.");
                }
                else
                {
                    Ensure(set.WeightKg is null,
                        $"Bodyweight exercise '{exerciseId}' in workout '{workoutId}' must not contain weightKg.");
                }
            }
        }
    }
}

static IReadOnlyDictionary<DateOnly, decimal> ValidateMeasurements(
    MeasurementsDocument measurements,
    ProgramIndex program)
{
    Ensure(measurements.SchemaVersion == Contract.SupportedSchemaVersion,
        $"measurements.json schemaVersion must be '{Contract.SupportedSchemaVersion}'.");

    var items = RequiredList(measurements.Measurements, "measurements.json.measurements");
    var byDate = new Dictionary<DateOnly, decimal>();

    foreach (var measurement in items)
    {
        Ensure(measurement.Date != default, "measurements.json contains a measurement without date.");
        Ensure(measurement.WeightKg > 0,
            $"Measurement on {FormatDate(measurement.Date)} must have positive weightKg.");
        Ensure(measurement.Date >= program.From && measurement.Date <= program.To,
            $"Measurement date {FormatDate(measurement.Date)} is outside the program period.");
        Ensure(byDate.TryAdd(measurement.Date, measurement.WeightKg),
            $"Duplicate body-weight measurement date {FormatDate(measurement.Date)}.");
    }

    return byDate;
}

static JsonObject BuildMetrics(
    ProgramIndex program,
    WorkoutsDocument workouts,
    MeasurementsDocument measurements,
    IReadOnlyDictionary<DateOnly, decimal> measurementByDate)
{
    var orderedMeasurements = measurements.Measurements!
        .OrderBy(measurement => measurement.Date)
        .ToList();

    var rawMeasurementNodes = orderedMeasurements.Select(measurement => new JsonObject
    {
        ["date"] = FormatDate(measurement.Date),
        ["weightKg"] = measurement.WeightKg
    });

    var weeklyNodes = new List<JsonNode?>();
    decimal? previousAverage = null;
    decimal? firstAverage = null;
    decimal? lastAverage = null;

    for (var week = 1; week <= program.WeekCount; week++)
    {
        var values = orderedMeasurements
            .Where(measurement => GetWeek(measurement.Date, program.From) == week)
            .Select(measurement => measurement.WeightKg)
            .ToList();

        decimal? average = values.Count == 0
            ? null
            : decimal.Round(values.Average(), 3, MidpointRounding.AwayFromZero);
        JsonNode? averageNode = average.HasValue ? JsonValue.Create(average.Value) : null;
        JsonNode? changeNode = average.HasValue && previousAverage.HasValue
            ? JsonValue.Create(decimal.Round(average.Value - previousAverage.Value, 3, MidpointRounding.AwayFromZero))
            : null;

        weeklyNodes.Add(new JsonObject
        {
            ["week"] = week,
            ["averageKg"] = averageNode,
            ["measurementCount"] = values.Count,
            ["changeFromPreviousWeekKg"] = changeNode
        });

        if (average.HasValue)
        {
            firstAverage ??= average;
            lastAverage = average;
            previousAverage = average;
        }
    }

    JsonNode? totalChangeNode = firstAverage.HasValue && lastAverage.HasValue
        ? JsonValue.Create(decimal.Round(lastAverage.Value - firstAverage.Value, 3, MidpointRounding.AwayFromZero))
        : null;

    var orderedWorkouts = workouts.Workouts!
        .OrderBy(workout => workout.StartedAt)
        .ThenBy(workout => workout.WorkoutId, StringComparer.Ordinal)
        .ToList();

    var exerciseNodes = new List<JsonNode?>();
    foreach (var exerciseDefinition in program.OrderedExercises)
    {
        var performances = new List<JsonNode?>();

        foreach (var workout in orderedWorkouts)
        {
            var exercise = workout.Exercises!
                .SingleOrDefault(item => item.ExerciseId == exerciseDefinition.ExerciseId);
            if (exercise is null)
            {
                continue;
            }

            var workoutDate = DateOnly.FromDateTime(workout.StartedAt.Date);
            var workingSets = exercise.Sets!
                .Where(set => set.SetType == "working")
                .OrderBy(set => set.Order)
                .ToList();

            var setNodes = workingSets.Select((set, index) =>
            {
                var node = new JsonObject
                {
                    ["order"] = index + 1,
                    ["reps"] = set.Reps,
                    ["effort"] = set.Effort
                };

                if (exerciseDefinition.ResistanceMode == "external_load")
                {
                    node["weightKg"] = set.WeightKg!.Value;
                }

                return node;
            });

            var performance = new JsonObject
            {
                ["workoutId"] = workout.WorkoutId,
                ["date"] = FormatDate(workoutDate),
                ["week"] = GetWeek(workoutDate, program.From),
                ["workingSets"] = ToJsonArray(setNodes)
            };

            var totalReps = workingSets.Sum(set => set.Reps);
            var summary = new JsonObject
            {
                ["workingSetCount"] = workingSets.Count,
                ["totalReps"] = totalReps
            };

            if (exerciseDefinition.ResistanceMode == "external_load")
            {
                summary["volumeKg"] = workingSets.Sum(set => set.WeightKg!.Value * set.Reps);
                summary["maxWeightKg"] = workingSets.Max(set => set.WeightKg!.Value);
            }
            else
            {
                Ensure(measurementByDate.TryGetValue(workoutDate, out var bodyWeightKg),
                    $"Bodyweight exercise '{exerciseDefinition.ExerciseId}' requires a measurement on workout date {FormatDate(workoutDate)}.");
                performance["bodyWeightKg"] = bodyWeightKg;
            }

            performance["summary"] = summary;
            performances.Add(performance);
        }

        var definition = exerciseDefinition.Definition;
        exerciseNodes.Add(new JsonObject
        {
            ["exerciseId"] = exerciseDefinition.ExerciseId,
            ["resistanceMode"] = exerciseDefinition.ResistanceMode,
            ["programContext"] = new JsonObject
            {
                ["workingSets"] = RangeNode(definition.WorkingSets!),
                ["targetReps"] = RangeNode(definition.TargetReps!),
                ["targetEffort"] = new JsonObject
                {
                    ["min"] = definition.TargetEffort!.Min,
                    ["max"] = definition.TargetEffort.Max
                }
            },
            ["performances"] = ToJsonArray(performances)
        });
    }

    return new JsonObject
    {
        ["schemaVersion"] = Contract.SupportedSchemaVersion,
        ["analysisPeriod"] = new JsonObject
        {
            ["from"] = FormatDate(program.From),
            ["to"] = FormatDate(program.To),
            ["weekCount"] = program.WeekCount,
            ["weekDefinition"] = "consecutive_7_day_periods"
        },
        ["context"] = new JsonObject
        {
            ["goal"] = program.Goal,
            ["nutritionPhase"] = program.NutritionPhase,
            ["programId"] = program.ProgramId,
            ["programVersion"] = program.Version
        },
        ["bodyWeight"] = new JsonObject
        {
            ["measurements"] = ToJsonArray(rawMeasurementNodes),
            ["weekly"] = ToJsonArray(weeklyNodes),
            ["totalChangeKg"] = totalChangeNode
        },
        ["exercises"] = ToJsonArray(exerciseNodes)
    };
}

static JsonObject RangeNode(IntRange range) => new()
{
    ["min"] = range.Min,
    ["max"] = range.Max
};

static JsonArray ToJsonArray(IEnumerable<JsonNode?> nodes)
{
    var result = new JsonArray();
    foreach (var node in nodes)
    {
        result.Add(node);
    }

    return result;
}

static int GetWeek(DateOnly date, DateOnly from) => (date.DayNumber - from.DayNumber) / 7 + 1;

static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

static void ValidateRange(IntRange range, string path)
{
    Ensure(range.Min > 0, $"{path}.min must be positive.");
    Ensure(range.Max >= range.Min, $"{path}.max must be greater than or equal to min.");
}

static void ValidateEffort(string effort, string path)
{
    Ensure(effort is "low" or "moderate" or "high" or "maximum",
        $"{path} has unsupported value '{effort}'.");
}

static T RequiredObject<T>(T? value, string path) where T : class =>
    value ?? throw new InvalidDataException($"{path} is required.");

static string Required(string? value, string path)
{
    Ensure(!string.IsNullOrWhiteSpace(value), $"{path} is required.");
    return value!;
}

static List<T> RequiredList<T>(List<T>? value, string path)
{
    Ensure(value is { Count: > 0 }, $"{path} must be a non-empty array.");
    return value!;
}

static void EnsureUnique<T>(IEnumerable<T> values, string description) where T : notnull
{
    var seen = new HashSet<T>();
    foreach (var value in values)
    {
        Ensure(seen.Add(value), $"Duplicate {description}: '{value}'.");
    }
}

static void Ensure(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidDataException(message);
    }
}

sealed record ProgramIndex(
    string ProgramId,
    string Version,
    string Goal,
    string NutritionPhase,
    DateOnly From,
    DateOnly To,
    int WeekCount,
    IReadOnlyDictionary<string, ProgramDay> DayById,
    IReadOnlyDictionary<string, IndexedExercise> ExerciseById,
    IReadOnlyList<IndexedExercise> OrderedExercises);

sealed record IndexedExercise(
    string DayId,
    string ExerciseId,
    string ResistanceMode,
    ProgramExercise Definition);

sealed class CliOptions
{
    public string ScenarioDirectory { get; private set; } = ".";
    public string? OutputPath { get; private set; }
    public bool ShowHelp { get; private set; }

    public static CliOptions Parse(string[] arguments)
    {
        var result = new CliOptions();

        for (var index = 0; index < arguments.Length; index++)
        {
            switch (arguments[index])
            {
                case "--scenario":
                    result.ScenarioDirectory = NextValue(arguments, ref index, "--scenario");
                    break;
                case "--output":
                    result.OutputPath = NextValue(arguments, ref index, "--output");
                    break;
                case "--help" or "-h":
                    result.ShowHelp = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{arguments[index]}'. Use --help for usage.");
            }
        }

        return result;
    }

    private static string NextValue(string[] arguments, ref int index, string option)
    {
        if (index + 1 >= arguments.Length)
        {
            throw new ArgumentException($"{option} requires a value.");
        }

        index++;
        if (string.IsNullOrWhiteSpace(arguments[index]))
        {
            throw new ArgumentException($"{option} requires a non-empty value.");
        }

        return arguments[index];
    }
}

static class Contract
{
    public const string SupportedSchemaVersion = "1.0";
}

sealed class ProgramDocument
{
    public string? SchemaVersion { get; init; }
    public string? ProgramId { get; init; }
    public string? Version { get; init; }
    public ProgramPeriod? Period { get; init; }
    public string? Goal { get; init; }
    public string? NutritionPhase { get; init; }
    public List<ProgramDay>? Days { get; init; }
}

sealed class ProgramPeriod
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
}

sealed class ProgramDay
{
    public string? DayId { get; init; }
    public int FrequencyPerWeek { get; init; }
    public List<ProgramExercise>? Exercises { get; init; }
}

sealed class ProgramExercise
{
    public int Order { get; init; }
    public string? ExerciseId { get; init; }
    public string? ResistanceMode { get; init; }
    public IntRange? WorkingSets { get; init; }
    public IntRange? TargetReps { get; init; }
    public StringRange? TargetEffort { get; init; }
}

sealed class IntRange
{
    public int Min { get; init; }
    public int Max { get; init; }
}

sealed class StringRange
{
    public string? Min { get; init; }
    public string? Max { get; init; }
}

sealed class WorkoutsDocument
{
    public string? SchemaVersion { get; init; }
    public List<Workout>? Workouts { get; init; }
}

sealed class Workout
{
    public string? WorkoutId { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public int DurationSeconds { get; init; }
    public WorkoutProgramReference? Program { get; init; }
    public List<WorkoutExercise>? Exercises { get; init; }
}

sealed class WorkoutProgramReference
{
    public string? ProgramId { get; init; }
    public string? Version { get; init; }
    public string? DayId { get; init; }
}

sealed class WorkoutExercise
{
    public int Order { get; init; }
    public string? ExerciseId { get; init; }
    public string? SourceName { get; init; }
    public List<WorkoutSet>? Sets { get; init; }
}

sealed class WorkoutSet
{
    public int Order { get; init; }
    public string? SetType { get; init; }
    public string? ResistanceMode { get; init; }
    public decimal? WeightKg { get; init; }
    public int Reps { get; init; }
    public string? Effort { get; init; }
}

sealed class MeasurementsDocument
{
    public string? SchemaVersion { get; init; }
    public List<BodyWeightMeasurement>? Measurements { get; init; }
}

sealed class BodyWeightMeasurement
{
    public DateOnly Date { get; init; }
    public decimal WeightKg { get; init; }
}