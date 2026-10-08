using System.Numerics;

namespace Understudies.Core.Tests;

public class UnderstudyTests
{
    /// <summary>Two seconds: the act of <see cref="Scene"/>.</summary>
    private const int ActTicks = 2 * Simulation.TicksPerSecond;

    /// <summary>In the stage's top edge: where the one critic of <see cref="Scene"/> stands.</summary>
    private static readonly Vector2 Door = new(20f, 0f);

    /// <summary>Twenty units to the right of the critic, and further than a Vanish from every edge.</summary>
    private static readonly Vector2 Mark = new(40f, 13f);

    /// <summary>
    /// A performance of five acts of two seconds, with one critic that stands still. The door has no width and the
    /// critic no speed, so it enters on the first tick exactly at <see cref="Door"/> and stays there; no other
    /// critic enters. The magician is far from it on <see cref="Mark"/> and walks a quarter of a unit a
    /// tick, which a float adds and takes away without a rounding. Nobody throws, and no critic turns on the
    /// magician; a Vanish takes it six units, is ready again half a second later, and leaves a cloud that is there
    /// for the tick of the Vanish only.
    /// </summary>
    private Tuning Scene { get; } = CommittedTuning.Parse().WithCritic(critic => critic with { Speed = 0f }) with
    {
        ActLength = 2f,
        ActsInPerformance = 5,
        StageFloorTop = 0f,
        StageDoors = [new StageDoor(Door, 1)],
        StageDoorWidth = 0f,
        MagicianMark = Mark,
        MagicianSpeed = 15f,
        VanishDistance = 6f,
        VanishCooldown = 0.5f,
        VanishCloudTime = 1f / Simulation.TicksPerSecond,
        ThrowRange = 0f,
        CriticTurnRadius = 0f,
    };

    /// <summary>
    /// <see cref="Scene"/> with the mark six units straight below the critic, which is in the range of whoever
    /// stands there and outlasts every card thrown at it. A throw is ready again half a second later, and a card
    /// flies a unit a second: no card thrown in a test has landed by the end of it.
    /// </summary>
    private Tuning InRangeOnTheMark => Scene.WithCritic(critic => critic with { HitPoints = 1000f }) with
    {
        MagicianMark = Door + new Vector2(0f, 6f),
        ThrowRange = 9f,
        ThrowCooldown = 0.5f,
        ThrownCardSpeed = 1f,
    };

    [Test]
    public void Understudies_InTheFirstAct_ThereIsNone()
    {
        var simulation = Shows.WithOneCritic(Scene);
        Assert.That(simulation.Understudies, Is.Empty);

        // Nor when the act is over: the act just played is an understudy from the next act on.
        Run(simulation, ticks: ActTicks);

        Assert.That(simulation.Phase, Is.EqualTo(Phase.BetweenActs));
        Assert.That(simulation.Understudies, Is.Empty);
    }

    [Test]
    public void GoOn_EveryActPlayed_LeavesAnUnderstudy_AndActFiveHasFour()
    {
        var simulation = Shows.WithOneCritic(Scene);

        for (int act = 2; act <= 5; act++)
        {
            Run(simulation, ticks: ActTicks);
            simulation.GoOn();

            // In the order of their acts, the oldest first.
            Assert.That(simulation.Act, Is.EqualTo(act));
            Assert.That(
                simulation.Understudies.Select(understudy => understudy.Act),
                Is.EqualTo(Enumerable.Range(1, act - 1)));
        }

        Assert.That(simulation.Understudies, Has.Count.EqualTo(4));
    }

