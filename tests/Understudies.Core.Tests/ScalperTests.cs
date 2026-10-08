using System.Numerics;

namespace Understudies.Core.Tests;

/// <summary>Plan T57: a kind that eats applause.</summary>
public class ScalperTests
{
    /// <summary>The doors of <see cref="Scene"/>, by their places: each is a point of the stage's top edge.</summary>
    private const int DoorA = 0, DoorB = 1, DoorFar = 2, DoorBetween = 3;

    /// <summary>The kinds of <see cref="Scene"/>, by their places.</summary>
    private const int Still = 0, Scalper = 1;

    private static readonly Vector2 A = new(20f, 0f);

    private static readonly Vector2 B = new(26f, 0f);

    /// <summary>Ten units to the left of <see cref="A"/> and sixteen of <see cref="B"/>.</summary>
    private static readonly Vector2 Far = new(10f, 0f);

    /// <summary>Three units from <see cref="A"/> and three from <see cref="B"/>.</summary>
    private static readonly Vector2 Between = new(23f, 0f);

    /// <summary>
    /// An act of thirty seconds with four doors of no width in the stage's top edge. The first kind stands where
    /// it enters and falls to one card, which is how a piece of applause comes to lie exactly at a door; the
    /// second is the committed scalper with hit points that no test's cards take, so that it is watched and not
    /// felled. The magician stands six units below the middle of <see cref="A"/> and <see cref="B"/>, in range of
    /// both and out of reach of a piece at either, and throws every half second. A piece lies for twenty
    /// seconds, a fall anywhere leaves one, nobody turns on the magician and an encore costs more than an act has.
    /// </summary>
    private Tuning Scene { get; } = CommittedTuning.Parse() with
    {
        EnemyKinds =
        [
            CommittedTuning.Parse().Critic() with { Speed = 0f, Radius = 0.5f, HitPoints = 1f, TurnsOnTheMagician = false },
            CommittedTuning.Parse().Scalper() with { HitPoints = 1000f },
        ],
        CurtainTime = 0f,
        ActLength = 30f,
        ActsInPerformance = 2,
        StageFloorTop = 0f,
        StageDoors = [new StageDoor(A, 1), new StageDoor(B, 1), new StageDoor(Far, 1), new StageDoor(Between, 1)],
        StageDoorWidth = 0f,
        MagicianMark = new Vector2(23f, 6f),
        MagicianRadius = 0.6f,
        CriticTurnRadius = 0f,
        ThrowRange = 9f,
        ThrowCooldown = 0.5f,
        ThrownCardSpeed = 60f,
        ThrownCardDamage = 1f,
        ApplauseTime = 20f,
        ApplausePickUpReach = 0.4f,
        ApplauseBoxOfficeRadius = 0f,
        EncoreFirstCost = 1000,
    };

    [Test]
    public void Parse_TheCommittedKinds_TheScalperIsTheFifth_EatsApplauseAndNeverTurns_AndNoOtherKindEats()
    {
        Tuning tuning = CommittedTuning.Parse();

        Assert.Multiple(() =>
        {
            Assert.That(tuning.Scalper().Name, Is.EqualTo("scalper"));
            Assert.That(tuning.Scalper().TurnsOnTheMagician, Is.False);
            Assert.That(tuning.EnemyKinds.Select(kind => kind.EatsApplause), Is.EqualTo(new[] { false, false, false, false, true }));
        });
    }

    [Test]
    public void Step_WithNoApplauseOnTheFloor_AScalperWalksToTheBoxOfficeAndStrikesItAsAKindThatEatsNone()
    {
        Simulation scalper = Show(Scene, (0, DoorFar, Scalper));
        Simulation plain = Show(WithAScalperThat(eats: false), (0, DoorFar, Scalper));

        for (int tick = 0; tick < 10 * Simulation.TicksPerSecond; tick++)
        {
            scalper.Step(default);
            plain.Step(default);
            Assert.That(scalper.Critics[0].Position, Is.EqualTo(plain.Critics[0].Position), $"tick {tick}");
        }

        Assert.That(scalper.BoxOfficeHitPoints, Is.LessThan(Scene.BoxOfficeHitPoints));
        Assert.That(scalper.BoxOfficeHitPoints, Is.EqualTo(plain.BoxOfficeHitPoints));
    }

