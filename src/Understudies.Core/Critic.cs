using System.Numerics;

namespace Understudies.Core;

/// <summary>A critic on the stage: a circle on the floor that walks to the box office and strikes it.</summary>
public sealed class Critic
{
    internal Critic(int id, Vector2 position, float hitPoints)
    {
        Id = id;
        Position = position;
        PreviousPosition = position;
        HitPoints = hitPoints;
    }

    /// <summary>The same for the critic's whole life, and no other critic of the show has it.</summary>
    public int Id { get; }

    /// <summary>The middle of the critic's circle on the floor, after the last tick.</summary>
    public Vector2 Position { get; internal set; }

    /// <summary>Where the critic was before the last tick: the view draws between the two.</summary>
    public Vector2 PreviousPosition { get; internal set; }

    /// <summary>What the critic has left. A critic on the stage has more than nothing: at nothing it falls.</summary>
    public float HitPoints { get; internal set; }

    /// <summary>A cloud touched the critic a moment ago: it neither walks nor strikes.</summary>
    public bool IsStunned => TicksStunned > 0;

    internal int TicksToNextStrike { get; set; }

    internal int TicksStunned { get; set; }
}
