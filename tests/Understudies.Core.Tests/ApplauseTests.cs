using System.Numerics;

namespace Understudies.Core.Tests;

public class ApplauseTests
{
    /// <summary>Two seconds: the act of <see cref="Scene"/>.</summary>
    private const int ActTicks = 2 * Simulation.TicksPerSecond;

    /// <summary>In the stage's top edge: where every critic of <see cref="Scene"/> enters, stands and falls.</summary>
    private static readonly Vector2 Door = new(20f, 0f);

    /// <summary>Six units straight below the door: in range of it, and far out of reach of a piece that lies there.</summary>
    private static readonly Vector2 Mark = new(20f, 6f);

    /// <summary>
    /// Right below the door, as high as the floor lets the magician stand: a piece at the door is in its reach.
    /// </summary>
    private static readonly Vector2 AtTheDoor = new(20f, 0.6f);

    private static readonly MagicianInput Up = new(new Vector2(0f, -1f));

    private static readonly MagicianInput Down = new(new Vector2(0f, 1f));

    private static readonly MagicianInput Right = new(new Vector2(1f, 0f));

    /// <summary>
    /// A performance of three acts of two seconds whose critics stand still and fall to one card. The door has no
    /// width and a critic no speed, so each enters exactly at <see cref="Door"/> and stays there, and no critic
    /// turns on the magician. The magician stands on <see cref="Mark"/>, walks a quarter of a unit a tick, throws
    /// every half second at what is within nine units, and its card flies a unit a tick: a critic that enters on
    /// the first tick falls on the eighth. The magician's circle is 0.6 and it reaches 0.4 past it: a piece no
    /// further than one unit from its centre is picked up. A piece lies for four seconds, longer than an act.
    /// An encore costs more pieces than an act has.
    /// The stage has no back wall and the curtain no length: these tests count their ticks from the first tick
    /// of an act.
    /// </summary>
    private Tuning Scene { get; } = CommittedTuning.Parse().WithCritic(
        critic => critic with { Speed = 0f, Radius = 0.5f, HitPoints = 1f }) with
    {
        CurtainTime = 0f,
        ActLength = 2f,
        ActsInPerformance = 3,
        StageFloorTop = 0f,
        StageDoors = [new StageDoor(Door, 1)],
        StageDoorWidth = 0f,
        MagicianMark = Mark,
        MagicianSpeed = 15f,
        MagicianRadius = 0.6f,
        CriticTurnRadius = 0f,
        ThrowRange = 9f,
        ThrowCooldown = 0.5f,
        ThrownCardSpeed = 60f,
        ThrownCardDamage = 1f,
        ApplauseTime = 4f,
        ApplausePickUpReach = 0.4f,
        EncoreFirstCost = 1000,
    };

    /// <summary>
    /// <see cref="Scene"/> with the magician at the door and a throw on every tick: a critic falls two ticks after
    /// it enters, and on the third its piece is picked up, if the magician still stands there.
    /// </summary>
    private Tuning PointBlank => Scene with { MagicianMark = AtTheDoor, ThrowCooldown = 1f / Simulation.TicksPerSecond };

    [Test]
    public void Step_ACriticFallsToTheMagiciansOwnCard_OnePieceIsLeftWhereItFell()
    {
        Simulation simulation = Shows.WithOneCritic(Scene);
        Assert.That(simulation.ApplauseOnTheFloor, Is.Empty);

        StepUntil(simulation, TickEventKind.Kill);

        // The kill first, then what it left, both at the critic's place: the piece has its whole time.
        Assert.That(simulation.Events, Is.EqualTo(new[]
        {
            new TickEvent(TickEventKind.Kill, Door, CriticId: 0),
            new TickEvent(TickEventKind.ApplauseDropped, Door, CriticId: 0),
        }));
        Assert.That(simulation.ApplauseOnTheFloor, Has.Count.EqualTo(1));
        Assert.That(simulation.ApplauseOnTheFloor[0].Position, Is.EqualTo(Door));
        Assert.That(simulation.ApplauseOnTheFloor[0].TicksLeft, Is.EqualTo(4 * Simulation.TicksPerSecond));

        // Dropped is not picked up.
        Assert.That(simulation.ActApplause, Is.Zero);
    }

