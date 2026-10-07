using System.Numerics;

namespace Understudies.Core;

/// <summary>
/// Every tunable number of the game, in world units and seconds.
/// ponytail: constants in code; T03 reads them from tuning.json.
/// </summary>
public static class Tuning
{
    /// <summary>The stage's floor: (0, 0) is its top-left corner and y grows downward.</summary>
    public static readonly Vector2 StageSize = new(48f, 27f);

    /// <summary>The middle of the box office's base on the floor.</summary>
    public static readonly Vector2 BoxOfficePosition = new(31f, 12f);

    /// <summary>The box office is a square this wide and this tall.</summary>
    public const float BoxOfficeSize = 4f;

    /// <summary>Where the magician stands when the show starts.</summary>
    public static readonly Vector2 MagicianMark = new(27f, 13f);

    /// <summary>Units per second.</summary>
    public const float MagicianSpeed = 9f;

    public const float MagicianRadius = 0.6f;
}
