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

    /// <summary>
    /// A show for the Vanish, which is asked for on every tick and so comes on the first. The mark is a unit inside
    /// the first door: the cloud lies on the critic that enters on that tick, and stuns it on the second. The Vanish
    /// goes further than the stage is long, so it ends on the bottom edge wherever it began, out of range of the
    /// door.
    /// </summary>
    private Tuning AVanishAtOnce => Tuning with
    {
        MagicianMark = Tuning.StageDoors[0] + new Vector2(1f, 0f),
        VanishDistance = 100f,
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

    /// <summary>The same for <see cref="AVanishAtOnce"/>.</summary>
    private static IEnumerable<TestCaseData> OneThingOfAVanishApart()
    {
        // The magician has vanished.
        yield return Case("the Vanish's cooldown", 1, t => t with { VanishCooldown = t.VanishCooldown * 2f });
        yield return Case("the time nothing hurts the magician", 1, t => t with { VanishInvulnerableTime = t.VanishInvulnerableTime * 2f });
        yield return Case("how long a cloud is still there", 1, t => t with { VanishCloudTime = t.VanishCloudTime * 2f });
        yield return Case("where a cloud is", 1, t => t with { MagicianMark = t.MagicianMark + Vector2.UnitY });

        // The cloud has stunned the first critic.
        yield return Case("how long a critic is still stunned", 2, t => t with { VanishStunTime = t.VanishStunTime * 2f });
    }

    [TestCaseSource(nameof(OneThingOfAVanishApart))]
    public void ComputeStateHash_TwoShowsOneThingOfAVanishApart_AreTwoHashes(int ticks, Func<Tuning, Tuning> change)
    {
        var vanish = new MagicianInput(Vector2.Zero, Vanish: true);

        Assert.That(
            Play(change(AVanishAtOnce), seed: 7, vanish, ticks),
            Is.Not.EqualTo(Play(AVanishAtOnce, seed: 7, vanish, ticks)));
    }

    [Test]
    public void ComputeStateHash_TwoMagiciansThatFaceTwoWays_AreTwoHashes()
    {
        // In the stage's top-left corner the edge stops a step to the left and a step up alike: the two shows differ
        // only in the way the magician was last asked to go, which is the way its next Vanish goes.
        Tuning inTheCorner = Tuning with { MagicianMark = new Vector2(Tuning.MagicianRadius) };
        var left = new MagicianInput(new Vector2(-1f, 0f));
        var up = new MagicianInput(new Vector2(0f, -1f));

        Assert.That(Play(inTheCorner, seed: 7, up, ticks: 1), Is.Not.EqualTo(Play(inTheCorner, seed: 7, left, ticks: 1)));
    }

    private static TestCaseData Case(string what, int ticks, Func<Tuning, Tuning> change) =>
        new TestCaseData(ticks, change).SetArgDisplayNames(what);

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
