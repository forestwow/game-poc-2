using System.Numerics;

namespace Understudies.Core.Tests;

public class StateHashTests
{
    private Tuning Tuning { get; } = CommittedTuning.Parse();

    /// <summary>
    /// A show in which everything happens at once: the box office stands just inside the first door, so a critic
    /// strikes it within half a second of entering, and the magician stands in range of the door and throws on the
    /// second tick, at critics that outlast the test.
    /// </summary>
    private Tuning EverythingAtOnce => Tuning with
    {
        BoxOfficePosition = Tuning.StageDoors[0] + new Vector2(4f, 0f),
        MagicianMark = Tuning.StageDoors[0] + new Vector2(8f, 0f),
        CriticHitPoints = 1000f,
    };

    [Test]
    public void ComputeStateHash_TheSameSeedAndTheSameInputsPlayedTwice_EndInTheSameHash()
    {
        var left = new MagicianInput(new Vector2(-1f, 0f));

        ulong one = Play(Tuning, seed: 7, left);
        ulong other = Play(Tuning, seed: 7, left);

        Assert.That(other, Is.EqualTo(one));
    }

    [Test]
    public void ComputeStateHash_AnotherSeed_IsAnotherHashBeforeTheFirstTick()
    {
        // Nothing has been drawn from either generator yet: its state is all the two shows differ in.
        var one = new Simulation(Tuning, seed: 1);
        var other = new Simulation(Tuning, seed: 2);

        Assert.That(other.ComputeStateHash(), Is.Not.EqualTo(one.ComputeStateHash()));
    }

    /// <summary>
    /// Each case is a change to one number of <see cref="EverythingAtOnce"/> and the tick by which it has changed
    /// the one thing it is named for and nothing else in the state: the hash must see that one thing.
    /// </summary>
    private static IEnumerable<TestCaseData> OneThingApart()
    {
        static TestCaseData Case(string what, int ticks, Func<Tuning, Tuning> change) =>
            new TestCaseData(ticks, change).SetArgDisplayNames(what);

        yield return Case("where the magician stands", 0, t => t with { MagicianMark = t.MagicianMark + Vector2.One });
        yield return Case("the box office's hit points", 0, t => t with { BoxOfficeHitPoints = t.BoxOfficeHitPoints + 1f });

        // The first critic has entered.
        yield return Case("the time to the next critic", 1, t => t with { CriticEntryInterval = t.CriticEntryInterval * 2f });
        yield return Case("where a critic stands", 1, t => t with { StageDoors = [t.StageDoors[0] + Vector2.UnitY] });
        yield return Case("a critic's hit points", 1, t => t with { CriticHitPoints = t.CriticHitPoints + 1f });

        // The first card has been thrown.
        yield return Case("the throw's cooldown", 2, t => t with { ThrowCooldown = t.ThrowCooldown * 2f });
        yield return Case("how far a card may still fly", 2, t => t with { ThrowRange = t.ThrowRange + 0.5f });

        // And has flown one step. The faster card is further on with less of its range left: the two go together.
        yield return Case("where a card is", 3, t => t with { ThrownCardSpeed = t.ThrownCardSpeed * 2f });

        // The first critic has struck once, and not twice on either cooldown.
        yield return Case("a critic's time to its next strike", 60, t => t with { CriticStrikeCooldown = t.CriticStrikeCooldown * 2f });
    }

    [TestCaseSource(nameof(OneThingApart))]
    public void ComputeStateHash_TwoShowsOneThingApart_AreTwoHashes(int ticks, Func<Tuning, Tuning> change)
    {
        Assert.That(
            Play(change(EverythingAtOnce), seed: 7, input: default, ticks),
            Is.Not.EqualTo(Play(EverythingAtOnce, seed: 7, input: default, ticks)));
    }

    /// <summary>The hash a show ends in when <paramref name="input"/> is held all through it.</summary>
    private static ulong Play(Tuning tuning, ulong seed, MagicianInput input, int ticks = 30 * Simulation.TicksPerSecond)
    {
        var simulation = new Simulation(tuning, seed);
        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(input);
        }

        return simulation.ComputeStateHash();
    }
}
