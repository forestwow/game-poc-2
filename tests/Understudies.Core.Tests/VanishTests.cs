using System.Numerics;

namespace Understudies.Core.Tests;

public class VanishTests
{
    private const float Tolerance = 1e-4f;

    /// <summary>In the middle of the floor: further from every edge than a Vanish goes.</summary>
    private static readonly Vector2 Mark = new(24f, 13f);

    private static readonly MagicianInput Vanish = new(Vector2.Zero, Vanish: true);

    /// <summary>In the stage's top edge: where the critic of <see cref="ACriticWalksDown"/> enters.</summary>
    private static readonly Vector2 Door = new(20f, 0f);

    /// <summary>
    /// The committed numbers with a magician that throws at nobody and that no critic turns on, and a Vanish of six
    /// units, ready again two seconds later, with a quarter of a second in which nothing hurts and a cloud that is
    /// there for half a second.
    /// </summary>
    private Tuning Scene { get; } = CommittedTuning.Parse() with
    {
        MagicianMark = Mark,
        ThrowRange = 0f,
        CriticTurnRadius = 0f,
        VanishDistance = 6f,
        VanishCooldown = 2f,
        VanishInvulnerableTime = 0.25f,
        VanishCloudTime = 0.5f,
    };

    /// <summary>
    /// <see cref="Scene"/> with one critic. The door has no width, so the critic enters on the first tick exactly
    /// at <see cref="Door"/>; from the second tick on it walks a unit a tick straight down the stage, to a box
    /// office it touches 17.5 units below the door. The second critic is an hour away. A cloud reaches two units
    /// from its middle and is there for the tick of its Vanish only, so it stuns once, for half a second. Each test
    /// puts the mark where its cloud is to be; the Vanish itself goes down the stage, as it does before the first
    /// move. The stage has no back wall: its floor starts at the top edge, where the door is, so the mark can be
    /// near enough to the door for the cloud to reach it, where a wall would not let the magician stand.
    /// </summary>
    private Tuning ACriticWalksDown => Scene with
    {
        StageFloorTop = 0f,
        StageDoors = [Door],
        StageDoorWidth = 0f,
        BoxOfficePosition = Door + new Vector2(0f, 20f),
        BoxOfficeSize = 4f,
        CriticSpeed = 60f,
        CriticRadius = 0.5f,
        CriticEntryInterval = 3600f,
        VanishCloudRadius = 2f,
        VanishCloudTime = 1f / Simulation.TicksPerSecond,
        VanishStunTime = 0.5f,
    };

    [Test]
    public void Step_TheVanishWhileWalking_TheMagicianIsAtOnceItsDistanceFurtherThatWay()
    {
        var simulation = new Simulation(Scene, seed: 1);

        simulation.Step(new MagicianInput(new Vector2(1f, 0f), Vanish: true));

        // The Vanish is the tick's whole move: no step of the walk is added to it.
        Assert.That(simulation.MagicianPosition, Is.EqualTo(Mark + new Vector2(6f, 0f)));
    }

    // A keyboard diagonal, a stick pushed halfway, and the stick pushed while a key is held.
    [TestCase(1f, 1f)]
    [TestCase(0f, -0.5f)]
    [TestCase(-2f, 0f)]
    public void Step_TheVanishWhileWalking_GoesItsWholeDistanceHoweverHardTheMoveIsPushed(float x, float y)
    {
        var simulation = new Simulation(Scene, seed: 1);
        var move = new Vector2(x, y);

        simulation.Step(new MagicianInput(move, Vanish: true));

        Vector2 gone = simulation.MagicianPosition - Mark;
        Assert.That(gone.Length(), Is.EqualTo(6f).Within(Tolerance));
        Assert.That(Vector2.Distance(Vector2.Normalize(gone), Vector2.Normalize(move)), Is.Zero.Within(Tolerance));
    }

