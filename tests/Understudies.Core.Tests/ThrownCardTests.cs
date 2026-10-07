using System.Numerics;

namespace Understudies.Core.Tests;

public class ThrownCardTests
{
    private const float Tolerance = 1e-3f;

    /// <summary>In the stage's top edge: where the one critic of <see cref="Scene"/> stands.</summary>
    private static readonly Vector2 Door = new(20f, 0f);

    /// <summary>Six units straight below the critic: a card thrown at it flies straight up.</summary>
    private static readonly Vector2 Mark = new(20f, 6f);

    /// <summary>
    /// One critic that stands still, with the magician in range of it. The door has no width and the critic no
    /// speed, so it enters on the first tick exactly at <see cref="Door"/> and stays there; the second critic is an
    /// hour away, and no critic turns on the magician. The second card is an hour away too, and a card flies one
    /// unit a tick.
    /// </summary>
    private Tuning Scene { get; } = CommittedTuning.Parse() with
    {
        StageDoors = [Door],
        StageDoorWidth = 0f,
        MagicianMark = Mark,
        CriticSpeed = 0f,
        CriticEntryInterval = 3600f,
        CriticTurnRadius = 0f,
        CriticHitPoints = 3f,
        ThrowRange = 9f,
        ThrowCooldown = 3600f,
        ThrownCardSpeed = 60f,
        ThrownCardDamage = 1f,
    };

    /// <summary>
    /// <see cref="Scene"/> with the critic walking a unit a tick straight down the stage, to a box office far below
    /// the door, and the magician a unit below the door's edge and four units to the right. The card is thrown on
    /// the second tick, when the critic stands straight to the left of the magician, and flies straight left behind
    /// its back.
    /// </summary>
    private Tuning ACriticWalksPast => Scene with
    {
        MagicianMark = new Vector2(24f, 1f),
        BoxOfficePosition = new Vector2(20f, 25f),
        CriticSpeed = 60f,
    };

    [Test]
    public void Step_ACriticEnters_WithTheHitPointsOfTheTuning()
    {
        var simulation = new Simulation(Scene with { CriticHitPoints = 7f }, seed: 1);

        simulation.Step(default);

        Assert.That(simulation.Critics[0].HitPoints, Is.EqualTo(7f));
    }

    [Test]
    public void Step_ACriticInRange_TheMagicianThrowsACardFromWhereItStands()
    {
        var simulation = new Simulation(Scene, seed: 1);
        simulation.Step(default);
        Assert.That(simulation.ThrownCards, Is.Empty);

        // A card thrown this tick has not flown yet.
        simulation.Step(default);

        Assert.That(simulation.ThrownCards, Has.Count.EqualTo(1));
        Assert.That(simulation.ThrownCards[0].Position, Is.EqualTo(Mark));
        Assert.That(simulation.ThrownCards[0].PreviousPosition, Is.EqualTo(Mark));
    }

    [Test]
    public void Step_TheCriticIsOutOfRange_NothingIsThrownUntilItIsInRange()
    {
        // The magician's step is 0.15 of a unit: one step from 9.1 units away is well inside a range of 9.
        Tuning tuning = Scene with { MagicianMark = new Vector2(20f, 9.1f), MagicianSpeed = 9f };
        var simulation = new Simulation(tuning, seed: 1);

        Run(simulation, ticks: 5 * Simulation.TicksPerSecond);
        Assert.That(simulation.ThrownCards, Is.Empty);

        // However long there was nobody to throw at, the throw is ready on the tick somebody is there.
        simulation.Step(new MagicianInput(new Vector2(0f, -1f)));
        Assert.That(simulation.ThrownCards, Has.Count.EqualTo(1));
    }

    [Test]
    public void Step_ACriticStaysInRange_TheMagicianThrowsOncePerCooldown()
    {
        // Half a second is 30 ticks. The cards are slow: each is still in the air, so they count the throws.
        var simulation = new Simulation(Scene with { ThrowCooldown = 0.5f, ThrownCardSpeed = 1f }, seed: 1);
        Run(simulation, ticks: 2);
        Assert.That(simulation.ThrownCards, Has.Count.EqualTo(1));

        Run(simulation, ticks: 29);
        Assert.That(simulation.ThrownCards, Has.Count.EqualTo(1));

        Run(simulation, ticks: 1);
        Assert.That(simulation.ThrownCards, Has.Count.EqualTo(2));

        Run(simulation, ticks: 30);
        Assert.That(simulation.ThrownCards, Has.Count.EqualTo(3));
    }

