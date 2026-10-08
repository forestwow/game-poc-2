using System.Numerics;

namespace Understudies.Core;

/// <summary>
/// A piece of applause on the floor: left where a critic fell to the magician's own card, there for a time, and
/// picked up by the magician alone.
/// </summary>
public sealed class Applause
{
    internal Applause(Vector2 position, int ticksLeft)
    {
        Position = position;
        TicksLeft = ticksLeft;
    }

    /// <summary>Where the piece lies: the middle of the circle of the critic that fell.</summary>
    public Vector2 Position { get; }

    /// <summary>
    /// What the piece has left of its time, in ticks: all of it on the tick it is dropped, and the tick that would
    /// leave none takes the piece away. The view dims the piece by it.
    /// </summary>
    public int TicksLeft { get; internal set; }
}

/// <summary>What an act's applause comes to: which of the tuning's two thresholds its share has reached.</summary>
public enum ApplauseBand
{
    /// <summary>No piece was picked up.</summary>
    None,

    /// <summary>Some was, and its share is under the first threshold.</summary>
    UnderTheFirst,

    /// <summary>The share has reached the first threshold and not the second.</summary>
    First,

    /// <summary>The share has reached the second threshold.</summary>
    Second,
}