    [Test]
    public void Step_AStagehandFallsToTheMagiciansOwnCard_OnePieceIsLeftWhereItFell()
    {
        // The stagehand as the committed file has it, running from the door to the committed box office: the
        // magician's card meets it on its way.
        Tuning tuning = Scene with { EnemyKinds = [Scene.Critic(), CommittedTuning.Parse().Stagehand()] };
        Simulation simulation = Shows.WithOneOfKind(tuning, kind: 1);

        StepUntil(simulation, TickEventKind.Kill);

        Vector2 fell = simulation.Events[0].Position;
        Assert.That(fell, Is.Not.EqualTo(Door));
        Assert.That(simulation.Events, Is.EqualTo(new[]
        {
            new TickEvent(TickEventKind.Kill, fell, CriticId: 0),
            new TickEvent(TickEventKind.ApplauseDropped, fell, CriticId: 0),
        }));
        Assert.That(simulation.ApplauseOnTheFloor.Select(piece => piece.Position), Is.EqualTo(new[] { fell }));
    }

    [Test]
    public void Step_ARivalFallsToTheMagiciansOwnCard_OnePieceIsLeftWhereItFell()
    {
        // The rival's understudy as the committed file has it, but for hit points that two cards take: it walks
        // from the door toward the committed box office, sixteen units off, and falls on its way, far outside
        // the floor on which a fall earns nothing.
        Tuning committed = CommittedTuning.Parse();
        Tuning tuning = Scene with
        {
            EnemyKinds = [Scene.Critic(), committed.Stagehand(), committed.Rival() with { HitPoints = 2f }],
        };
        Simulation simulation = Shows.WithOneOfKind(tuning, kind: 2);

        StepUntil(simulation, TickEventKind.Kill);

        Vector2 fell = simulation.Events[0].Position;
        Assert.That(simulation.Events, Is.EqualTo(new[]
        {
            new TickEvent(TickEventKind.Kill, fell, CriticId: 0),
            new TickEvent(TickEventKind.ApplauseDropped, fell, CriticId: 0),
        }));
        Assert.That(simulation.ApplauseOnTheFloor.Select(piece => piece.Position), Is.EqualTo(new[] { fell }));
    }

    [Test]
    public void Step_ACriticFallsToAnUnderstudysCard_NothingIsDropped()
    {
        // The first act is stood through on the mark, with nobody on the stage. In the second the magician walks
        // away to the right: when the critic enters, a second into the act, it is fifteen units off and out of
        // range, and the understudy on the mark is the only one to throw.
        var simulation = new Simulation(Scene, seed: 1, [[], [new PlannedEntry(Simulation.TicksPerSecond, Door: 0, Kind: 0)]]);
        PlayTheAct(simulation, _ => default);
        simulation.GoOn();

        List<TickEvent> events = PlayTheAct(simulation, _ => Right);

        Assert.That(Count(events, TickEventKind.Kill), Is.EqualTo(1));
        Assert.That(Count(events, TickEventKind.ApplauseDropped), Is.Zero);
        Assert.That(simulation.ApplauseOnTheFloor, Is.Empty);

        // And that far from the box office, where the magician's own card would have left a piece.
        Assert.That(Vector2.Distance(Door, Scene.BoxOfficePosition), Is.GreaterThan(Scene.ApplauseBoxOfficeRadius));
    }

    // No applause by the box office (plan decision 27). The box office stands eight units straight below the
    // door, where the critic enters, stands and falls: the fall is eight from its middle, to the last digit.
    [TestCase(8.5f, 0, TestName = "Step_ACriticFallsNearerToTheBoxOfficeThanTheRadius_NothingIsDropped")]
    [TestCase(8f, 1, TestName = "Step_ACriticFallsExactlyOnTheRadius_OnePieceIsDropped")]
    [TestCase(7.5f, 1, TestName = "Step_ACriticFallsOutsideTheRadius_OnePieceIsDropped")]
    [TestCase(0f, 1, TestName = "Step_WithNoRadius_ACriticThatFallsByTheBoxOffice_LeavesItsPiece")]
    public void Step_ACriticFallsByTheBoxOffice_TheRadiusDecides(float radius, int pieces)
    {
        Tuning tuning = Scene with { BoxOfficePosition = Door + new Vector2(0f, 8f), ApplauseBoxOfficeRadius = radius };
        Simulation simulation = Shows.WithOneCritic(tuning);

        StepUntil(simulation, TickEventKind.Kill);

        // The fall is a fall either way: only the piece is missing.
        Assert.That(simulation.Events[0], Is.EqualTo(new TickEvent(TickEventKind.Kill, Door, CriticId: 0)));
        Assert.That(simulation.Events, Has.Count.EqualTo(1 + pieces));
        Assert.That(simulation.ApplauseOnTheFloor, Has.Count.EqualTo(pieces));
    }