    [Test]
    public void Step_AnUnderstudy_StandsTickForTickWhereTheMagicianStoodInItsAct()
    {
        // The first act goes left, stands, goes down and to the right at once, and then up at half speed; the
        // second goes down the stage all through.
        var simulation = Shows.WithOneCritic(Scene);
        (Vector2 Position, Vector2 Before)[] first = Play(simulation, tick => tick switch
        {
            < 20 => new MagicianInput(new Vector2(-1f, 0f)),
            < 30 => default,
            < 70 => new MagicianInput(new Vector2(1f, 1f)),
            _ => new MagicianInput(new Vector2(0f, -0.5f)),
        });
        Assert.That(first.Select(stood => stood.Position).Distinct().Count(), Is.EqualTo(ActTicks - 10));
        simulation.GoOn();

        // Before the act's first tick the understudy stands on the first place of its route: where the magician
        // stood after the first tick of its act, a step from the mark.
        Understudy ofTheFirst = simulation.Understudies[0];
        Assert.That(ofTheFirst.IsOnStage, Is.True);
        Assert.That(ofTheFirst.Position, Is.EqualTo(Mark + new Vector2(-0.25f, 0f)));
        Assert.That(ofTheFirst.PreviousPosition, Is.EqualTo(ofTheFirst.Position));

        (Vector2 Position, Vector2 Before)[] second = Play(
            simulation, _ => new MagicianInput(new Vector2(0f, 1f)), tick => AssertStandsAsIn(first, ofTheFirst, tick));
        simulation.GoOn();

        // In the third act both play theirs again, each from its first tick, whatever the magician does now.
        Understudy ofTheSecond = simulation.Understudies[1];
        Assert.That(ofTheFirst.Position, Is.EqualTo(first[0].Position));
        Assert.That(ofTheSecond.Position, Is.EqualTo(second[0].Position));
        Play(simulation, _ => new MagicianInput(new Vector2(1f, 0f)), tick =>
        {
            AssertStandsAsIn(first, ofTheFirst, tick);
            AssertStandsAsIn(second, ofTheSecond, tick);
        });
    }

    [Test]
    public void Step_AnUnderstudysVanish_LeavesItsCloudWhereTheMagiciansWasLeftOnTheSameTickOfTheAct()
    {
        // Twenty ticks to the left, five units, and a Vanish from there on the next: the cloud is left where the
        // magician stood, and is there for that tick only.
        var from = Mark + new Vector2(-5f, 0f);
        var simulation = Shows.WithOneCritic(Scene);
        (Vector2 Position, Vector2 Before)[] first = Play(simulation, tick => tick switch
        {
            < 20 => new MagicianInput(new Vector2(-1f, 0f)),
            20 => new MagicianInput(new Vector2(-1f, 0f), Vanish: true),
            _ => default,
        });
        Assert.That(first[20], Is.EqualTo((from + new Vector2(-6f, 0f), from + new Vector2(-6f, 0f))));
        simulation.GoOn();

        // The magician of the second act walks down the stage and never vanishes.
        Understudy understudy = simulation.Understudies[0];
        Play(simulation, _ => new MagicianInput(new Vector2(0f, 1f)), tick =>
        {
            // The cloud is the understudy's, with all its time, on the tick the magician's was left and on no
            // other. The Vanish that is reported is the magician's own: an understudy's is not.
            Assert.That(
                simulation.Clouds.Select(cloud => (cloud.Position, cloud.TicksLeft)),
                Is.EqualTo(tick == 20 ? new[] { (from, 1) } : []),
                $"tick {tick}");
            Assert.That(simulation.Events, Is.Empty, $"tick {tick}");

            // And the understudy is at once where the Vanish took the magician, with nothing between the two
            // places for the view to draw.
            AssertStandsAsIn(first, understudy, tick);
        });
    }

