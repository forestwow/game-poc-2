using System.Numerics;

namespace Understudies.Core.Tests;

public class CriticsTurnTests
{
    private const float Tolerance = 1e-4f;

    /// <summary>In the stage's top edge: where the one critic of <see cref="Scene"/> enters.</summary>
    private static readonly Vector2 Door = new(20f, 0f);

    /// <summary>A unit and a half to the right of the critic's way down the stage, five units below the door.</summary>
    private static readonly Vector2 Mark = new(21.5f, 5f);

    private static readonly MagicianInput Vanish = new(Vector2.Zero, Vanish: true);

    /// <summary>
    /// One critic and a magician that throws at nobody. The door has no width, so the critic enters on the first
    /// tick exactly at <see cref="Door"/>; from the second tick on it walks a unit a tick straight down the stage,
    /// to a box office it touches 17.5 units below the door. The second critic is an hour away. A critic turns on a
    /// magician nearer than three units: this one does on the fifth tick, three units below the door, where the
    /// magician is one and a half to the side and two further down, two and a half away. Each of the two circles is
    /// half a unit, so they touch on the sixth tick. A touch takes two of the magician's ten hit points and a strike
    /// one of the box office's hundred, and a critic deals a blow every half second.
    /// The curtain has no length, which is no curtain: these tests count their ticks from the first
    /// tick of an act, and the curtain has tests of its own.
    /// </summary>
    private Tuning Scene { get; } = CommittedTuning.Parse() with
    {
        CurtainTime = 0f,
        StageDoors = [Door],
        StageDoorWidth = 0f,
        BoxOfficePosition = Door + new Vector2(0f, 20f),
        BoxOfficeSize = 4f,
        BoxOfficeHitPoints = 100f,
        MagicianMark = Mark,
        MagicianRadius = 0.5f,
        MagicianHitPoints = 10f,
        ThrowRange = 0f,
        CriticSpeed = 60f,
        CriticRadius = 0.5f,
        CriticEntryInterval = 3600f,
        CriticTurnRadius = 3f,
        CriticStrikeDamage = 1f,
        CriticTouchDamage = 2f,
        CriticBlowCooldown = 0.5f,
    };

    [Test]
    public void Step_ACriticInsideTheTurnRadius_WalksAtTheMagician()
    {
        // Until then the magician is too far and the critic walks at the box office.
        var simulation = new Simulation(Scene, seed: 1);
        Run(simulation, ticks: 4);
        Assert.That(simulation.Critics[0].Position, Is.EqualTo(Door + new Vector2(0f, 3f)));

        simulation.Step(default);

        // A tick's step, one unit, along the line to the magician: 0.6 to the side and 0.8 down.
        Assert.That(Vector2.Distance(simulation.Critics[0].Position, new Vector2(20.6f, 3.8f)), Is.Zero.Within(Tolerance));
    }

    [Test]
    public void Step_ACriticThatHasTurned_StopsWhereItsCircleTouchesTheMagicians()
    {
        var simulation = new Simulation(Scene, seed: 1);

        Run(simulation, ticks: Simulation.TicksPerSecond);

        // Each of the two circles is half a unit.
        Critic critic = simulation.Critics[0];
        Assert.That(Vector2.Distance(critic.Position, Mark), Is.EqualTo(1f).Within(Tolerance));
        Assert.That(Vector2.Distance(critic.PreviousPosition, critic.Position), Is.Zero.Within(Tolerance));
    }

    [Test]
    public void Step_TheMagicianLeavesTheTurnRadius_TheCriticGoesBackToTheBoxOffice()
    {
        // The critic has turned and taken its first step at the magician, who then walks three units in a tick,
        // straight away from the critic's side of the stage: it is four units from the critic.
        Tuning tuning = Scene with { MagicianSpeed = 180f };
        var simulation = new Simulation(tuning, seed: 1);
        Run(simulation, ticks: 5);
        float toBoxOffice = Vector2.Distance(simulation.Critics[0].Position, tuning.BoxOfficePosition);

        simulation.Step(new MagicianInput(new Vector2(1f, 0f)));

        // The whole step of that tick, one unit, is straight at the box office: there is no step after the magician.
        Assert.That(
            Vector2.Distance(simulation.Critics[0].Position, tuning.BoxOfficePosition),
            Is.EqualTo(toBoxOffice - 1f).Within(Tolerance));
    }