    [Test]
    public void Step_ACriticFallsTouchingTheBoxOffice_NothingIsDropped_WhoeverStandsWhere()
    {
        // The committed radius about a box office the critic touches, with the magician six units away from
        // the fall: it is the critic's place that counts, and not the magician's.
        Tuning tuning = Scene with { BoxOfficePosition = Door + new Vector2(0f, 2.5f), BoxOfficeSize = 4f, MagicianMark = Mark + new Vector2(0f, 6f), ThrowRange = 15f };
        Assert.That(tuning.ApplauseBoxOfficeRadius, Is.InRange(2.5f, 9f), "the committed radius reaches the fall and not the magician");
        Simulation simulation = Shows.WithOneCritic(tuning);

        StepUntil(simulation, TickEventKind.Kill);

        Assert.That(simulation.Events, Is.EqualTo(new[] { new TickEvent(TickEventKind.Kill, Door, CriticId: 0) }));
        Assert.That(simulation.ApplauseOnTheFloor, Is.Empty);
    }

    // The magician and the understudy of an act it stood through are on one mark and throw on one tick, the
    // magician first: its card lands first. Of two hit points the understudy's card takes the last. Of three, the
    // two cards leave one, and half a second later the magician's next card takes it.
    [TestCase(2f, 0, TestName = "Step_AnUnderstudysCardFellsACriticTheMagicianHurt_NothingIsDropped")]
    [TestCase(3f, 1, TestName = "Step_TheMagiciansCardFellsACriticAnUnderstudyHurt_OnePieceIsDropped")]
    public void Step_ACriticHurtByBoth_TheCardThatFellsItCounts(float hitPoints, int pieces)
    {
        Tuning tuning = Scene.WithCritic(critic => critic with { HitPoints = hitPoints });
        var simulation = new Simulation(tuning, seed: 1, [[], [new PlannedEntry(0, Door: 0, Kind: 0)]]);
        PlayTheAct(simulation, _ => default);
        simulation.GoOn();

        List<TickEvent> events = PlayTheAct(simulation, _ => default);

        Assert.That(Count(events, TickEventKind.Kill), Is.EqualTo(1));
        Assert.That(Count(events, TickEventKind.ApplauseDropped), Is.EqualTo(pieces));
        Assert.That(simulation.ApplauseOnTheFloor, Has.Count.EqualTo(pieces));
    }

    [Test]
    public void Step_TheMagicianWalksOverAPiece_PicksItUpAndReportsItOnce()
    {
        Simulation simulation = Shows.WithOneCritic(Scene);
        StepUntil(simulation, TickEventKind.Kill);

        // Up to the door, as far as the floor goes, and on the spot there to the end of the act.
        List<TickEvent> events = PlayTheAct(simulation, _ => Up);

        Assert.That(simulation.MagicianPosition, Is.EqualTo(AtTheDoor));
        Assert.That(
            events.Where(happened => happened.Kind == TickEventKind.ApplausePickedUp),
            Is.EqualTo(new[] { new TickEvent(TickEventKind.ApplausePickedUp, Door) }));
        Assert.That(simulation.ActApplause, Is.EqualTo(1));
        Assert.That(simulation.ApplauseOnTheFloor, Is.Empty);
    }

    [Test]
    public void Step_APieceJustOutOfReach_StaysUntilTheMagicianIsAStepNearer()
    {
        // A tenth of a unit further from the door than the magician reaches.
        Simulation simulation = Shows.WithOneCritic(Scene with { MagicianMark = Door + new Vector2(0f, 1.1f) });
        StepUntil(simulation, TickEventKind.Kill);

        Run(simulation, ticks: 30);
        Assert.That(simulation.ApplauseOnTheFloor, Has.Count.EqualTo(1));
        Assert.That(simulation.ActApplause, Is.Zero);

        // A step is a quarter of a unit.
        simulation.Step(Up);
        Assert.That(simulation.ApplauseOnTheFloor, Is.Empty);
        Assert.That(simulation.ActApplause, Is.EqualTo(1));
    }

