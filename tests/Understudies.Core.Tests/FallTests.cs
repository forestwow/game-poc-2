using System.Numerics;

namespace Understudies.Core.Tests;

public class FallTests
{
    /// <summary>Two seconds: the act of <see cref="Scene"/>.</summary>
    private const int ActTicks = 2 * Simulation.TicksPerSecond;

    /// <summary>The magician of <see cref="Scene"/> that stands on its mark falls on the sixth tick of the act.</summary>
    private const int TicksToTheFall = 6;

    /// <summary>In the stage's top edge: where the one critic of <see cref="Scene"/> enters.</summary>
    private static readonly Vector2 Door = new(20f, 0f);

    /// <summary>A unit and a half to the right of the critic's way down the stage, five units below the door.</summary>
    private static readonly Vector2 Mark = new(21.5f, 5f);

    private static readonly MagicianInput Left = new(new Vector2(-1f, 0f));

    private static readonly MagicianInput Right = new(new Vector2(1f, 0f));

    private static readonly MagicianInput Down = new(new Vector2(0f, 1f));

    /// <summary>
    /// A performance of three acts of two seconds, with one critic and a magician that one touch fells. The door
    /// has no width, so the critic enters on the first tick exactly at <see cref="Door"/>; from the second tick on
    /// it walks a unit a tick straight down the stage, to a box office it touches 17.5 units below the door. The
    /// second critic is an hour away. A critic turns on a magician nearer than three units: this one does on the
    /// fifth tick, three units below the door, where a magician on <see cref="Mark"/> is two and a half away. Each
    /// of the two circles is half a unit, so they touch on the sixth tick, and the magician falls. A critic deals
    /// a blow every half second, one of the box office's thousand hit points a strike. The magician walks a
    /// quarter of a unit a tick and throws at nobody. The curtain has no length, which is no curtain: these tests
    /// count their ticks from the first tick of an act.
    /// </summary>
    private Tuning Scene { get; } = CommittedTuning.Parse().WithCritic(critic => critic with { Speed = 60f, Radius = 0.5f, StrikeDamage = 1f }) with
    {
        CurtainTime = 0f,
        ActLength = 2f,
        ActsInPerformance = 3,
        StageFloorTop = 0f,
        StageDoors = [new StageDoor(Door, 1)],
        StageDoorWidth = 0f,
        BoxOfficePosition = Door + new Vector2(0f, 20f),
        BoxOfficeSize = 4f,
        BoxOfficeHitPoints = 1000f,
        MagicianMark = Mark,
        MagicianSpeed = 15f,
        MagicianRadius = 0.5f,
        MagicianHitPoints = 1f,
        VanishDistance = 6f,
        ThrowRange = 0f,
        CriticTurnRadius = 3f,
        CriticTouchDamage = 1f,
        CriticBlowCooldown = 0.5f,
    };

    [Test]
    public void Step_TheMagicianFalls_TheShowIsNotClosedAndTheActRunsToItsTimer()
    {
        Simulation simulation = Shows.WithOneCritic(Scene);
        Run(simulation, ticks: TicksToTheFall - 1);
        Assert.That(simulation.MagicianHasFallen, Is.False);

        simulation.Step(default);
        Assert.That(simulation.MagicianHasFallen, Is.True);
        Assert.That(simulation.ShowClosed, Is.False);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.ActTicksLeft, Is.EqualTo(ActTicks - TicksToTheFall));