    [Test]
    public void Step_TheMagicianWalksIntoACriticThatTouchesIt_TheCriticStaysWhereItStands()
    {
        // Half a unit in a tick, straight at the critic: the two circles are half over each other.
        Tuning tuning = Scene with { MagicianSpeed = 30f };
        var simulation = new Simulation(tuning, seed: 1);
        Run(simulation, ticks: Simulation.TicksPerSecond);
        Vector2 stood = simulation.Critics[0].Position;

        simulation.Step(new MagicianInput(new Vector2(-0.6f, -0.8f)));

        // Nothing blocks the magician, and it pushes nobody out of its way.
        Assert.That(Vector2.Distance(simulation.MagicianPosition, stood), Is.EqualTo(0.5f).Within(Tolerance));
        Assert.That(Vector2.Distance(simulation.Critics[0].Position, stood), Is.Zero.Within(Tolerance));
    }

    [Test]
    public void Step_ACriticTouchesTheMagician_HurtsItOncePerCooldown()
    {
        var simulation = new Simulation(Scene, seed: 1);
        Assert.That(simulation.MagicianHitPoints, Is.EqualTo(10f));

        // The first blow lands on the tick the critic arrives, and not before.
        Run(simulation, ticks: 5);
        Assert.That(simulation.MagicianHitPoints, Is.EqualTo(10f));
        Run(simulation, ticks: 1);
        Assert.That(simulation.MagicianHitPoints, Is.EqualTo(8f));

        // Half a second is 30 ticks.
        Run(simulation, ticks: 29);
        Assert.That(simulation.MagicianHitPoints, Is.EqualTo(8f));

        Run(simulation, ticks: 1);
        Assert.That(simulation.MagicianHitPoints, Is.EqualTo(6f));

        Run(simulation, ticks: 30);
        Assert.That(simulation.MagicianHitPoints, Is.EqualTo(4f));

        // The critic has turned from the box office: that has not been struck.
        Assert.That(simulation.BoxOfficeHitPoints, Is.EqualTo(100f));
    }

    [Test]
    public void Step_ACriticTurnsFromTheBoxOfficeToTheMagicianAndBack_EachBlowIsACooldownAfterItsLast()
    {
        // The magician stands a unit to the side of where the critic stops at the box office, so the critic there
        // touches both. It turns on the magician when the test gives it the radius to, and not before.
        Tuning turns = Scene with { MagicianMark = Door + new Vector2(1f, 17.5f) };
        Tuning keepsToTheBoxOffice = turns with { CriticTurnRadius = 0f };
        var simulation = new Simulation(keepsToTheBoxOffice, seed: 1);

        // The first strike, on the tick the critic arrives.
        Run(simulation, ticks: 19);
        Assert.That(simulation.BoxOfficeHitPoints, Is.EqualTo(99f));

        // Ten ticks later it turns on the magician, with twenty ticks left of the thirty to its next blow.
        Run(simulation, ticks: 10);
        simulation.Tuning = turns;
        Run(simulation, ticks: 19);
        Assert.That(simulation.MagicianHitPoints, Is.EqualTo(10f));

        Run(simulation, ticks: 1);
        Assert.That(simulation.MagicianHitPoints, Is.EqualTo(8f));
        Assert.That(simulation.BoxOfficeHitPoints, Is.EqualTo(99f));

        // And the other way round: it turns back at once, and its strike is the whole thirty ticks after the touch.
        simulation.Tuning = keepsToTheBoxOffice;
        Run(simulation, ticks: 29);
        Assert.That(simulation.BoxOfficeHitPoints, Is.EqualTo(99f));

        Run(simulation, ticks: 1);
        Assert.That(simulation.BoxOfficeHitPoints, Is.EqualTo(98f));
        Assert.That(simulation.MagicianHitPoints, Is.EqualTo(8f));
    }