    [Test]
    public void Step_APieceLiesOnTheFloor_AScalperRunsStraightToItAndEatsItWhenItsCircleIsOnThePiece()
    {
        Simulation simulation = Show(Scene, (0, DoorA, Still), (60, DoorFar, Scalper));
        float distanceBefore = float.PositiveInfinity;
        int ticks = 0;
        while (!Happened(simulation, TickEventKind.ApplauseEaten))
        {
            if (simulation.Critics is [{ Kind: Scalper } before])
            {
                // Along the stage's top edge to the piece: the box office is down the stage.
                Assert.That(before.Position.Y, Is.Zero);
                distanceBefore = Vector2.Distance(before.Position, A);
            }

            simulation.Step(default);
            Assert.That(++ticks, Is.LessThan(10 * Simulation.TicksPerSecond), "the piece is never eaten");
        }

        // A tick earlier the piece was outside its circle; the step that brings the piece's middle onto the
        // circle is the one that eats it, and ends there.
        Critic scalper = simulation.Critics.Single();
        Assert.Multiple(() =>
        {
            Assert.That(distanceBefore, Is.GreaterThan(Scene.EnemyKinds[Scalper].Radius));
            Assert.That(Vector2.Distance(scalper.Position, A), Is.EqualTo(Scene.EnemyKinds[Scalper].Radius).Within(0.0001f));
            Assert.That(
                simulation.Events.Where(happened => happened.Kind == TickEventKind.ApplauseEaten),
                Is.EqualTo(new[] { new TickEvent(TickEventKind.ApplauseEaten, A, CriticId: scalper.Id) }));
            Assert.That(simulation.ApplauseOnTheFloor, Is.Empty);

            // Eaten is not picked up: the magician's counts are of what it picked up, and have nothing.
            Assert.That(simulation.ActApplause, Is.Zero);
            Assert.That(simulation.EncoreApplause, Is.Zero);
        });
    }

    [Test]
    public void Step_TwoPieces_AScalperEatsTheNearerFirstThoughItWasDroppedLast_ThenTheOther_ThenGoesToTheBoxOffice()
    {
        // The piece at B is dropped first and the one at A second; the scalper enters nearer to A.
        Simulation simulation = Show(Scene, (0, DoorB, Still), (30, DoorA, Still), (120, DoorFar, Scalper));
        var eaten = new List<Vector2>();
        bool struck = false;
        for (int tick = 0; tick < 15 * Simulation.TicksPerSecond && !struck; tick++)
        {
            simulation.Step(default);
            eaten.AddRange(simulation.Events
                .Where(happened => happened.Kind == TickEventKind.ApplauseEaten).Select(happened => happened.Position));
            struck = Happened(simulation, TickEventKind.BoxOfficeStruck);
        }

        Assert.Multiple(() =>
        {
            Assert.That(eaten, Is.EqualTo(new[] { A, B }));
            Assert.That(struck, "with none left it walks to the box office and strikes it");
            Assert.That(simulation.BoxOfficeHitPoints, Is.EqualTo(Scene.BoxOfficeHitPoints - Scene.EnemyKinds[Scalper].StrikeDamage));
        });
    }

    [Test]
    public void Step_TwoPiecesAsNear_AScalperGoesToTheOneThatWasDroppedFirst()
    {
        // The piece at B is dropped first, and the scalper enters as far from it as from the one at A.
        Simulation simulation = Show(Scene, (0, DoorB, Still), (30, DoorA, Still), (120, DoorBetween, Scalper));

        TicksUntil(simulation, TickEventKind.ApplauseEaten);

        Assert.That(simulation.Events.Single(happened => happened.Kind == TickEventKind.ApplauseEaten).Position, Is.EqualTo(B));
    }

    [Test]
    public void Step_AKindThatDoesNotEat_LeavesThePieceWhereItLies()
    {
        Simulation simulation = Show(WithAScalperThat(eats: false), (0, DoorA, Still), (60, DoorFar, Scalper));

        for (int tick = 0; tick < 10 * Simulation.TicksPerSecond; tick++)
        {
            simulation.Step(default);
            Assert.That(Happened(simulation, TickEventKind.ApplauseEaten), Is.False);
        }

        Assert.That(simulation.ApplauseOnTheFloor.Select(piece => piece.Position), Is.EqualTo(new[] { A }));
    }

