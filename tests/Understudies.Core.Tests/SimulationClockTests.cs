namespace Understudies.Core.Tests;

public class SimulationClockTests
{
    private const double Tick = 1.0 / Simulation.TicksPerSecond;

    [Test]
    public void Advance_APartOfATick_GivesNoTickUntilTheRestArrives()
    {
        var clock = new SimulationClock();

        Assert.That(clock.Advance(0.6 * Tick), Is.Zero);
        Assert.That(clock.Advance(0.6 * Tick), Is.EqualTo(1));
    }

    [Test]
    public void Advance_AFrameOfSeveralTicks_GivesThemAllAndTheFractionOfTheNext()
    {
        var clock = new SimulationClock();

        Assert.That(clock.Advance(3.25 * Tick), Is.EqualTo(3));
        Assert.That(clock.Alpha, Is.EqualTo(0.25f).Within(1e-4f));
    }

    [Test]
    public void Advance_ALongFrame_IsCappedAndTheRestOfItIsDropped()
    {
        var clock = new SimulationClock();

        Assert.That(clock.Advance(10.0), Is.EqualTo(SimulationClock.MaxTicksPerFrame));
        Assert.That(clock.Advance(0.0), Is.Zero);
    }
}