    [Test]
    public void Step_AnUnderstudysVanish_StunsOnTheRecordedTickAndForAsLongAsTheMagiciansDid()
    {
        // The mark is two units below the critic, so the cloud of a Vanish from the mark touches it: it reaches
        // two and a half units and the critic half a unit. The Vanish itself goes down the stage, away from it.
        Tuning tuning = Scene.WithCritic(critic => critic with { Radius = 0.5f }) with
        {
            MagicianMark = Door + new Vector2(0f, 2f),
            VanishCloudRadius = 2.5f,
            VanishStunTime = 0.5f,
        };
        var simulation = Shows.WithOneCritic(tuning);
        var stunned = new List<bool>();
        void NoteTheCritic(int tick) => stunned.Add(simulation.Critics[0].IsStunned);

        // Half a second of stun from the tick of the Vanish, which is the forty-first of the act.
        bool[] fromTheFortyFirst = [.. Enumerable.Range(0, ActTicks).Select(tick => tick is >= 40 and < 70)];
        Play(simulation, tick => new MagicianInput(Vector2.Zero, Vanish: tick == 40), NoteTheCritic);
        Assert.That(stunned, Is.EqualTo(fromTheFortyFirst));
        simulation.GoOn();

        // In the second act the magician only stands there: the stun is the understudy's.
        stunned.Clear();
        Play(simulation, _ => default, NoteTheCritic);

        Assert.That(stunned, Is.EqualTo(fromTheFortyFirst));
    }

    [Test]
    public void Step_AnUnderstudy_ThrowsAtACriticInItsRangeFromWhereItStands_AndNeverAtNothing()
    {
        // The critic is 13 units up the stage from the magician's row. The fifty-first step to the left is the
        // first inside a range of fifteen: 7.25 units to the right of the critic is 14.9 away from it, and a
        // quarter of a unit further right is a hair more than 15. The second throw of anybody is an hour away, and
        // a card flies a unit a tick. The critic can take two cards.
        var inRange = Mark + new Vector2(-12.75f, 0f);
        Tuning tuning = Scene.WithCritic(critic => critic with { HitPoints = 2f }) with
        {
            ThrowRange = 15f,
            ThrowCooldown = 3600f,
            ThrownCardSpeed = 60f,
            ThrownCardDamage = 1f,
        };
        var simulation = Shows.WithOneCritic(tuning);
        IEnumerable<(Vector2, Vector2, bool)> Cards() =>
            simulation.ThrownCards.Select(card => (card.Position, card.PreviousPosition, card.ThrownByMagician));

        // The first act: sixty steps to the left, and the magician throws its one card on the way.
        Play(simulation, tick => tick < 60 ? new MagicianInput(new Vector2(-1f, 0f)) : default, tick =>
        {
            if (tick <= 50)
            {
                Assert.That(Cards(), Is.EqualTo(tick < 50 ? [] : new[] { (inRange, inRange, true) }), $"tick {tick}");
            }
        });
        Assert.That(simulation.Critics[0].HitPoints, Is.EqualTo(1f));
        simulation.GoOn();

        // In the second the magician stays on its mark, out of range: whatever is thrown, the understudy throws.
        // Nothing while nobody is in its range, and a card from where it stands on the tick the critic is.
        var events = new List<TickEvent>();
        Play(simulation, _ => default, tick =>
        {
            if (tick <= 50)
            {
                Assert.That(Cards(), Is.EqualTo(tick < 50 ? [] : new[] { (inRange, inRange, false) }), $"tick {tick}");
            }

            events.AddRange(simulation.Events);
        });

        // An understudy's throw is a throw, reported at the place it throws from. And its card hurts like any
        // card: this one was the critic's last.
        Assert.That(
            events,
            Is.EqualTo(new[] { new TickEvent(TickEventKind.Throw, inRange), new TickEvent(TickEventKind.Kill, Door) }));
        Assert.That(simulation.Critics, Is.Empty);
        simulation.GoOn();

        // In the third act two understudies have their throws ready and nobody is left to throw at.
        Play(simulation, _ => default, tick => Assert.That(simulation.ThrownCards, Is.Empty, $"tick {tick}"));
    }

