namespace Understudies.Core;

/// <summary>
/// Turns the variable time of frames into a whole number of fixed ticks ("Fix Your Timestep"): time is accumulated
/// and each full tick's worth of it is one tick to run. One frame never asks for more than
/// <see cref="MaxTicksPerFrame"/>; the rest of a long hitch is dropped, so the game never tries to catch up for
/// seconds.
/// </summary>
public sealed class SimulationClock
{
    /// <summary>A quarter of a second.</summary>
    public const int MaxTicksPerFrame = Simulation.TicksPerSecond / 4;

    // Double, so that sums of frame times do not drift across tick boundaries.
    private const double TickSeconds = 1.0 / Simulation.TicksPerSecond;

    private double _accumulated;

    /// <summary>The fraction of the next tick already elapsed, in [0, 1]: how far to draw between two ticks.</summary>
    public float Alpha => (float)Math.Clamp(_accumulated / TickSeconds, 0.0, 1.0);

    /// <summary>Adds a frame's time and returns how many ticks to run now.</summary>
    public int Advance(double frameSeconds)
    {
        _accumulated += frameSeconds;
        int due = (int)(_accumulated / TickSeconds);
        _accumulated -= due * TickSeconds;
        return Math.Min(due, MaxTicksPerFrame);
    }
}