    [Test]
    public void Step_ACriticTouchesTheMagicianWhileNothingHurtsIt_TakesNothingAndItsBlowStaysReady()
    {
        // A Vanish that goes nowhere and leaves no cloud: all there is to it is the quarter of a second, 15 ticks,
        // in which nothing hurts the magician. It is asked for on the tick the critic arrives.
        Tuning tuning = Scene with { VanishDistance = 0f, VanishCloudTime = 0f, VanishInvulnerableTime = 0.25f };
        var simulation = new Simulation(tuning, seed: 1);
        Run(simulation, ticks: 5);

        simulation.Step(Vanish);
        Assert.That(simulation.MagicianIsInvulnerable, Is.True);
        Assert.That(simulation.MagicianHitPoints, Is.EqualTo(10f));

        Run(simulation, ticks: 14);
        Assert.That(simulation.MagicianIsInvulnerable, Is.True);
        Assert.That(simulation.MagicianHitPoints, Is.EqualTo(10f));

        // The touch that took nothing was no blow: the blow lands on the first tick the magician can be hurt, and
        // not a cooldown after that touch.
        Run(simulation, ticks: 1);
        Assert.That(simulation.MagicianIsInvulnerable, Is.False);
        Assert.That(simulation.MagicianHitPoints, Is.EqualTo(8f));
    }

    [Test]
    public void Step_ACriticThatTouchesTheMagicianIsStunned_ItDoesNotHurtAndItsTimeToTheNextBlowStandsStill()
    {
        // A Vanish that goes nowhere and in which the magician can be hurt all the same. Its cloud, there for that
        // tick only, reaches two units and stuns for three quarters of a second: 45 ticks.
        Tuning tuning = Scene with
        {
            VanishDistance = 0f,
            VanishInvulnerableTime = 0f,
            VanishCloudRadius = 2f,
            VanishCloudTime = 1f / Simulation.TicksPerSecond,
            VanishStunTime = 0.75f,
        };
        var simulation = new Simulation(tuning, seed: 1);

        // The first blow, on the tick the critic arrives; then ten ticks more, twenty before the second.
        Run(simulation, ticks: 6);
        Assert.That(simulation.MagicianHitPoints, Is.EqualTo(8f));
        Run(simulation, ticks: 10);
        simulation.Step(Vanish);
        Assert.That(simulation.Critics[0].IsStunned, Is.True);
        Assert.That(simulation.MagicianIsInvulnerable, Is.False);

        // Stunned on the tick of the Vanish and the 44 after it: the blow that was twenty ticks away has not come.
        Run(simulation, ticks: 44);
        Assert.That(simulation.Critics[0].IsStunned, Is.True);
        Assert.That(simulation.MagicianHitPoints, Is.EqualTo(8f));

        // And it is still twenty ticks away when the stun ends.
        Run(simulation, ticks: 19);
        Assert.That(simulation.Critics[0].IsStunned, Is.False);
        Assert.That(simulation.MagicianHitPoints, Is.EqualTo(8f));

        Run(simulation, ticks: 1);
        Assert.That(simulation.MagicianHitPoints, Is.EqualTo(6f));
    }

    [Test]
    public void Step_ACrowdThatHasTurnedOnTheMagicianBesideTheBoxOfficeIsStunned_NoStunnedCriticStandsInTheBoxOffice()
    {
        // The committed stage with a critic every second and blows that take nothing, so the show goes on: forty
        // critics crowd round the magician on its mark beside the box office, and push one another into the box
        // office, where one that has turned is left. The Vanish goes nowhere, so its cloud lies on them with the
        // magician still in their midst. A stunned critic has not turned: it is put back out of the box office.
        // No curtain: the forty seconds are counted from the first tick of the act.
        Tuning tuning = CommittedTuning.Parse() with
        {
            CurtainTime = 0f,
            ThrowRange = 0f,
            VanishDistance = 0f,
            CriticEntryInterval = 1f,
            CriticStrikeDamage = 0f,
            CriticTouchDamage = 0f,
        };
        var simulation = new Simulation(tuning, seed: 1);
        Run(simulation, ticks: 40 * Simulation.TicksPerSecond);

        float touching = (tuning.BoxOfficeSize / 2f) + tuning.CriticRadius;
        float FromBoxOffice(Critic critic) => Vector2.Distance(critic.Position, tuning.BoxOfficePosition);
        Assert.That(simulation.Critics.Count(critic => FromBoxOffice(critic) < touching - 0.1f), Is.GreaterThan(0));

        simulation.Step(Vanish);
        Assert.That(simulation.Critics.Count(critic => critic.IsStunned), Is.GreaterThan(5));

        for (int i = 0; i < Simulation.TicksPerSecond; i++)
        {
            Assert.That(
                simulation.Critics.Where(critic => critic.IsStunned).Select(FromBoxOffice),
                Has.All.GreaterThan(touching - 1e-3f));
            simulation.Step(default);
        }
    }

