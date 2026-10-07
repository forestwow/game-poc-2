using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Understudies.Core;

/// <summary>
/// Every tunable number of the game, in world units and seconds, as tuning.json holds them: a member here is the
/// key of the same name there in camelCase, and a point is written <c>{ "x": 1, "y": 2 }</c>.
/// </summary>
/// <param name="StageSize">The stage's floor: (0, 0) is its top-left corner and y grows downward.</param>
/// <param name="StageDoors">
/// The middle of each stage door, a point on the stage's edge, in the order the doors open. Critics enter at the
/// first.
/// </param>
/// <param name="StageDoorWidth">How much of the edge a door takes: a critic enters anywhere along it.</param>
/// <param name="BoxOfficePosition">The centre of the box office's circle on the floor.</param>
/// <param name="BoxOfficeSize">The box office is this wide and this tall; its circle's radius is half of it.</param>
/// <param name="BoxOfficeHitPoints">What the box office has when the show starts.</param>
/// <param name="MagicianMark">Where the magician stands when the show starts.</param>
/// <param name="MagicianSpeed">Units per second.</param>
/// <param name="MagicianRadius">The magician is a circle on the floor.</param>
/// <param name="MagicianHitPoints">What the magician has when the show starts.</param>
/// <param name="VanishDistance">How far the Vanish takes the magician, when no edge of the stage is in the way.</param>
/// <param name="VanishCooldown">Seconds from one Vanish to the next.</param>
/// <param name="VanishInvulnerableTime">Seconds from a Vanish in which nothing hurts the magician.</param>
/// <param name="VanishCloudRadius">The cloud a Vanish leaves is a circle on the floor.</param>
/// <param name="VanishCloudTime">Seconds the cloud is there.</param>
/// <param name="VanishStunTime">
/// Seconds a critic is stunned, counted from the last tick its circle touched a cloud.
/// </param>
/// <param name="ThrowRange">
/// The magician throws at a critic whose centre is no further than this, and a thrown card flies this far.
/// </param>
/// <param name="ThrowCooldown">Seconds from one throw to the next.</param>
/// <param name="ThrownCardSpeed">Units per second.</param>
/// <param name="ThrownCardDamage">The hit points one card takes off a critic.</param>
/// <param name="CriticSpeed">Units per second.</param>
/// <param name="CriticRadius">A critic is a circle on the floor.</param>
/// <param name="CriticHitPoints">What a critic has when it enters.</param>
/// <param name="CriticEntryInterval">Seconds from one critic entering to the next.</param>
/// <param name="CriticTurnRadius">
/// A critic whose centre is nearer than this to the magician's centre turns on the magician; any other walks to the
/// box office. A critic that has not turned does not hurt the magician, so a radius of nothing leaves the magician
/// alone.
/// </param>
/// <param name="CriticStrikeDamage">The hit points one strike takes off the box office.</param>
/// <param name="CriticTouchDamage">The hit points one touch takes off the magician.</param>
/// <param name="CriticBlowCooldown">
/// Seconds from one blow of a critic to its next, whether a blow is a strike on the box office or a touch that hurts
/// the magician.
/// </param>
public sealed record Tuning(
    Vector2 StageSize,
    IReadOnlyList<Vector2> StageDoors,
    float StageDoorWidth,
    Vector2 BoxOfficePosition,
    float BoxOfficeSize,
    float BoxOfficeHitPoints,
    Vector2 MagicianMark,
    float MagicianSpeed,
    float MagicianRadius,
    float MagicianHitPoints,
    float VanishDistance,
    float VanishCooldown,
    float VanishInvulnerableTime,
    float VanishCloudRadius,
    float VanishCloudTime,
    float VanishStunTime,
    float ThrowRange,
    float ThrowCooldown,
    float ThrownCardSpeed,
    float ThrownCardDamage,
    float CriticSpeed,
    float CriticRadius,
    float CriticHitPoints,
    float CriticEntryInterval,
    float CriticTurnRadius,
    float CriticStrikeDamage,
    float CriticTouchDamage,
    float CriticBlowCooldown)
{
    // ponytail: the serializer reads the types by reflection; a trimmed or AOT build needs a source-generated context.
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // A key that no member answers to is an error.
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        // A key written twice is an error: the last one would win in silence.
        AllowDuplicateProperties = false,
        // A Vector2's X and Y are fields.
        IncludeFields = true,
        // A missing key is an error at any depth. Marking every key here asks it of a point's x and y too, which
        // `required` members of this record could not.
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                static type =>
                {
                    foreach (JsonPropertyInfo key in type.Properties)
                    {
                        key.IsRequired = true;
                    }
                },
            },
        },
    };

    /// <summary>Reads the text of a tuning.json.</summary>
    /// <exception cref="JsonException">
    /// The text is not a tuning. For an unknown key and for a missing one the message names the key.
    /// </exception>
    public static Tuning Parse(string json)
    {
        Tuning tuning = JsonSerializer.Deserialize<Tuning>(json, Options) ?? throw new JsonException("The tuning is null.");

        // The rules take the first door for granted.
        return tuning.StageDoors is { Count: > 0 }
            ? tuning
            : throw new JsonException("'stageDoors' needs at least one door.");
    }
}