    [Test]
    public void Step_AThrownCard_FliesItsSpeedStraightAtWhereItsTargetStood()
    {
        var simulation = new Simulation(ACriticWalksPast, seed: 1);
        Run(simulation, ticks: 2);
        Assert.That(simulation.Critics[0].Position, Is.EqualTo(new Vector2(20f, 1f)));

        // The critic walks on and the magician walks away: neither turns the card.
        for (int i = 0; i < 3; i++)
        {
            simulation.Step(new MagicianInput(new Vector2(1f, 0f)));
        }

        ThrownCard card = simulation.ThrownCards[0];
        Assert.That(Vector2.Distance(card.Position, new Vector2(21f, 1f)), Is.Zero.Within(Tolerance));
        Assert.That(Vector2.Distance(card.PreviousPosition, new Vector2(22f, 1f)), Is.Zero.Within(Tolerance));
    }

    [Test]
    public void Step_ACardThatTouchesNobody_IsGoneWhenItHasFlownTheThrowsRange()
    {
        var simulation = new Simulation(ACriticWalksPast with { ThrowRange = 6f }, seed: 1);

        // Thrown on the second tick; five ticks later it has flown five units.
        Run(simulation, ticks: 7);
        Assert.That(simulation.ThrownCards, Has.Count.EqualTo(1));

        simulation.Step(default);
        Assert.That(simulation.ThrownCards, Is.Empty);
    }

    [Test]
    public void Step_ACardReachesItsCritic_HurtsItOnceAndIsGone()
    {
        var simulation = new Simulation(Scene, seed: 1);

        // Thrown on the second tick from six units away; five ticks later it is half a unit short of the critic's
        // circle.
        Run(simulation, ticks: 7);
        Assert.That(simulation.ThrownCards, Has.Count.EqualTo(1));
        Assert.That(simulation.Critics[0].HitPoints, Is.EqualTo(3f));

        simulation.Step(default);
        Assert.That(simulation.ThrownCards, Is.Empty);
        Assert.That(simulation.Critics[0].HitPoints, Is.EqualTo(2f));

        Run(simulation, ticks: Simulation.TicksPerSecond);
        Assert.That(simulation.Critics[0].HitPoints, Is.EqualTo(2f));
    }

    [Test]
    public void Step_ACardFastEnoughToJumpOverACritic_StillHurtsIt()
    {
        // Eight units a tick: the card's first step starts six units before the critic and ends two units behind it.
        var simulation = new Simulation(Scene with { ThrownCardSpeed = 480f }, seed: 1);

        Run(simulation, ticks: 3);

        Assert.That(simulation.ThrownCards, Is.Empty);
        Assert.That(simulation.Critics[0].HitPoints, Is.EqualTo(2f));
    }

    [Test]
    public void Step_ACriticBeyondTheThrowsRange_IsOutOfTheCardsReach()
    {
        // The critic walks a unit a tick straight down the stage, away from the magician, who stands on its line 1.1
        // units below the door. Nobody is thrown at until the range is given, on the tick the critic comes to stand
        // 8.9 units away. The card flies eight units a tick: one step, which ends short of the critic, and then only
        // the unit that is left of its range. The eight it has not got would take it through the critic, two units
        // on.
        Tuning tuning = Scene with
        {
            MagicianMark = new Vector2(20f, 1.1f),
            BoxOfficePosition = new Vector2(20f, 25f),
            CriticSpeed = 60f,
            ThrownCardSpeed = 480f,
        };
        var simulation = new Simulation(tuning with { ThrowRange = 0f }, seed: 1);
        Run(simulation, ticks: 10);
        simulation.Tuning = tuning;
        simulation.Step(default);
        Assert.That(simulation.Critics[0].Position, Is.EqualTo(new Vector2(20f, 10f)));
        Assert.That(simulation.ThrownCards, Has.Count.EqualTo(1));

        Run(simulation, ticks: 2);

        Assert.That(simulation.ThrownCards, Is.Empty);
        Assert.That(simulation.Critics[0].HitPoints, Is.EqualTo(3f));
    }

    [Test]
    public void Step_TwoCriticsInRange_TheNearestIsThrownAt()
    {
        // Straight below the second critic: six units from it and a little more from the first.
        Simulation simulation = TwoCritics(Scene with { MagicianMark = new Vector2(20.5f, 6f) });

        Run(simulation, ticks: 10);

        Assert.That(simulation.Critics.Select(critic => critic.HitPoints), Is.EqualTo(new[] { 3f, 2f }));
    }

    [Test]
    public void Step_TwoCriticsAsNear_TheOneThatEnteredFirstIsThrownAt()
    {
        // Straight below the middle of the two, which is Scene's own mark.
        Simulation simulation = TwoCritics(Scene);

        Run(simulation, ticks: 10);

        Assert.That(simulation.Critics.Select(critic => critic.HitPoints), Is.EqualTo(new[] { 2f, 3f }));
    }