    [Test]
    public void Step_TheMagiciansHitPointsRunOut_TheMagicianHasFallenAndTheShowIsNotClosed()
    {
        // Two touches are not enough and the third is more than enough.
        var simulation = new Simulation(Scene with { MagicianHitPoints = 5f }, seed: 1);

        Run(simulation, ticks: 6 + 30);
        Assert.That(simulation.MagicianHitPoints, Is.EqualTo(1f));
        Assert.That(simulation.MagicianHasFallen, Is.False);
        Assert.That(simulation.ShowClosed, Is.False);

        // Never less than nothing. And it is the magician that fell: the box office stands as it stood.
        Run(simulation, ticks: 30);
        Assert.That(simulation.MagicianHitPoints, Is.Zero);
        Assert.That(simulation.MagicianHasFallen, Is.True);
        Assert.That(simulation.ShowClosed, Is.False);
        Assert.That(simulation.BoxOfficeHitPoints, Is.EqualTo(100f));
    }

    [Test]
    public void Step_ACriticHurtsTheMagician_ItIsReportedWhereTheMagicianStandsForThatTickOnly()
    {
        var simulation = new Simulation(Scene, seed: 1);
        Run(simulation, ticks: 5);
        Assert.That(simulation.Events, Is.Empty);

        simulation.Step(default);
        Assert.That(simulation.Events, Is.EqualTo(new[] { new TickEvent(TickEventKind.MagicianHurt, Mark) }));

        simulation.Step(default);
        Assert.That(simulation.Events, Is.Empty);
    }

    [Test]
    public void Step_SeveralBlowsAreReadyOnTheTickTheMagicianFalls_OnlyTheOneThatFellsItLands()
    {
        // Critics gather round a magician that nothing hurts for a second, each keeping its blow ready, and one
        // blow is all the magician can take. On the first tick it can be hurt the first of them fells it: it is
        // not there for a critic from then on, and the others deal nothing.
        Tuning tuning = Scene with
        {
            StageDoorWidth = 4f,
            CriticEntryInterval = 0.25f,
            CriticTurnRadius = 30f,
            MagicianHitPoints = 2f,
            VanishInvulnerableTime = 1f,
            VanishCloudTime = 0f,
        };
        var simulation = new Simulation(tuning, seed: 1);
        simulation.Step(Vanish);

        for (int i = 0; i < 2 * Simulation.TicksPerSecond && !simulation.MagicianHasFallen; i++)
        {
            simulation.Step(default);
        }

        Assert.That(simulation.MagicianHasFallen, Is.True);
        float touch = tuning.MagicianRadius + tuning.CriticRadius + Tolerance;
        Assert.That(
            simulation.Critics.Count(critic => Vector2.Distance(critic.Position, simulation.MagicianPosition) <= touch),
            Is.GreaterThan(1),
            "The scene needs more than one critic touching the magician when it falls.");
        Assert.That(
            simulation.Events,
            Is.EqualTo(new[]
            {
                new TickEvent(TickEventKind.MagicianHurt, simulation.MagicianPosition),
                new TickEvent(TickEventKind.MagicianFell, simulation.MagicianPosition),
            }));
    }

    [Test]
    public void Step_NothingHurtsTheMagician_TheBoxOfficeIsStruckAllTheSame()
    {
        // No critic turns, and the one critic reaches the box office on the nineteenth tick, inside the second in
        // which nothing hurts the magician: that moment is the magician's and shields nothing else.
        Tuning tuning = Scene with { CriticTurnRadius = 0f, VanishInvulnerableTime = 1f, VanishCloudTime = 0f };
        var simulation = new Simulation(tuning, seed: 1);
        simulation.Step(Vanish);

        Run(simulation, ticks: 18);

        Assert.That(simulation.MagicianIsInvulnerable, Is.True);
        Assert.That(simulation.BoxOfficeHitPoints, Is.EqualTo(99f));
    }

    private static void Run(Simulation simulation, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(default);
        }
    }
}
