using System.Numerics;

namespace Understudies.Core;

/// <summary>
/// The cloud a Vanish leaves behind: a circle on the floor, there for a time, that stuns the critics it touches.
/// </summary>
public sealed class Cloud
{
    internal Cloud(Vector2 position, int ticksLeft)
    {
        Position = position;
        TicksLeft = ticksLeft;
    }

    /// <summary>The middle of the cloud's circle on the floor. A cloud stays where it was left.</summary>
    public Vector2 Position { get; }

    /// <summary>
    /// What the cloud has left of its time, in ticks: all of it on the tick it is left, and the tick that would
    /// leave none takes the cloud away. The view fades the cloud by it.
    /// </summary>
    public int TicksLeft { get; internal set; }
}