    [Test]
    public void Step_ACriticStaysInRangeOfAnUnderstudy_ItThrowsOncePerCooldownOnACountdownOfItsOwn()
    {
        // The mark is in range of the critic, and nobody leaves it. Three quarters of a second are 45 ticks. The
        // cards are slow: each is still in the air, so they count the throws.
        var simulation = Shows.WithOneCritic(InRangeOnTheMark with { ThrowCooldown = 0.75f });
        int Thrown(bool byTheMagician) => simulation.ThrownCards.Count(card => card.ThrownByMagician == byTheMagician);

        // The critic enters on the first tick, after the throws: the magician throws on ticks 1, 46 and 91.
        Play(simulation, _ => default);
        Assert.That((Thrown(byTheMagician: true), Thrown(byTheMagician: false)), Is.EqualTo((3, 0)));
        simulation.GoOn();

        // The understudy's throw is ready when the act begins, and the critic is there: ticks 0, 45 and 90. The
        // magician's own countdown goes on from the first act, where going on left it: its next throw is 45 ticks
        // after its last, on tick 16 of this act.
        var understudys = new List<int>();
        var magicians = new List<int>();
        Play(simulation, _ => default, _ =>
        {
            understudys.Add(Thrown(byTheMagician: false));
            magicians.Add(Thrown(byTheMagician: true));
        });
        Assert.That(understudys[0], Is.EqualTo(1));
        Assert.That(understudys[44], Is.EqualTo(1));
        Assert.That(understudys[45], Is.EqualTo(2));
        Assert.That(understudys[89], Is.EqualTo(2));
        Assert.That(understudys[90], Is.EqualTo(3));
        Assert.That(understudys[^1], Is.EqualTo(3));
        Assert.That(magicians[15], Is.EqualTo(3));
        Assert.That(magicians[16], Is.EqualTo(4));
        Assert.That(magicians[^1], Is.EqualTo(6));
        simulation.GoOn();

        // An act begins with every understudy's throw ready, though the first threw thirty ticks ago.
        simulation.Step(default);
        Assert.That(Thrown(byTheMagician: false), Is.EqualTo(5));
    }

    [Test]
    public void Step_ABlowClosesTheShow_NothingIsThrownAfterItOnThatTick()
    {
        // The critic walks a unit a tick at a magician that one touch fells, six units below the door. The magician
        // has the critic in range and a throw ready on every tick; its cards are slow and none has landed.
        Tuning tuning = InRangeOnTheMark.WithCritic(critic => critic with { Speed = 60f }) with
        {
            MagicianHitPoints = 1f,
            ThrowCooldown = 1f / Simulation.TicksPerSecond,
            CriticTurnRadius = 100f,
        };
        var simulation = Shows.WithOneCritic(tuning);
        simulation.Step(default);
        simulation.Step(default);
        Assert.That(simulation.ThrownCards, Has.Count.EqualTo(1));
        Assert.That(simulation.ShowClosed, Is.False);

        int thrown = 0;
        while (!simulation.ShowClosed)
        {
            thrown = simulation.ThrownCards.Count;
            simulation.Step(default);
        }

        Assert.That(simulation.MagicianHasFallen, Is.True);
        Assert.That(simulation.ThrownCards, Has.Count.EqualTo(thrown));
        Assert.That(simulation.Events.Select(happened => happened.Kind), Has.None.EqualTo(TickEventKind.Throw));
    }

    [Test]
    public void Step_AVanishInTheFirstActAndNoneInTheSecond_OnlyTheFirstActsUnderstudyLeavesACloud()
    {
        var simulation = Shows.WithOneCritic(Scene);
        Play(simulation, tick => new MagicianInput(Vector2.Zero, Vanish: tick == 20));
        simulation.GoOn();
        Play(simulation, _ => default);
        simulation.GoOn();

        // An act's recording starts empty: the second act's has no Vanish of the first in it. So the third act has
        // one cloud, the first understudy's, on its tick and where it was left.
        Play(simulation, _ => default, tick => Assert.That(
            simulation.Clouds.Select(cloud => cloud.Position),
            Is.EqualTo(tick == 20 ? new[] { Mark } : []),
            $"tick {tick}"));
    }

