using System.Numerics;

namespace Understudies.Core;

/// <summary>
/// A critic on the stage, or an enemy of another kind: a circle on the floor that walks to the box office and strikes
/// it, and turns on the magician when that comes near, if its kind does.
/// </summary>
public sealed class Critic
{
    internal Critic(int id, int kind, Vector2 position)
    {
        Id = id;
        Kind = kind;
        Position = position;
        PreviousPosition = position;
    }

    /// <summary>The same for the critic's whole life, and no other critic of the show has it.</summary>
    public int Id { get; }

    /// <summary>Its kind, by the kind's place in <see cref="Tuning.EnemyKinds"/>.</summary>
    public int Kind { get; }

    /// <summary>The middle of the critic's circle on the floor, after the last tick.</summary>
    public Vector2 Position { get; internal set; }

    /// <summary>
    /// Where the critic was before the last tick: the view draws between the two, and a card is thrown ahead of the
    /// critic by the step from there.
    /// </summary>
    public Vector2 PreviousPosition { get; internal set; }

    /// <summary>What the critic has left. A critic on the stage has more than nothing: at nothing it falls.</summary>
    public float HitPoints { get; internal set; }

    /// <summary>
    /// A cloud touched the critic a moment ago: it neither walks nor deals a blow, and does not turn on the magician.
    /// </summary>
    public bool IsStunned => TicksStunned > 0;

    internal int TicksToNextBlow { get; set; }

    internal int TicksStunned { get; set; }
}