    [Test]
    public void Step_TheVanishWhileStanding_GoesTheWayTheMagicianLastWalked()
    {
        var simulation = new Simulation(Scene, seed: 1);
        simulation.Step(new MagicianInput(new Vector2(1f, 0f)));
        simulation.Step(new MagicianInput(new Vector2(0f, -1f)));
        Run(simulation, ticks: 5);
        Vector2 stood = simulation.MagicianPosition;

        simulation.Step(Vanish);

        Assert.That(simulation.MagicianPosition, Is.EqualTo(stood + new Vector2(0f, -6f)));
    }

    [Test]
    public void Step_TheVanishBeforeTheFirstMove_GoesDownTheStageTowardsTheAudience()
    {
        var simulation = new Simulation(Scene, seed: 1);

        simulation.Step(Vanish);

        Assert.That(simulation.MagicianPosition, Is.EqualTo(Mark + new Vector2(0f, 6f)));
    }

    [Test]
    public void Step_TheVanishTowardsAnEdgeNearerThanItsDistance_EndsWithTheWholeCircleOnTheStage()
    {
        Tuning tuning = Scene with { MagicianMark = new Vector2(Scene.StageSize.X - 3f, 13f) };
        var simulation = new Simulation(tuning, seed: 1);

        simulation.Step(new MagicianInput(new Vector2(1f, 0f), Vanish: true));

        Assert.That(simulation.MagicianPosition, Is.EqualTo(new Vector2(tuning.StageSize.X - tuning.MagicianRadius, 13f)));
    }

    [Test]
    public void Step_TheVanishUpTheStageFromNearerTheBackWallThanItsDistance_EndsAtTheFootOfTheWall()
    {
        // The floor starts three units down the stage and the mark is four units below that; the magician's circle
        // reaches half a unit from its middle.
        Tuning tuning = Scene with { StageFloorTop = 3f, MagicianRadius = 0.5f, MagicianMark = new Vector2(24f, 7f) };
        var simulation = new Simulation(tuning, seed: 1);

        simulation.Step(new MagicianInput(new Vector2(0f, -1f), Vanish: true));

        Assert.That(simulation.MagicianPosition, Is.EqualTo(new Vector2(24f, 3.5f)));
    }

    [Test]
    public void Step_TheVanish_LeavesTheViewNothingToDrawBetweenTheTwoPlaces()
    {
        var simulation = new Simulation(Scene, seed: 1);

        simulation.Step(Vanish);

        Assert.That(simulation.MagicianPosition, Is.Not.EqualTo(Mark));
        Assert.That(simulation.MagicianPreviousPosition, Is.EqualTo(simulation.MagicianPosition));
    }

    [Test]
    public void Step_ASecondPressWhileTheCooldownRuns_IsRefused()
    {
        // Two seconds are 120 ticks: the Vanish of the first tick is ready again on tick 121.
        var simulation = new Simulation(Scene, seed: 1);
        simulation.Step(Vanish);
        Vector2 stood = simulation.MagicianPosition;

        for (int i = 0; i < 119; i++)
        {
            simulation.Step(Vanish);
        }

        Assert.That(simulation.MagicianPosition, Is.EqualTo(stood));

        simulation.Step(Vanish);
        Assert.That(simulation.MagicianPosition, Is.EqualTo(stood + new Vector2(0f, 6f)));
    }

    [Test]
    public void Step_ARefusedPress_IsNotKeptForLaterAndLeavesTheWalkAsItIs()
    {
        var simulation = new Simulation(Scene, seed: 1);
        simulation.Step(Vanish);
        Vector2 stood = simulation.MagicianPosition;

        // The press is refused and the magician walks its step of the tick, as if nothing had been pressed.
        simulation.Step(new MagicianInput(new Vector2(1f, 0f), Vanish: true));
        Vector2 walked = simulation.MagicianPosition - stood;
        Assert.That(walked.X, Is.EqualTo(Scene.MagicianSpeed / Simulation.TicksPerSecond).Within(Tolerance));
        Assert.That(simulation.MagicianPreviousPosition, Is.EqualTo(stood));

        // Long after the cooldown, nothing has come of it.
        Run(simulation, ticks: 5 * Simulation.TicksPerSecond);
        Assert.That(simulation.MagicianPosition, Is.EqualTo(stood + walked));
    }