        // The act is played to its last tick, and the magician lies fallen all through it.
        Run(simulation, ticks: ActTicks - TicksToTheFall - 1);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        simulation.Step(default);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.BetweenActs));
        Assert.That(simulation.MagicianHasFallen, Is.True);
    }

    [Test]
    public void Step_TheMagicianFalls_TheFallIsReportedWhereItFellAfterTheBlowForThatTickOnly()
    {
        Simulation simulation = Shows.WithOneCritic(Scene);
        Run(simulation, ticks: TicksToTheFall);

        Assert.That(
            simulation.Events,
            Is.EqualTo(new[]
            {
                new TickEvent(TickEventKind.MagicianHurt, Mark), new TickEvent(TickEventKind.MagicianFell, Mark),
            }));

        simulation.Step(default);
        Assert.That(simulation.Events, Is.Empty);
    }

    [Test]
    public void Step_AFallenMagician_TakesNoInputAndLiesWhereItFell()
    {
        Simulation simulation = Shows.WithOneCritic(Scene);
        Run(simulation, ticks: TicksToTheFall);

        for (int tick = 0; tick < Simulation.TicksPerSecond; tick++)
        {
            simulation.Step(new MagicianInput(new Vector2(1f, 0f), Vanish: true));

            // Nor is there anything behind it for the view to draw from.
            Assert.That(simulation.MagicianPosition, Is.EqualTo(Mark));
            Assert.That(simulation.MagicianPreviousPosition, Is.EqualTo(Mark));
            Assert.That(simulation.Clouds, Is.Empty);
            Assert.That(simulation.MagicianIsInvulnerable, Is.False);
            Assert.That(simulation.Events.Select(happened => happened.Kind), Has.None.EqualTo(TickEventKind.Vanish));
        }
    }

    [Test]
    public void Step_AFallenMagician_ThrowsNothing()
    {
        // The critic is in range all through, and a throw is ready on every tick; the cards are slow and none
        // lands.
        Tuning tuning = Scene.WithCritic(critic => critic with { HitPoints = 1000f }) with
        {
            ThrowRange = 30f, ThrowCooldown = 1f / Simulation.TicksPerSecond, ThrownCardSpeed = 1f,
        };
        Simulation simulation = Shows.WithOneCritic(tuning);
        Run(simulation, ticks: TicksToTheFall - 1);
        int thrown = simulation.ThrownCards.Count;
        Assert.That(thrown, Is.GreaterThan(0));

        // Nor on the tick of the fall: the blow comes before the throw.
        for (int tick = 0; tick < Simulation.TicksPerSecond / 2; tick++)
        {
            simulation.Step(default);
            Assert.That(simulation.Events.Select(happened => happened.Kind), Has.None.EqualTo(TickEventKind.Throw));
        }

        Assert.That(simulation.MagicianHasFallen, Is.True);
        Assert.That(simulation.ThrownCards, Has.Count.EqualTo(thrown));
    }

    [Test]
    public void Step_TheMagicianHasFallen_TheCriticGoesBackToTheBoxOfficeAndNothingMoreHurtsTheMagician()
    {
        Simulation simulation = Shows.WithOneCritic(Scene);
        Run(simulation, ticks: TicksToTheFall);
        Critic critic = simulation.Critics[0];
        float toTheBoxOffice = Vector2.Distance(critic.Position, Scene.BoxOfficePosition);

        // On the very next tick it walks its whole step straight at the box office, the magician still in its
        // radius.
        simulation.Step(default);
        Assert.That(
            Vector2.Distance(critic.Position, Scene.BoxOfficePosition), Is.EqualTo(toTheBoxOffice - 1f).Within(1e-4f));

        // And strikes it for the rest of the act, as if the magician were not there.
        var kinds = new List<TickEventKind>();
        while (simulation.Phase == Phase.Act)
        {
            simulation.Step(default);
            kinds.AddRange(simulation.Events.Select(happened => happened.Kind));
        }

        Assert.That(kinds, Is.Not.Empty);
        Assert.That(kinds, Is.All.EqualTo(TickEventKind.BoxOfficeStruck));
        Assert.That(simulation.BoxOfficeHitPoints, Is.EqualTo(1000f - kinds.Count));
    }

    [Test]
    public void Step_ACriticWalksPastAFallenMagician_ItDoesNotTurnOnIt()
    {
        // A second critic enters half a second after the first, which felled the magician and went on: it walks
        // down the stage past the magician, a unit and a half to its side, and never out of its way.
        Simulation simulation = Shows.WithACriticEvery(Simulation.TicksPerSecond / 2, Scene);
        Run(simulation, ticks: 30 + TicksToTheFall + 4);

        Assert.That(simulation.MagicianHasFallen, Is.True);
        Assert.That(simulation.Critics[1].Position, Is.EqualTo(Door + new Vector2(0f, 9f)));
    }

    [Test]
    public void GoOn_AfterAFall_TheRecordingEndsWhereTheMagicianFellAndItsUnderstudyIsGoneFromThatTickOn()
    {
        // The magician walks to the left for the whole act, towards the critic, and falls where the sixth tick has put it.
        Simulation simulation = Shows.WithOneCritic(Scene);
        Run(simulation, ticks: TicksToTheFall, Left);
        Assert.That(simulation.MagicianHasFallen, Is.True);
        Vector2 fellAt = simulation.MagicianPosition;
        Assert.That(fellAt, Is.EqualTo(Mark - new Vector2(1.5f, 0f)));
        Run(simulation, ticks: ActTicks - TicksToTheFall, Left);
        simulation.GoOn();

        Understudy understudy = simulation.Understudies[0];
        Assert.That(understudy.Route, Has.Count.EqualTo(TicksToTheFall));
        Assert.That(understudy.Route[^1], Is.EqualTo(fellAt));

        // In the next act the magician walks the same way, far from the critic at the box office, and its understudy plays
        // the six ticks it has and is gone for the rest of the act.
        var onStage = new List<bool>();
        while (simulation.Phase == Phase.Act)
        {
            simulation.Step(Left);
            onStage.Add(understudy.IsOnStage);
        }

        Assert.That(simulation.MagicianHasFallen, Is.False);
        Assert.That(onStage, Is.EqualTo(Enumerable.Range(0, ActTicks).Select(tick => tick < TicksToTheFall)));
        Assert.That(understudy.Position, Is.EqualTo(fellAt));

        // And in the same way in the act after that.
        simulation.GoOn();
        Run(simulation, ticks: TicksToTheFall, Left);
        Assert.That(understudy.IsOnStage, Is.True);
        simulation.Step(Left);
        Assert.That(understudy.IsOnStage, Is.False);
    }

    [Test]
    public void GoOn_AfterAFall_TheNextActStartsWithTheMagicianWholeAndOnTheMark()
    {
        Simulation simulation = Shows.WithOneCritic(Scene);
        Run(simulation, ticks: 3, Right);
        Run(simulation, ticks: ActTicks - 3);
        Assert.That(simulation.MagicianHasFallen, Is.True);
        Assert.That(simulation.MagicianPosition, Is.Not.EqualTo(Mark));

        simulation.GoOn();

        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.MagicianHasFallen, Is.False);
        Assert.That(simulation.MagicianHitPoints, Is.EqualTo(Scene.MagicianHitPoints));
        Assert.That(simulation.MagicianPosition, Is.EqualTo(Mark));
        Assert.That(simulation.MagicianPreviousPosition, Is.EqualTo(Mark));

        // And it takes its input again.
        simulation.Step(Right);
        Assert.That(simulation.MagicianPosition, Is.EqualTo(Mark + new Vector2(0.25f, 0f)));
    }

    [Test]
    public void Step_TheMagicianFallsInALaterAct_TheOlderUnderstudyPlaysOnTickForTick()
    {
        // The first act: to the right all through, out of the critic's way, which goes to the box office and
        // stays there.
        Simulation simulation = Shows.WithOneCritic(Scene);
        var first = new List<Vector2>();
        while (simulation.Phase == Phase.Act)
        {
            simulation.Step(Right);
            first.Add(simulation.MagicianPosition);
        }

        Assert.That(simulation.MagicianHasFallen, Is.False);
        Assert.That(first.Distinct().Count(), Is.GreaterThan(ActTicks / 2));
        simulation.GoOn();

        // The second: down the stage and into the critic's radius, where the magician falls well before the act
        // is over. Its understudy from the first act goes on along its route to the act's last tick.
        Understudy ofTheFirst = simulation.Understudies[0];
        int ticksToTheFall = 0;
        for (int tick = 0; tick < ActTicks; tick++)
        {
            simulation.Step(Down);
            ticksToTheFall += simulation.MagicianHasFallen ? 0 : 1;
            Assert.That(ofTheFirst.IsOnStage, Is.True, $"tick {tick}");
            Assert.That(ofTheFirst.Position, Is.EqualTo(first[tick]), $"tick {tick}");
        }

        Assert.That(simulation.Phase, Is.EqualTo(Phase.BetweenActs));
        Assert.That(ticksToTheFall, Is.InRange(1, ActTicks - 30));
        simulation.GoOn();

        // The third: the understudy of the act cut short leaves the stage on the tick after its fall, and the
        // older one plays on.
        Understudy ofTheSecond = simulation.Understudies[1];
        Assert.That(ofTheSecond.Route, Has.Count.EqualTo(ticksToTheFall + 1));
        for (int tick = 0; tick < ActTicks; tick++)
        {
            simulation.Step(Right);
            Assert.That(ofTheSecond.IsOnStage, Is.EqualTo(tick <= ticksToTheFall), $"tick {tick}");
            Assert.That(ofTheFirst.IsOnStage, Is.True, $"tick {tick}");
            Assert.That(ofTheFirst.Position, Is.EqualTo(first[tick]), $"tick {tick}");
        }
    }

    [Test]
    public void Step_TheBoxOfficeFallsAfterTheMagicianFell_TheShowCloses()
    {
        // One strike is all this box office can take: the critic that felled the magician walks on to it.
        Simulation simulation = Shows.WithOneCritic(Scene with { BoxOfficeHitPoints = 1f });
        Run(simulation, ticks: TicksToTheFall);
        Assert.That(simulation.ShowClosed, Is.False);

        Run(simulation, ticks: ActTicks / 2);

        Assert.That(simulation.BoxOfficeHitPoints, Is.Zero);
        Assert.That(simulation.ShowClosed, Is.True);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Closed));
        Assert.That(simulation.ActTicksLeft, Is.GreaterThan(0));
    }

    [Test]
    public void Step_TheMagicianFallsInTheLastAct_ThePerformanceStillEndsInTheOvation()
    {
        Simulation simulation = Shows.WithOneCritic(Scene with { ActsInPerformance = 1 });

        Run(simulation, ticks: ActTicks);

        Assert.That(simulation.MagicianHasFallen, Is.True);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Ovation));
    }

    [Test]
    public void ComputeStateHash_WhateverIsAskedOfAFallenMagician_TheShowGoesOnAlike()
    {
        ulong HashAtTheEndOfTheAct(MagicianInput afterTheFall)
        {
            Simulation simulation = Shows.WithOneCritic(Scene);
            Run(simulation, ticks: TicksToTheFall);
            Run(simulation, ticks: ActTicks - TicksToTheFall, afterTheFall);
            return simulation.ComputeStateHash();
        }

        Assert.That(
            HashAtTheEndOfTheAct(new MagicianInput(new Vector2(-1f, 1f), Vanish: true)),
            Is.EqualTo(HashAtTheEndOfTheAct(default)));
    }

    private static void Run(Simulation simulation, int ticks, MagicianInput input = default)
    {
        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(input);
        }
    }
}