    [Test]
    public void Step_TheMagicianAndTwoUnderstudiesThrowOnOneTick_TheMagicianThrowsFirstAndThenTheUnderstudiesInTheOrderOfTheirActs()
    {
        // Nobody has a range in the first two acts, which leave an understudy a step to the left of the mark and
        // one a step to the right of it.
        var simulation = Shows.WithOneCritic(Scene);
        Play(simulation, tick => tick == 0 ? new MagicianInput(new Vector2(-1f, 0f)) : default);
        simulation.GoOn();
        Play(simulation, tick => tick == 0 ? new MagicianInput(new Vector2(1f, 0f)) : default);

        // From the third act on a throw reaches the critic from anywhere near the mark, and all three are ready.
        simulation.Tuning = Scene with { ThrowRange = 30f };
        simulation.GoOn();
        simulation.Step(new MagicianInput(new Vector2(0f, 1f)));

        Assert.That(
            simulation.ThrownCards.Select(card => (card.ThrownFrom, card.ThrownByMagician)),
            Is.EqualTo(new[]
            {
                (Mark + new Vector2(0f, 0.25f), true),
                (Mark + new Vector2(-0.25f, 0f), false),
                (Mark + new Vector2(0.25f, 0f), false),
            }));
    }

    [Test]
    public void Step_AnActGoesOnForLongerThanAnUnderstudysOwn_ItIsGoneFromTheStageWhenItsRouteRunsOut()
    {
        // The first act is a second long. New numbers count from the next act: the second is two seconds long.
        var simulation = Shows.WithOneCritic(InRangeOnTheMark with { ActLength = 1f });
        Play(simulation, _ => default);
        simulation.Tuning = InRangeOnTheMark;
        simulation.GoOn();
        Understudy understudy = simulation.Understudies[0];
        Assert.That(understudy.Route, Has.Count.EqualTo(60));
        Assert.That(simulation.ActTicksLeft, Is.EqualTo(ActTicks));

        var onStage = new List<bool>();
        var thrown = new List<int>();
        Play(simulation, _ => default, _ =>
        {
            onStage.Add(understudy.IsOnStage);
            thrown.Add(simulation.ThrownCards.Count(card => !card.ThrownByMagician));
        });

        // It is on the stage for the sixty ticks it has a place for, and throws on ticks 0 and 30 of them. Then it
        // is gone: the critic is still in range of where it stood, and nothing is thrown from there on ticks 60
        // and 90.
        Assert.That(onStage, Is.EqualTo(Enumerable.Range(0, ActTicks).Select(tick => tick < 60)));
        Assert.That(thrown[59], Is.EqualTo(2));
        Assert.That(thrown[^1], Is.EqualTo(2));

        // The next act has it back, from the start of its route.
        simulation.GoOn();
        Assert.That(understudy.IsOnStage, Is.True);
        simulation.Step(default);
        Assert.That(understudy.IsOnStage, Is.True);
    }