    [Test]
    public void VanishCooldownLeft_IsAllOfItOnTheTickOfTheVanishAndNothingWhenTheNextIsReady()
    {
        var simulation = new Simulation(Scene, seed: 1);
        Assert.That(simulation.VanishCooldownLeft, Is.Zero);

        simulation.Step(Vanish);
        Assert.That(simulation.VanishCooldownLeft, Is.EqualTo(1f));

        Run(simulation, ticks: 30);
        Assert.That(simulation.VanishCooldownLeft, Is.EqualTo(0.75f));

        Run(simulation, ticks: 89);
        Assert.That(simulation.VanishCooldownLeft, Is.GreaterThan(0f));

        // Ready, and it stays ready until it is asked for.
        Run(simulation, ticks: 1);
        Assert.That(simulation.VanishCooldownLeft, Is.Zero);
        Run(simulation, ticks: 30);
        Assert.That(simulation.VanishCooldownLeft, Is.Zero);
    }

    [Test]
    public void VanishCooldownLeft_AfterNewTuningWithAShorterCooldown_IsNeverMoreThanAllOfIt()
    {
        // The countdown that runs is the old one, longer than the whole of the new.
        var simulation = new Simulation(Scene, seed: 1);
        simulation.Step(Vanish);

        simulation.Tuning = Scene with { VanishCooldown = 1f };
        Assert.That(simulation.VanishCooldownLeft, Is.EqualTo(1f));

        simulation.Tuning = Scene with { VanishCooldown = 0f };
        Assert.That(simulation.VanishCooldownLeft, Is.EqualTo(1f));
    }

    [Test]
    public void MagicianIsInvulnerable_ForItsTimeFromTheTickOfTheVanish()
    {
        // A quarter of a second is 15 ticks: the tick of the Vanish and the 14 after it.
        var simulation = new Simulation(Scene, seed: 1);
        Run(simulation, ticks: 3);
        Assert.That(simulation.MagicianIsInvulnerable, Is.False);

        simulation.Step(Vanish);
        Assert.That(simulation.MagicianIsInvulnerable, Is.True);

        // The button held down: every press after the first is refused, and a refused press renews nothing.
        for (int i = 0; i < 14; i++)
        {
            simulation.Step(Vanish);
        }

        Assert.That(simulation.MagicianIsInvulnerable, Is.True);

        Run(simulation, ticks: 1);
        Assert.That(simulation.MagicianIsInvulnerable, Is.False);
    }

    [Test]
    public void Step_TheVanish_IsReportedAtThePlaceTheMagicianLeftForThatTickOnly()
    {
        var simulation = new Simulation(Scene, seed: 1);
        simulation.Step(Vanish);
        Assert.That(simulation.Events, Is.EqualTo(new[] { new TickEvent(TickEventKind.Vanish, Mark) }));

        // A refused press is no Vanish: no event, and no second cloud.
        simulation.Step(Vanish);
        Assert.That(simulation.Events, Is.Empty);
        Assert.That(simulation.Clouds, Has.Count.EqualTo(1));
    }

    [Test]
    public void Step_TheVanish_LeavesACloudWhereTheMagicianStood()
    {
        var simulation = new Simulation(Scene, seed: 1);
        simulation.Step(new MagicianInput(new Vector2(1f, 0f)));
        Vector2 stood = simulation.MagicianPosition;
        Assert.That(simulation.Clouds, Is.Empty);

        simulation.Step(Vanish);

        Assert.That(simulation.Clouds.Select(cloud => cloud.Position), Is.EqualTo(new[] { stood }));
        Assert.That(simulation.MagicianPosition, Is.Not.EqualTo(stood));
    }