    [Test]
    public void Step_TheMagicianAndAScalperReachAPieceOnOneTick_TheMagicianPicksItUp()
    {
        Simulation simulation = OnTheTickBefore(TickEventKind.ApplauseEaten, Scene, (0, DoorA, Still), (60, DoorFar, Scalper));

        // On the tick the scalper would eat it, the piece is in the magician's reach too: the magician picks up
        // right after it moves, before the critics walk.
        simulation.Tuning = Scene with { ApplausePickUpReach = 100f };
        simulation.Step(default);

        Assert.Multiple(() =>
        {
            Assert.That(Happened(simulation, TickEventKind.ApplausePickedUp));
            Assert.That(Happened(simulation, TickEventKind.ApplauseEaten), Is.False);
            Assert.That(simulation.ActApplause, Is.EqualTo(1));
        });
    }

    [Test]
    public void Step_AStunnedScalper_EatsNothing()
    {
        // A scalper that stands still enters exactly where a piece lies, and would eat it on its first tick: on
        // that tick a cloud covers the whole stage. The piece is inside its circle, and it eats nothing.
        Simulation simulation = Show(StandingStill, (0, DoorA, Still), (60, DoorA, Scalper));
        for (int tick = 0; tick <= 60; tick++)
        {
            simulation.Step(default);
        }

        Assert.That(simulation.Critics.Single().Position, Is.EqualTo(simulation.ApplauseOnTheFloor.Single().Position));
        simulation.Tuning = StandingStill with { VanishCloudRadius = 100f };
        simulation.Step(new MagicianInput(Vector2.Zero, Vanish: true));

        Assert.Multiple(() =>
        {
            Assert.That(simulation.Critics.Single().IsStunned);
            Assert.That(Happened(simulation, TickEventKind.ApplauseEaten), Is.False);
            Assert.That(simulation.ApplauseOnTheFloor, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Step_APieceDroppedWithinAScalpersStep_IsEatenOnTheTickItIsDropped()
    {
        // The scalper stands still at the far door until the tick the critic at A falls, and on that tick it is
        // quick enough to be there in one step: the cards fly before the critics walk, so the piece is on the
        // floor for its walk, and the magician, which picks up from the next tick, never has it.
        (int Tick, int Door, int Kind)[] plan = [(0, DoorFar, Scalper), (60, DoorA, Still)];
        Simulation simulation = OnTheTickBefore(TickEventKind.Kill, StandingStill, plan);

        simulation.Tuning = InOneStep;
        simulation.Step(default);

        Assert.Multiple(() =>
        {
            Assert.That(
                simulation.Events.Select(happened => happened.Kind).Where(kind => kind != TickEventKind.Throw),
                Is.EqualTo(new[] { TickEventKind.Kill, TickEventKind.ApplauseDropped, TickEventKind.ApplauseEaten }));
            Assert.That(simulation.ApplauseOnTheFloor, Is.Empty);
            Assert.That(simulation.ActApplause, Is.Zero);
        });
    }

    [Test]
    public void Step_TwoScalpersReachOnePieceOnOneTick_TheOneThatEnteredFirstEatsIt()
    {
        (int Tick, int Door, int Kind)[] plan = [(0, DoorFar, Scalper), (1, DoorFar, Scalper), (60, DoorA, Still)];
        Simulation simulation = OnTheTickBefore(TickEventKind.Kill, StandingStill, plan);

        simulation.Tuning = InOneStep;
        simulation.Step(default);

        Assert.Multiple(() =>
        {
            Assert.That(
                simulation.Events.Where(happened => happened.Kind == TickEventKind.ApplauseEaten).Select(happened => happened.CriticId),
                Is.EqualTo(new[] { 0 }));
            Assert.That(simulation.ApplauseOnTheFloor, Is.Empty);
        });
    }

    [Test]
    public void Step_AKindThatEatsAndHasTurnedOnTheMagician_HuntsTheMagicianAndEatsNothing()
    {
        // No committed kind does both; a reload can make one. The piece lies at A, and the scalper enters between
        // A and the magician with the whole stage inside the turn radius.
        Tuning both = Scene with
        {
            CriticTurnRadius = 100f,
            EnemyKinds = [Scene.Critic(), Scene.EnemyKinds[Scalper] with { TurnsOnTheMagician = true }],
        };
        Simulation simulation = Show(both, (0, DoorA, Still), (60, DoorBetween, Scalper));
        bool hurt = false;
        for (int tick = 0; tick < 5 * Simulation.TicksPerSecond; tick++)
        {
            simulation.Step(default);
            hurt |= Happened(simulation, TickEventKind.MagicianHurt);
            Assert.That(Happened(simulation, TickEventKind.ApplauseEaten), Is.False, $"tick {tick}");
        }

        Assert.Multiple(() =>
        {
            Assert.That(hurt, "it reached the magician");
            Assert.That(simulation.ApplauseOnTheFloor.Select(piece => piece.Position), Is.EqualTo(new[] { A }));
        });
    }

    [Test]
    public void Step_AScalperFallsToTheMagiciansOwnCard_OnePieceIsLeftWhereItFell()
    {
        // The scalper as the committed file has it, in range from the moment it enters.
        Tuning tuning = Scene with { EnemyKinds = [Scene.Critic(), CommittedTuning.Parse().Scalper()] };
        Simulation simulation = Show(tuning, (0, DoorBetween, Scalper));

        TicksUntil(simulation, TickEventKind.Kill);

        Vector2 fell = simulation.Events.Single(happened => happened.Kind == TickEventKind.Kill).Position;
        Assert.That(simulation.Events.Where(happened => happened.Kind != TickEventKind.Throw), Is.EqualTo(new[]
        {
            new TickEvent(TickEventKind.Kill, fell, CriticId: 0),
            new TickEvent(TickEventKind.ApplauseDropped, fell, CriticId: 0),
        }));
        Assert.That(simulation.ApplauseOnTheFloor.Select(piece => piece.Position), Is.EqualTo(new[] { fell }));
    }

    [Test]
    public void ComputeStateHash_APieceEatenAndAPieceLeft_AreTwoHashes()
    {
        // A scalper that stands still enters exactly where a piece lies, and eats it on its first tick; one
        // that does not eat stands on it. Nothing else is apart: an eaten piece is no state but the piece gone.
        Tuning eater = StandingStill;
        Tuning plain = eater with { EnemyKinds = [eater.Critic(), eater.EnemyKinds[Scalper] with { EatsApplause = false }] };
        Simulation one = Show(eater, (0, DoorA, Still), (60, DoorA, Scalper));
        Simulation other = Show(plain, (0, DoorA, Still), (60, DoorA, Scalper));

        for (int tick = 0; tick <= 61; tick++)
        {
            one.Step(default);
            other.Step(default);
        }

        Assert.Multiple(() =>
        {
            Assert.That(one.ApplauseOnTheFloor, Is.Empty);
            Assert.That(other.ApplauseOnTheFloor, Has.Count.EqualTo(1));
            Assert.That(one.Critics.Single().Position, Is.EqualTo(other.Critics.Single().Position));
            Assert.That(one.ComputeStateHash(), Is.Not.EqualTo(other.ComputeStateHash()));
        });
    }

    /// <summary><see cref="Scene"/> with a scalper that stands where it entered.</summary>
    private Tuning StandingStill => Scene with { EnemyKinds = [Scene.Critic(), Scene.EnemyKinds[Scalper] with { Speed = 0f }] };

    /// <summary><see cref="Scene"/> with a scalper that crosses the stage in one tick.</summary>
    private Tuning InOneStep => Scene with { EnemyKinds = [Scene.Critic(), Scene.EnemyKinds[Scalper] with { Speed = 1200f }] };

    /// <summary>A show played up to the tick before the one on which <paramref name="kind"/> first happens in it.</summary>
    private static Simulation OnTheTickBefore(TickEventKind kind, Tuning tuning, params (int Tick, int Door, int Kind)[] plan)
    {
        int on = TicksUntil(Show(tuning, plan), kind);
        Simulation simulation = Show(tuning, plan);
        for (int tick = 1; tick < on; tick++)
        {
            simulation.Step(default);
        }

        return simulation;
    }

    private Tuning WithAScalperThat(bool eats) =>
        Scene with { EnemyKinds = [Scene.Critic(), Scene.EnemyKinds[Scalper] with { EatsApplause = eats }] };

    private static Simulation Show(Tuning tuning, params (int Tick, int Door, int Kind)[] entries) =>
        new(tuning, seed: 1, [[.. entries.Select(entry => new PlannedEntry(entry.Tick, entry.Door, entry.Kind))]]);

    private static bool Happened(Simulation simulation, TickEventKind kind) =>
        simulation.Events.Any(happened => happened.Kind == kind);

    /// <summary>Plays a show until something of a kind happens, and says how many ticks that was: the last is its tick.</summary>
    private static int TicksUntil(Simulation simulation, TickEventKind kind)
    {
        for (int ticks = 1; ticks <= 20 * Simulation.TicksPerSecond; ticks++)
        {
            simulation.Step(default);
            if (Happened(simulation, kind))
            {
                return ticks;
            }
        }

        Assert.Fail($"No {kind} in twenty seconds.");
        return 0;
    }
}
