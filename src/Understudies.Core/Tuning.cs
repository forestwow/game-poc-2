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
/// <param name="BoxOfficePosition">The middle of the box office's base on the floor.</param>
/// <param name="BoxOfficeSize">The box office is a square this wide and this tall.</param>
/// <param name="MagicianMark">Where the magician stands when the show starts.</param>
/// <param name="MagicianSpeed">Units per second.</param>
/// <param name="MagicianRadius">The magician is a circle on the floor.</param>
public sealed record Tuning(
    Vector2 StageSize,
    Vector2 BoxOfficePosition,
    float BoxOfficeSize,
    Vector2 MagicianMark,
    float MagicianSpeed,
    float MagicianRadius)
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
    public static Tuning Parse(string json) =>
        JsonSerializer.Deserialize<Tuning>(json, Options) ?? throw new JsonException("The tuning is null.");
}