    [Test]
    public void Step_ACloud_IsGoneWhenItsTimeIsOver()
    {
        // Half a second is 30 ticks: the tick of the Vanish and the 29 after it.
        var simulation = new Simulation(Scene, seed: 1);
        simulation.Step(Vanish);
        Assert.That(simulation.Clouds.Select(cloud => cloud.TicksLeft), Is.EqualTo(new[] { 30 }));

        Run(simulation, ticks: 29);
        Assert.That(simulation.Clouds.Select(cloud => cloud.TicksLeft), Is.EqualTo(new[] { 1 }));

        Run(simulation, ticks: 1);
        Assert.That(simulation.Clouds, Is.Empty);
    }

    [Test]
    public void Step_TwoCloudsAtOnce_AreKeptInTheOrderTheyWereLeftAndEachGoesWhenItsOwnTimeIsOver()
    {
        // A Vanish every tenth of a second: the second cloud is left on the seventh tick.
        var simulation = new Simulation(Scene with { VanishCooldown = 0.1f }, seed: 1);
        simulation.Step(Vanish);
        Run(simulation, ticks: 5);
        simulation.Step(Vanish);
        Assert.That(
            simulation.Clouds.Select(cloud => cloud.Position),
            Is.EqualTo(new[] { Mark, Mark + new Vector2(0f, 6f) }));

        Run(simulation, ticks: 24);
        Assert.That(
            simulation.Clouds.Select(cloud => cloud.Position),
            Is.EqualTo(new[] { Mark + new Vector2(0f, 6f) }));
    }

    [Test]
    public void Step_ACloudTimeOfNothing_TheVanishLeavesNoCloud()
    {
        var simulation = new Simulation(Scene with { VanishCloudTime = 0f }, seed: 1);

        simulation.Step(Vanish);

        Assert.That(simulation.MagicianPosition, Is.EqualTo(Mark + new Vector2(0f, 6f)));
        Assert.That(simulation.Clouds, Is.Empty);
    }

    [Test]
    public void Step_ACriticsCircleTouchesACloud_ItStandsStunnedUntilTheStunEnds()
    {
        // The critic's centre is outside the cloud's two units, and its circle reaches in. The cloud is there on the
        // second tick only, and half a second is 30 ticks: that tick and the 29 after it.
        Simulation simulation = ACriticAtTheDoor(ACriticWalksDown with { MagicianMark = Door + new Vector2(0f, 2.25f) });

        simulation.Step(Vanish);
        Assert.That(simulation.Critics[0].IsStunned, Is.True);
        Assert.That(simulation.Critics[0].Position, Is.EqualTo(Door));

        Run(simulation, ticks: 29);
        Assert.That(simulation.Critics[0].IsStunned, Is.True);
        Assert.That(simulation.Critics[0].Position, Is.EqualTo(Door));
        Assert.That(simulation.Critics[0].PreviousPosition, Is.EqualTo(Door));

        Run(simulation, ticks: 1);
        Assert.That(simulation.Critics[0].IsStunned, Is.False);
        Assert.That(simulation.Critics[0].Position, Is.EqualTo(Door + new Vector2(0f, 1f)));
    }

    [Test]
    public void Step_TwoCloudsAtOnce_TheSecondStunsAsTheFirstDoes()
    {
        // Two Vanishes to the right, a tick apart, each cloud there for five ticks. The first cloud is six units
        // to the critic's left and touches nobody; the second is left under the critic, which has walked one step.
        Tuning tuning = ACriticWalksDown with
        {
            MagicianMark = Door + new Vector2(-6f, 2.25f),
            VanishCooldown = 1f / Simulation.TicksPerSecond,
            VanishCloudTime = 5f / Simulation.TicksPerSecond,
        };
        Simulation simulation = ACriticAtTheDoor(tuning);
        var vanishRight = new MagicianInput(new Vector2(1f, 0f), Vanish: true);

        simulation.Step(vanishRight);
        Assert.That(simulation.Critics[0].IsStunned, Is.False);
        Assert.That(simulation.Critics[0].Position, Is.EqualTo(Door + new Vector2(0f, 1f)));

        simulation.Step(vanishRight);
        Assert.That(simulation.Clouds, Has.Count.EqualTo(2));
        Assert.That(simulation.Critics[0].IsStunned, Is.True);
        Assert.That(simulation.Critics[0].Position, Is.EqualTo(Door + new Vector2(0f, 1f)));
    }

