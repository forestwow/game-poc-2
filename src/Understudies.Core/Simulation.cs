using System.Numerics;

namespace Understudies.Core;

/// <summary>Owns the state of the game and advances it one tick at a time.</summary>
public sealed class Simulation
{
    public const int TicksPerSecond = 60;

    /// <summary>The middle of the magician's circle on the floor, after the last tick.</summary>
    public Vector2 MagicianPosition { get; private set; } = Tuning.MagicianMark;

    /// <summary>Where the magician was before the last tick: the view draws between the two.</summary>
    public Vector2 MagicianPreviousPosition { get; private set; } = Tuning.MagicianMark;

    public void Step(MagicianInput input)
    {
        // A keyboard diagonal is no faster than a straight line; a stick pushed halfway stays at half speed.
        Vector2 move = input.Move;
        float lengthSquared = (move.X * move.X) + (move.Y * move.Y);
        if (lengthSquared > 1f)
        {
            move /= MathF.Sqrt(lengthSquared);
        }

        // The stage's edge stops the magician: the whole circle stays on the floor.
        var radius = new Vector2(Tuning.MagicianRadius);
        MagicianPreviousPosition = MagicianPosition;
        MagicianPosition = Vector2.Clamp(
            MagicianPosition + (move * (Tuning.MagicianSpeed / TicksPerSecond)),
            radius,
            Tuning.StageSize - radius);
    }
}