    [Test]
    public void Step_AnUnderstudyStandsInACriticsWay_TheCriticWalksThroughItAndNeitherTurnsOnItNorTouchesIt()
    {
        // The critic walks a twentieth of a unit a tick straight down the stage, to a box office it touches 17.5
        // units below the door, and turns on a magician that is nearer than two units. In the first act the
        // magician walks six units to the left and waits half a unit to the right of the critic's way and nine
        // units down it: the critic is six units down when the act ends, three from the magician.
        Tuning tuning = Scene.WithCritic(critic => critic with { Speed = 3f, Radius = 0.5f }) with
        {
            MagicianMark = Door + new Vector2(6.5f, 9f),
            MagicianRadius = 0.6f,
            BoxOfficePosition = Door + new Vector2(0f, 20f),
            BoxOfficeSize = 4f,
            CriticTurnRadius = 2f,
        };
        var simulation = Shows.WithOneCritic(tuning);
        Play(simulation, tick => tick < 24 ? new MagicianInput(new Vector2(-1f, 0f)) : default);
        simulation.GoOn();
        Understudy understudy = simulation.Understudies[0];
        Critic critic = simulation.Critics[0];

        // In the second act the magician stays on its mark, out of the critic's reach, and the critic walks from
        // six units down to twelve, past the understudy and through the edge of its circle.
        Play(simulation, _ => default, tick =>
        {
            // A critic that turned on the understudy would leave its line; one the understudy blocked or pushed
            // would fall behind on it.
            Assert.That(critic.Position.X, Is.EqualTo(Door.X), $"tick {tick}");
            Assert.That(critic.Position.Y, Is.EqualTo(0.05f * (ActTicks + tick)).Within(1e-3f), $"tick {tick}");

            // And a touch would be a blow.
            Assert.That(simulation.Events, Is.Empty, $"tick {tick}");
        });

        Assert.That(understudy.Position, Is.EqualTo(Door + new Vector2(0.5f, 9f)));
        Assert.That(critic.Position.Y, Is.GreaterThan(understudy.Position.Y + tuning.MagicianRadius + tuning.Critic().Radius));
        Assert.That(critic.HitPoints, Is.EqualTo(tuning.Critic().HitPoints));
        Assert.That(simulation.MagicianHitPoints, Is.EqualTo(tuning.MagicianHitPoints));
    }

    [Test]
    public void ComputeStateHash_TwoRoutesInAnEarlierAct_AreTwoHashesThoughEverybodyStandsAlikeNow()
    {
        // One magician took a step to the right and a step back when the first act began, and the other never left
        // the mark. Three ticks into the second act both understudies stand on the mark, as both magicians do.
        var walked = Shows.WithOneCritic(Scene);
        var stood = Shows.WithOneCritic(Scene);
        Play(walked, tick => tick switch
        {
            0 => new MagicianInput(new Vector2(1f, 0f)),
            1 => new MagicianInput(new Vector2(-1f, 0f)),
            _ => default,
        });
        Play(stood, _ => default);
        foreach (Simulation simulation in new[] { walked, stood })
        {
            simulation.GoOn();
            Run(simulation, ticks: 3);
            Assert.That(simulation.MagicianPosition, Is.EqualTo(Mark));
            Assert.That(simulation.Understudies[0].Position, Is.EqualTo(Mark));
        }

        // The two shows part again at the start of the third act: the routes are in the hash.
        Assert.That(walked.ComputeStateHash(), Is.Not.EqualTo(stood.ComputeStateHash()));
    }

    [Test]
    public void ComputeStateHash_TwoWaysToOnePlaceInTheActThatIsPlayed_AreTwoHashes()
    {
        // Four ticks into the first act both magicians stand on the mark and face right. One has been a step to
        // the right of it and a step to the left, the other only to the left: the two acts leave two understudies.
        var left = new MagicianInput(new Vector2(-1f, 0f));
        var right = new MagicianInput(new Vector2(1f, 0f));
        var bothWays = Shows.WithOneCritic(Scene);
        var oneWay = Shows.WithOneCritic(Scene);
        foreach (MagicianInput input in new[] { right, left, left, right })
        {
            bothWays.Step(input);
        }

        foreach (MagicianInput input in new[] { default, default, left, right })
        {
            oneWay.Step(input);
        }

        Assert.That(bothWays.MagicianPosition, Is.EqualTo(Mark));
        Assert.That(oneWay.MagicianPosition, Is.EqualTo(Mark));

        Assert.That(bothWays.ComputeStateHash(), Is.Not.EqualTo(oneWay.ComputeStateHash()));
    }

