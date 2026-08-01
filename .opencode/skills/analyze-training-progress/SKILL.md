---
name: analyze-training-progress
description: Analyze normalized resistance-training metrics and produce a structured result.json that separates observed exercise progression or plateaus from causal hypotheses, recommendations, and data limitations. Use when Codex is given a metrics.json containing weekly body-weight and exercise-performance history, or is asked to assess training progress, detect local versus program-wide plateaus, or generate output compatible with the bundled training-analysis result schema.
---

# Analyze Training Progress

Analyze normalized training history without turning correlation or program intent into proven causality. Produce `result.json` that conforms exactly to [references/result-schema.json](references/result-schema.json).

## Inputs and boundaries

1. Resolve exactly one `metrics.json`. Ask the user to identify it only when multiple candidates remain ambiguous.
2. Read the entire input and the bundled result schema before analyzing.
3. Use only `metrics.json` as evidence. Do not inspect `ground-truth.json`, `evaluation.json`, an existing `result.json`, or other expected-answer artifacts, even when they are in the same directory.
4. Treat `context.goal`, `context.nutritionPhase`, and exercise `programContext` as declared intent, not proof that nutrition, recovery, rest times, or technique matched the plan.
5. Do not estimate e1RM unless the input explicitly provides it and the schema supports it. The current schema does not.

Require enough history to distinguish a sustained pattern from short-term noise. When history is insufficient, avoid a plateau/progression claim and record `insufficient_history`.

## Analysis workflow

### 1. Inspect data quality

- Sort exercise performances by `week`, then `date`.
- Check week coverage, missing performances, duplicate weeks, changes in working-set count, and missing effort or load fields.
- Use `resistanceMode` to select comparable metrics:
  - `external_load`: compare `maxWeightKg`, `totalReps`, and `volumeKg` while accounting for set count and effort.
  - `bodyweight`: compare `totalReps`, set count, effort, and `bodyWeightKg`; increasing repetitions while body weight is stable or rising is progression.
- Prefer weekly body-weight averages over isolated measurements.
- Treat `volumeKg` as supporting evidence, not a standalone proof of progression when set count or effort changed.

### 2. Classify each exercise

Identify meaningful progression when at least one comparable performance dimension improves and the change is sustained rather than immediately reversed. Typical signals include:

- higher external load at comparable repetitions, working-set count, and effort;
- more repetitions at the same load with comparable sets and effort;
- repeated double-progression cycles where repetition gains are followed by load increases;
- for bodyweight work, more repetitions at stable or higher body weight.

Identify a performance plateau only when all of the following hold:

- at least four consecutive recent exposures form a sustained non-improving block;
- load or the relevant resistance measure does not increase;
- repetitions and volume show no positive trend beyond small fluctuations;
- working-set count and effort remain comparable;
- the pattern is not merely the expected repetition reset immediately after a load increase.

Set `onsetWeek` to the first week of the sustained non-improving block after the last meaningful improvement. Set `throughWeek` to the last observed week supporting the claim.

Do not emit overlapping plateau and progression observations for the same exercise and interval. Prefer the classification that describes the current sustained trend. Emit `program_target_deviation` only when observed sets, reps, or effort meaningfully and repeatedly deviate from `programContext`.

### 3. Determine scope

- Use `scope: exercise` for exercise-specific progression or plateau and include the exact `exerciseId`.
- Use `scope: body_weight` for a trend in weekly average body weight.
- Use `scope: program` for training consistency or a genuinely program-wide pattern.
- Do not infer a program-wide plateau when other exercises continue to progress.
- Derive training consistency from unique workout IDs and week coverage. Do not equate regular attendance with adequate recovery.

### 4. Separate facts from explanations

Place only directly supported patterns in `observations`. Use `high` confidence only when the relevant history is complete and the trend is unambiguous.

Place possible explanations in `hypotheses`:

- link every hypothesis to existing observation IDs through `basedOn`;
- use `low` confidence for recovery, nutrition, fatigue, technique, pain, or injury explanations when their direct data is absent;
- use at most `medium` confidence for plausible exercise-specific adaptation or programming constraints unless the input directly tests the explanation;
- use `insufficient_evidence` when the data supports the observed outcome but cannot distinguish its cause.

Never state a causal hypothesis as confirmed merely because it is plausible.

### 5. Recommend the smallest justified change

- Link every recommendation to existing observation or hypothesis IDs.
- For a local plateau, target that exercise first. Prefer `adjust_progression`, `modify_volume`, `modify_intensity`, or `substitute_exercise` according to the evidence.
- Do not prescribe a program-wide deload solely from one exercise plateau.
- Do not prioritize `review_nutrition` from a declared `surplus` phase or body-weight trend alone.
- Preserve exercises that are progressing unless there is separate evidence to change them.
- Make `action` concrete enough to test over the next few exposures, but do not invent unavailable load, recovery, nutrition, technique, or rest-time facts.

### 6. Record limitations

Add a limitation only when the corresponding data is absent or too sparse. In particular:

- add `missing_recovery_data` when sleep, readiness, or recovery measures are absent;
- add `missing_nutrition_intake_data` when actual energy and protein intake are absent, even if `nutritionPhase` is present;
- add `missing_technique_data` when execution quality, range of motion, or video evidence is absent;
- add `missing_pain_or_injury_data` only when its absence materially affects the analysis;
- add `measurement_sparsity` or `insufficient_history` when applicable.

Describe only how the missing data limits the conclusion. Do not use a limitation as an implied diagnosis.

## Output contract

1. Produce one JSON object with exactly these top-level fields: `schemaVersion`, `observations`, `hypotheses`, `recommendations`, and `limitations`.
2. Use `schemaVersion: "1.0"` and only enum values defined by the bundled schema.
3. Use unique, stable IDs matching `obs-*` and `hyp-*`. Prefer semantic IDs such as `obs-smith-press-plateau` over positional IDs.
4. Ensure every `basedOn` reference resolves to an ID in the same result.
5. Keep evidence intervals within the parent observation interval and ensure `fromWeek <= throughWeek`.
6. Include no unknown properties and no Markdown inside the JSON.
7. Validate the result against [references/result-schema.json](references/result-schema.json). Also check cross-reference integrity, which JSON Schema alone does not enforce.
8. Write `result.json` next to the local input. If the input is not writable or is not local, create a downloadable `result.json` artifact.

Use the user's language for `summary`, `reasoning`, `action`, and `impact`. Keep enum values and IDs in English as defined by the schema.