    [Test]
    public void Step_ACriticsCircleIsClearOfTheCloud_ItWalksOn()
    {
        Simulation simulation = ACriticAtTheDoor(ACriticWalksDown with { MagicianMark = Door + new Vector2(0f, 2.75f) });

        simulation.Step(Vanish);

        Assert.That(simulation.Critics[0].IsStunned, Is.False);
        Assert.That(simulation.Critics[0].Position, Is.EqualTo(Door + new Vector2(0f, 1f)));
    }

    [Test]
    public void Step_ACriticStaysInACloud_TheStunIsCountedFromTheCloudsLastTick()
    {
        // The cloud is there for ten ticks, the second to the eleventh, and the stun's 30 ticks are counted from
        // each of them anew: the critic stands until the fortieth tick and walks on the forty-first.
        Tuning tuning = ACriticWalksDown with
        {
            MagicianMark = Door + new Vector2(0f, 2.25f), VanishCloudTime = 10f / Simulation.TicksPerSecond,
        };
        Simulation simulation = ACriticAtTheDoor(tuning);
        simulation.Step(Vanish);

        Run(simulation, ticks: 38);
        Assert.That(simulation.Clouds, Is.Empty);
        Assert.That(simulation.Critics[0].IsStunned, Is.True);
        Assert.That(simulation.Critics[0].Position, Is.EqualTo(Door));

        Run(simulation, ticks: 1);
        Assert.That(simulation.Critics[0].IsStunned, Is.False);
        Assert.That(simulation.Critics[0].Position, Is.EqualTo(Door + new Vector2(0f, 1f)));
    }

    [Test]
    public void Step_ACriticAtTheBoxOfficeIsStunned_ItDoesNotStrikeAndItsTimeToTheNextStrikeStandsStill()
    {
        // The critic stops 17.5 units below the door; the magician waits two units to the side of there. A strike
        // every 30 ticks, and a stun of 45.
        Tuning tuning = ACriticWalksDown with
        {
            MagicianMark = Door + new Vector2(2f, 17.5f),
            BoxOfficeHitPoints = 100f,
            CriticStrikeDamage = 1f,
            CriticBlowCooldown = 0.5f,
            VanishStunTime = 0.75f,
        };
        var simulation = new Simulation(tuning, seed: 1);
        Run(simulation, ticks: 18);
        Assert.That(simulation.BoxOfficeHitPoints, Is.EqualTo(100f));

        // The first strike, on the tick the critic arrives; then ten ticks more, twenty before the second.
        Run(simulation, ticks: 1);
        Assert.That(simulation.BoxOfficeHitPoints, Is.EqualTo(99f));
        Run(simulation, ticks: 10);
        simulation.Step(Vanish);
        Assert.That(simulation.Critics[0].IsStunned, Is.True);

        // Stunned on the tick of the Vanish and the 44 after it: the strike that was twenty ticks away has not come.
        Run(simulation, ticks: 44);
        Assert.That(simulation.Critics[0].IsStunned, Is.True);
        Assert.That(simulation.BoxOfficeHitPoints, Is.EqualTo(99f));

        // And it is still twenty ticks away when the stun ends.
        Run(simulation, ticks: 19);
        Assert.That(simulation.Critics[0].IsStunned, Is.False);
        Assert.That(simulation.BoxOfficeHitPoints, Is.EqualTo(99f));

        Run(simulation, ticks: 1);
        Assert.That(simulation.BoxOfficeHitPoints, Is.EqualTo(98f));
    }