    [TestCase(1, TestName = "ComputeStateHash_AVanishATickLaterInAnEarlierAct_IsAnotherHash")]
    [TestCase(-1, TestName = "ComputeStateHash_NoVanishInAnEarlierAct_IsAnotherHash")]
    public void ComputeStateHash_AnotherVanishInAnEarlierAct_IsAnotherHash(int otherVanishTick)
    {
        // A Vanish that goes nowhere, on the first tick of the first act. When the second act begins its cloud is
        // long gone and the Vanish is ready again: all that is left of it is that the understudy has one, and when.
        Tuning tuning = Scene with { VanishDistance = 0f };
        var one = Shows.WithOneCritic(tuning);
        var other = Shows.WithOneCritic(tuning);
        Play(one, tick => new MagicianInput(Vector2.Zero, Vanish: tick == 0));
        Play(other, tick => new MagicianInput(Vector2.Zero, Vanish: tick == otherVanishTick));
        one.GoOn();
        other.GoOn();

        Assert.That(other.ComputeStateHash(), Is.Not.EqualTo(one.ComputeStateHash()));
    }

    [Test]
    public void ComputeStateHash_AnUnderstudysTimeToItsNextThrow_IsInTheHash()
    {
        // Nobody has a range in the first act, in which the magician walks sixty steps to the left. From the second
        // act on the fifty-first of those steps is in range of the critic: the understudy throws there, and the
        // magician on its mark throws nothing. The two shows differ in how long it is to a thrower's next throw.
        ulong HashAfterTheThrow(float throwCooldown)
        {
            var simulation = Shows.WithOneCritic(Scene);
            Play(simulation, tick => tick < 60 ? new MagicianInput(new Vector2(-1f, 0f)) : default);
            simulation.Tuning = Scene with { ThrowRange = 15f, ThrowCooldown = throwCooldown };
            simulation.GoOn();
            Run(simulation, ticks: 52);
            Assert.That(simulation.ThrownCards.Select(card => card.ThrownByMagician), Is.EqualTo(new[] { false }));
            return simulation.ComputeStateHash();
        }

        Assert.That(HashAfterTheThrow(throwCooldown: 1f), Is.Not.EqualTo(HashAfterTheThrow(throwCooldown: 0.5f)));
    }

    /// <summary>
    /// After the tick of this act numbered <paramref name="tick"/>, the first being 0, the understudy is where the
    /// magician was after that tick of the act in <paramref name="played"/>, and was before it where the magician
    /// was: on its first tick it is where it stood already.
    /// </summary>
    private static void AssertStandsAsIn((Vector2 Position, Vector2 Before)[] played, Understudy understudy, int tick)
    {
        Assert.That(understudy.IsOnStage, Is.True, $"tick {tick}");
        Assert.That(understudy.Position, Is.EqualTo(played[tick].Position), $"tick {tick}");
        Assert.That(
            understudy.PreviousPosition,
            Is.EqualTo(tick == 0 ? played[0].Position : played[tick].Before),
            $"before tick {tick}");
    }

    /// <summary>
    /// Plays an act to its end with the input <paramref name="script"/> gives for each of its ticks, the first
    /// being 0, and says where the magician stood after each and before it.
    /// </summary>
    private static (Vector2 Position, Vector2 Before)[] Play(
        Simulation simulation, Func<int, MagicianInput> script, Action<int>? afterEachTick = null)
    {
        var stood = new (Vector2, Vector2)[simulation.ActTicksLeft];
        for (int tick = 0; tick < stood.Length; tick++)
        {
            simulation.Step(script(tick));
            stood[tick] = (simulation.MagicianPosition, simulation.MagicianPreviousPosition);
            afterEachTick?.Invoke(tick);
        }

        Assert.That(simulation.Phase, Is.Not.EqualTo(Phase.Act));
        return stood;
    }

    private static void Run(Simulation simulation, int ticks, MagicianInput input = default)
    {
        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(input);
        }
    }
}