    [Test]
    public void Step_OneStepOfACardGoesThroughTwoCritics_TheOneItComesToFirstIsHurt()
    {
        // On the critics' line, to the right of both: the card, eight units a tick, comes to the second critic and
        // then, in the same step, to the first.
        Tuning tuning = OnTheCriticsLine(x: 26f) with { ThrownCardSpeed = 480f };
        Simulation simulation = TwoCritics(tuning);

        Run(simulation, ticks: 2);

        Assert.That(simulation.ThrownCards, Is.Empty);
        Assert.That(simulation.Critics.Select(critic => critic.HitPoints), Is.EqualTo(new[] { 3f, 2f }));
    }

    [Test]
    public void Step_ACriticBehindTheCard_IsNotHurtByIt()
    {
        // Between the two, nearer the second: the card flies right, away from the first, whose circle the line of
        // its flight goes through all the same, behind the magician's back.
        Simulation simulation = TwoCritics(OnTheCriticsLine(x: 20.2f));

        Run(simulation, ticks: 2);

        Assert.That(simulation.ThrownCards, Is.Empty);
        Assert.That(simulation.Critics.Select(critic => critic.HitPoints), Is.EqualTo(new[] { 3f, 2f }));
    }

    [Test]
    public void Step_ACriticTakesAsManyCardsAsItsHitPointsSay_ThenItFalls()
    {
        // Three hit points, and a card takes one. Thrown on ticks 2, 32 and 62, the cards arrive six ticks later.
        var simulation = new Simulation(Scene with { ThrowCooldown = 0.5f }, seed: 1);

        Run(simulation, ticks: 67);
        Assert.That(simulation.Critics, Has.Count.EqualTo(1));
        Assert.That(simulation.Critics[0].HitPoints, Is.EqualTo(1f));

        simulation.Step(default);
        Assert.That(simulation.Critics, Is.Empty);
    }

    [Test]
    public void Step_ACriticFalls_TheOtherStaysAsItWas()
    {
        // The same distance to both, so the first to enter is thrown at; it has one hit point.
        Simulation simulation = TwoCritics(Scene with { CriticHitPoints = 1f });
        Critic second = simulation.Critics[1];

        Run(simulation, ticks: 10);

        Assert.That(simulation.Critics, Is.EqualTo(new[] { second }));
        Assert.That(second.Id, Is.EqualTo(1));
        Assert.That(second.HitPoints, Is.EqualTo(1f));
    }

    [Test]
    public void Step_ACardHurtsACriticThatStillStands_AHitIsReportedThereForThatTickOnly()
    {
        var simulation = new Simulation(Scene, seed: 1);
        Run(simulation, ticks: 7);
        Assert.That(simulation.Events, Is.Empty);

        simulation.Step(default);
        Assert.That(simulation.Events, Is.EqualTo(new[] { new TickEvent(TickEventKind.Hit, Door) }));

        simulation.Step(default);
        Assert.That(simulation.Events, Is.Empty);
    }

    [Test]
    public void Step_ACardTakesACriticsLastHitPoint_AKillIsReportedThereAndNoHit()
    {
        var simulation = new Simulation(Scene with { CriticHitPoints = 1f }, seed: 1);

        Run(simulation, ticks: 8);
        Assert.That(simulation.Events, Is.EqualTo(new[] { new TickEvent(TickEventKind.Kill, Door) }));

        simulation.Step(default);
        Assert.That(simulation.Events, Is.Empty);
    }

    /// <summary>
    /// <see cref="Scene"/> with the magician in the stage's top edge, where the two of <see cref="TwoCritics"/>
    /// stand: a magician with no body, since the edge keeps the whole circle of one that has a body on the floor.
    /// </summary>
    private Tuning OnTheCriticsLine(float x) => Scene with { MagicianMark = new Vector2(x, 0f), MagicianRadius = 0f };

    /// <summary>
    /// The first three ticks of <paramref name="tuning"/> with a second critic: the two stand still side by side,
    /// the first to enter half a unit to the left of the door and the second half a unit to the right of it, and
    /// nothing has been thrown yet.
    /// </summary>
    private static Simulation TwoCritics(Tuning tuning)
    {
        // Both enter on the door's one point, a tick apart, and on the third tick they part along the stage's
        // width, the earlier to the left. Nobody is thrown at meanwhile: the throw's range is given last.
        Tuning outOfRange = tuning with { ThrowRange = 0f };
        var simulation = new Simulation(
            outOfRange with { CriticEntryInterval = 1f / Simulation.TicksPerSecond },
            seed: 1);
        simulation.Step(default);
        simulation.Tuning = outOfRange;
        Run(simulation, ticks: 2);
        simulation.Tuning = tuning;
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
