namespace Plugin.Maui.FeatureFlags;

/// <summary>
/// Result of evaluating a single flag against the current device context.
/// </summary>
public sealed record FeatureFlagEvaluation
{
    /// <summary>
    /// Flag key that was evaluated.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Whether the flag is on for this device / user.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Why the evaluator returned <see cref="Enabled"/>.
    /// </summary>
    public required FeatureFlagReason Reason { get; init; }

    /// <summary>
    /// Where the definition came from.
    /// </summary>
    public required FeatureFlagSource Source { get; init; }

    /// <summary>
    /// Definition that was used, when one existed.
    /// </summary>
    public FeatureFlagDefinition? Definition { get; init; }

    /// <summary>
    /// Sticky 0–99 bucket used for percentage rollout, when rollout ran.
    /// </summary>
    public int? RolloutBucket { get; init; }

    /// <summary>
    /// When this evaluation was produced.
    /// </summary>
    public required DateTimeOffset EvaluatedAt { get; init; }
}