    [Test]
    public void Step_APieceNobodyPicksUp_IsGoneAfterItsTime()
    {
        Simulation simulation = Shows.WithOneCritic(Scene with { ApplauseTime = 0.5f });
        StepUntil(simulation, TickEventKind.Kill);
        Assert.That(simulation.ApplauseOnTheFloor[0].TicksLeft, Is.EqualTo(30));

        Run(simulation, ticks: 29);
        Assert.That(simulation.ApplauseOnTheFloor, Has.Count.EqualTo(1));
        Assert.That(simulation.ApplauseOnTheFloor[0].TicksLeft, Is.EqualTo(1));

        simulation.Step(default);
        Assert.That(simulation.ApplauseOnTheFloor, Is.Empty);
        Assert.That(simulation.Events, Is.Empty);
        Assert.That(simulation.ActApplause, Is.Zero);
    }

    [Test]
    public void Step_APieceIsDropped_ItIsNotPickedUpOnThatTick()
    {
        // The magician stands at the door, with the place of the fall in its reach all along: it picks up after
        // it moves and before the cards fly, so what a card of this tick drops waits for the next.
        Simulation simulation = Shows.WithOneCritic(PointBlank);

        StepUntil(simulation, TickEventKind.Kill);
        Assert.That(simulation.ApplauseOnTheFloor, Has.Count.EqualTo(1));
        Assert.That(simulation.ActApplause, Is.Zero);

        simulation.Step(default);
        Assert.That(simulation.ApplauseOnTheFloor, Is.Empty);
        Assert.That(simulation.ActApplause, Is.EqualTo(1));
    }

    // A piece that lies for thirty ticks, a tenth of a unit out of reach of a magician that then takes one step
    // to it. After twenty-eight ticks the step finds the piece with one tick left after it. After twenty-nine the
    // piece was still seen, with its last tick left, and the step's own tick takes it away before the magician
    // has moved: it can be picked up on one tick fewer than it is seen.
    [TestCase(28, 1, TestName = "Step_APieceWithTwoTicksLeft_IsPickedUp")]
    [TestCase(29, 0, TestName = "Step_APieceOnItsLastTick_IsGoneBeforeTheMagicianReachesIt")]
    public void Step_APieceFadesBeforeTheMagicianPicksUp(int ticksStood, int pieces)
    {
        Simulation simulation = Shows.WithOneCritic(
            Scene with { MagicianMark = Door + new Vector2(0f, 1.1f), ApplauseTime = 0.5f });
        StepUntil(simulation, TickEventKind.Kill);
        Run(simulation, ticksStood);
        Assert.That(simulation.ApplauseOnTheFloor, Has.Count.EqualTo(1));

        simulation.Step(Up);

        Assert.That(simulation.ApplauseOnTheFloor, Is.Empty);
        Assert.That(simulation.ActApplause, Is.EqualTo(pieces));
    }

    [Test]
    public void Step_AnUnderstudyWalksOverAPiece_LeavesIt()
    {
        // The first act, with nobody on the stage: half a second on the mark, then up to the door and on the spot
        // there. In the second act the magician stands on the mark and fells the critic at the door, and the
        // understudy walks onto the piece and stands on it for more than a second.
        var simulation = new Simulation(Scene, seed: 1, [[], [new PlannedEntry(0, Door: 0, Kind: 0)]]);
        PlayTheAct(simulation, tick => tick < 30 ? default : Up);
        simulation.GoOn();

        List<TickEvent> events = PlayTheAct(simulation, _ => default);

        Assert.That(simulation.Understudies[0].Position, Is.EqualTo(AtTheDoor));
        Assert.That(Count(events, TickEventKind.ApplauseDropped), Is.EqualTo(1));
        Assert.That(Count(events, TickEventKind.ApplausePickedUp), Is.Zero);
        Assert.That(simulation.ApplauseOnTheFloor, Has.Count.EqualTo(1));
        Assert.That(simulation.ActApplause, Is.Zero);
    }

    [Test]
    public void Step_AFallenMagician_PicksUpNothing()
    {
        // The magician fells the first critic from just out of reach of where it falls, and throws no second
        // card. Then critics turn on a magician that near and walk: the second enters where the first fell,
        // touches the magician at once, and one touch fells it.
        Tuning tuning = Scene with
        {
            MagicianMark = Door + new Vector2(0f, 1.1f),
            MagicianHitPoints = 1f,
            CriticTouchDamage = 1f,
            ThrowCooldown = 3600f,
        };
        Simulation simulation = Shows.WithCriticsOnTicks(tuning, 0, 30);
        StepUntil(simulation, TickEventKind.Kill);
        simulation.Tuning = tuning.WithCritic(critic => critic with { Speed = 60f }) with { CriticTurnRadius = 3f };
        StepUntil(simulation, TickEventKind.MagicianFell);
        Assert.That(simulation.ApplauseOnTheFloor, Has.Count.EqualTo(1));

        // A reach that takes in the piece from where the magician lies.
        simulation.Tuning = simulation.Tuning with { ApplausePickUpReach = 5f };
        Run(simulation, ticks: 10, Up);

        Assert.That(simulation.MagicianHasFallen, Is.True);
        Assert.That(simulation.ApplauseOnTheFloor, Has.Count.EqualTo(1));
        Assert.That(simulation.ActApplause, Is.Zero);
    }