    [Test]
    public void Step_TwoStunnedCriticsOnOnePoint_ArePushedApartAndDoNotWalk()
    {
        // A cloud that lasts lies on the door. The second critic enters a tick after the first, on the same point.
        Tuning tuning = ACriticWalksDown with { MagicianMark = Door + new Vector2(0f, 2.25f), VanishCloudTime = 10f };
        var simulation = new Simulation(tuning with { CriticEntryInterval = 1f / Simulation.TicksPerSecond }, seed: 1);
        simulation.Step(Vanish);
        simulation.Tuning = tuning;
        Run(simulation, ticks: 1);
        Assert.That(simulation.Critics.Select(critic => critic.Position), Is.EqualTo(new[] { Door, Door }));

        simulation.Step(default);

        // Half the overlap each along the stage's width, as two that walk are pushed, and not a step down it.
        Assert.That(simulation.Critics.Select(critic => critic.IsStunned), Is.EqualTo(new[] { true, true }));
        Assert.That(simulation.Critics[0].Position.X, Is.EqualTo(Door.X - 0.5f).Within(Tolerance));
        Assert.That(simulation.Critics[1].Position.X, Is.EqualTo(Door.X + 0.5f).Within(Tolerance));
        Assert.That(simulation.Critics.Select(critic => critic.Position.Y), Is.EqualTo(new[] { 0f, 0f }));
    }

    [Test]
    public void Step_ACardReachesAStunnedCritic_HurtsItAsItHurtsOneThatWalks()
    {
        // The magician ends its Vanish 8.25 units below the critic, throws on that tick, and the card flies a unit
        // a tick: it is there within ten ticks, while the stun lasts thirty.
        Tuning tuning = ACriticWalksDown with
        {
            MagicianMark = Door + new Vector2(0f, 2.25f),
            ThrowRange = 9f,
            ThrowCooldown = 3600f,
            ThrownCardSpeed = 60f,
            ThrownCardDamage = 1f,
            CriticHitPoints = 3f,
        };
        Simulation simulation = ACriticAtTheDoor(tuning);
        simulation.Step(Vanish);

        Run(simulation, ticks: 10);

        Assert.That(simulation.Critics[0].IsStunned, Is.True);
        Assert.That(simulation.Critics[0].HitPoints, Is.EqualTo(2f));
    }

    [Test]
    public void Step_ACrowdAtTheBoxOfficeIsStunned_NoCriticIsPushedIntoIt()
    {
        // The committed stage with a critic every second and strikes that take nothing, so the show goes on: forty
        // critics, most of them at the side of the box office the magician's mark is on, and none turns on the
        // magician. The cloud lies on them, and those it does not reach walk on into their backs.
        Tuning tuning = CommittedTuning.Parse() with
        {
            ThrowRange = 0f,
            CriticTurnRadius = 0f,
            CriticEntryInterval = 1f,
            CriticStrikeDamage = 0f,
            VanishCloudRadius = 2.5f,
            VanishCloudTime = 1f,
            VanishStunTime = 1.5f,
        };
        var simulation = new Simulation(tuning, seed: 1);
        Run(simulation, ticks: 40 * Simulation.TicksPerSecond);

        simulation.Step(Vanish);
        Assert.That(simulation.Critics.Count(critic => critic.IsStunned), Is.GreaterThan(5));

        float touching = (tuning.BoxOfficeSize / 2f) + tuning.CriticRadius;
        for (int i = 0; i < 3 * Simulation.TicksPerSecond; i++)
        {
            simulation.Step(default);
            Assert.That(
                simulation.Critics.Select(critic => Vector2.Distance(critic.Position, tuning.BoxOfficePosition)),
                Has.All.GreaterThan(touching - 1e-3f));
        }
    }

    /// <summary>The first tick of <paramref name="tuning"/>: the critic has entered and nothing else has happened.</summary>
    private static Simulation ACriticAtTheDoor(Tuning tuning)
    {
        var simulation = new Simulation(tuning, seed: 1);
        simulation.Step(default);
        Assert.That(simulation.Critics.Select(critic => critic.Position), Is.EqualTo(new[] { Door }));
        return simulation;
    }

    private static void Run(Simulation simulation, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(default);
        }
    }
}