    [Test]
    public void GoOn_TheActsApplauseStartsAgain_AndWhatLiesOnTheFloorIsCleared()
    {
        // The magician fells the first critic, walks up and picks its piece up, and walks back down past the mark
        // before the second critic enters, a second into the act: that one's piece lies at the door when the act
        // is over.
        Simulation simulation = Shows.WithCriticsOnTicks(Scene, 0, Simulation.TicksPerSecond);
        PlayTheAct(simulation, tick => tick switch
        {
            < 8 => default,
            < 32 => Up,
            < 56 => Down,
            _ => default,
        });

        // The act that is over still says what it picked up.
        Assert.That(simulation.Phase, Is.EqualTo(Phase.BetweenActs));
        Assert.That(simulation.ActApplause, Is.EqualTo(1));
        Assert.That(simulation.ApplauseOnTheFloor, Has.Count.EqualTo(1));

        simulation.GoOn();

        Assert.That(simulation.ActApplause, Is.Zero);
        Assert.That(simulation.ApplauseOnTheFloor, Is.Empty);
    }

    [Test]
    public void ComputeStateHash_TwoPiecesWithTwoTimesLeft_AreTwoHashes()
    {
        Simulation one = Shows.WithOneCritic(Scene);
        Simulation other = Shows.WithOneCritic(Scene with { ApplauseTime = 8f });

        StepUntil(one, TickEventKind.Kill);
        StepUntil(other, TickEventKind.Kill);

        Assert.That(other.ComputeStateHash(), Is.Not.EqualTo(one.ComputeStateHash()));
    }

    [Test]
    public void ComputeStateHash_TwoActsWithTwoCountsOfApplause_AreTwoHashes()
    {
        // The critic falls on the third tick. A piece that lies for two ticks is picked up on the fourth; one
        // that lies for one is gone before the magician can. Nothing is on either floor after that: the two shows
        // differ only in what the act has picked up.
        Simulation one = Shows.WithOneCritic(PointBlank with { ApplauseTime = 2f / Simulation.TicksPerSecond });
        Simulation other = Shows.WithOneCritic(PointBlank with { ApplauseTime = 1f / Simulation.TicksPerSecond });

        Run(one, ticks: 4);
        Run(other, ticks: 4);

        Assert.That(one.ApplauseOnTheFloor, Is.Empty);
        Assert.That(other.ApplauseOnTheFloor, Is.Empty);
        Assert.That((one.ActApplause, other.ActApplause), Is.EqualTo((1, 0)));
        Assert.That(other.ComputeStateHash(), Is.Not.EqualTo(one.ComputeStateHash()));
    }

    private static int Count(List<TickEvent> events, TickEventKind kind) =>
        events.Count(happened => happened.Kind == kind);

    private static void Run(Simulation simulation, int ticks, MagicianInput input = default)
    {
        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(input);
        }
    }

    /// <summary>Stands until a tick reports <paramref name="kind"/>, which is then among the events.</summary>
    private static void StepUntil(Simulation simulation, TickEventKind kind)
    {
        do
        {
            Assert.That(simulation.Phase, Is.EqualTo(Phase.Act), $"The act was over before any {kind}.");
            simulation.Step(default);
        }
        while (simulation.Events.All(happened => happened.Kind != kind));
    }

    /// <summary>
    /// Plays what is left of the act by <paramref name="script"/>, which is asked for each tick of the act by its
    /// number, the first being 0. All that the ticks reported, in order.
    /// </summary>
    private static List<TickEvent> PlayTheAct(Simulation simulation, Func<int, MagicianInput> script)
    {
        var events = new List<TickEvent>();
        int first = ActTicks - simulation.ActTicksLeft;
        for (int tick = first; simulation.Phase == Phase.Act; tick++)
        {
            simulation.Step(script(tick));
            events.AddRange(simulation.Events);
        }

        return events;
    }
}
