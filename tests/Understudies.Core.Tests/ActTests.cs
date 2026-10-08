using System.Numerics;

namespace Understudies.Core.Tests;

public class ActTests
{
    /// <summary>Two seconds: the act of <see cref="Scene"/>.</summary>
    private const int ActTicks = 2 * Simulation.TicksPerSecond;

    /// <summary>In the stage's top edge: where the one critic of <see cref="Scene"/> enters.</summary>
    private static readonly Vector2 Door = new(20f, 0f);

    /// <summary>Twenty units to the right of the critic's way down the stage, and further than a Vanish from every edge.</summary>
    private static readonly Vector2 Mark = new(40f, 13f);

    private static readonly MagicianInput Left = new(new Vector2(-1f, 0f));

    private static readonly MagicianInput Vanish = new(Vector2.Zero, Vanish: true);

    /// <summary>
    /// A performance of three acts of two seconds, with one critic. The door has no width, so the critic enters on
    /// the first tick exactly at <see cref="Door"/>; from the second tick on it walks a unit a tick straight down the
    /// stage, to a box office it touches 17.5 units below the door: it strikes on the nineteenth tick and every half
    /// second after, one of the box office's thousand hit points a strike. No other critic enters. The
    /// magician is far from all of it on <see cref="Mark"/>, throws at nobody, and no critic turns on it; a Vanish
    /// takes it six units.
    /// The curtain has no length, which is no curtain: these tests count their ticks from the first
    /// tick of an act, and the curtain has tests of its own.
    /// </summary>
    private Tuning Scene { get; } = CommittedTuning.Parse().WithCritic(critic => critic with { Speed = 60f, Radius = 0.5f }) with
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
        VanishDistance = 6f,
        ThrowRange = 0f,
        CriticTurnRadius = 0f,
        CriticStrikeDamage = 1f,
        CriticBlowCooldown = 0.5f,
    };

    [Test]
    public void Step_TheFirstActsTimer_CountsItsLengthDownATickATick()
    {
        var simulation = Shows.WithOneCritic(Scene);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.Act, Is.EqualTo(1));
        Assert.That(simulation.ActTicksLeft, Is.EqualTo(ActTicks));

        simulation.Step(default);

        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.Act, Is.EqualTo(1));
        Assert.That(simulation.ActTicksLeft, Is.EqualTo(ActTicks - 1));
    }

    [Test]
    public void Step_TheActsTimerRunsOut_TheActIsOverWithItsCriticStillOnTheStage()
    {
        var simulation = Shows.WithOneCritic(Scene);

        Run(simulation, ticks: ActTicks - 1);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.ActTicksLeft, Is.EqualTo(1));

        simulation.Step(default);

        // Nobody had to fall for the act to end: it is the first act that is over, and its critic stands.
        Assert.That(simulation.Phase, Is.EqualTo(Phase.BetweenActs));
        Assert.That(simulation.Act, Is.EqualTo(1));
        Assert.That(simulation.ActTicksLeft, Is.Zero);
        Assert.That(simulation.Critics, Has.Count.EqualTo(1));
    }

    [Test]
    public void Step_TheActsLastTick_IsPlayedInFull()
    {
        // An act of nineteen ticks: the critic's first strike is on the last of them.
        var simulation = Shows.WithOneCritic(Scene with { ActLength = 19f / Simulation.TicksPerSecond });

        Run(simulation, ticks: 19);

        Assert.That(simulation.Phase, Is.EqualTo(Phase.BetweenActs));
        Assert.That(simulation.BoxOfficeHitPoints, Is.EqualTo(999f));
    }

    [Test]
    public void Step_BetweenTwoActs_ChangesNothing()
    {
        // The act's last tick is a Vanish that goes nowhere: it is reported, its cloud lies on the floor with all
        // its time, its cooldown has begun, and nothing hurts the magician.
        var simulation = Shows.WithOneCritic(Scene with { VanishDistance = 0f });
        Run(simulation, ticks: ActTicks - 1);
        simulation.Step(Vanish);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.BetweenActs));
        Assert.That(simulation.Events, Is.Not.Empty);
        ulong state = simulation.ComputeStateHash();
        int cloud = simulation.Clouds[0].TicksLeft;
        Vector2 magicianBefore = simulation.MagicianPreviousPosition;
        Vector2 criticBefore = simulation.Critics[0].PreviousPosition;

        // Five seconds, in which the critic at the box office would strike ten times and the Vanish come back.
        for (int i = 0; i < 5 * Simulation.TicksPerSecond; i++)
        {
            simulation.Step(new MagicianInput(new Vector2(1f, 0f), Vanish: true));
        }

        // The timer stands with the rest. What only the view reads stands too, and nothing is reported.
        Assert.That(simulation.Phase, Is.EqualTo(Phase.BetweenActs));
        Assert.That(simulation.Act, Is.EqualTo(1));
        Assert.That(simulation.ActTicksLeft, Is.Zero);
        Assert.That(simulation.ComputeStateHash(), Is.EqualTo(state));
        Assert.That(simulation.MagicianPosition, Is.EqualTo(Mark));
        Assert.That(simulation.Clouds.Select(left => left.TicksLeft), Is.EqualTo(new[] { cloud }));
        Assert.That(simulation.MagicianPreviousPosition, Is.EqualTo(magicianBefore));
        Assert.That(simulation.Critics[0].PreviousPosition, Is.EqualTo(criticBefore));
        Assert.That(simulation.Events, Is.Empty);
    }

    [Test]
    public void GoOn_BetweenTwoActs_TheNextActBeginsWithTheCriticsOfTheLast()
    {
        var simulation = Shows.WithOneCritic(Scene);
        Run(simulation, ticks: ActTicks);
        Critic critic = simulation.Critics[0];
        Vector2 stood = critic.Position;

        simulation.GoOn();

        // The second act with its whole time, and the critic where it stood. Nothing has been played yet.
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.Act, Is.EqualTo(2));
        Assert.That(simulation.ActTicksLeft, Is.EqualTo(ActTicks));
        Assert.That(simulation.Critics, Is.EqualTo(new[] { critic }));
        Assert.That(critic.Position, Is.EqualTo(stood));

        simulation.Step(default);
        Assert.That(simulation.ActTicksLeft, Is.EqualTo(ActTicks - 1));
    }

    [Test]
    public void GoOn_AMagicianThatWasHurtFarFromItsMark_IsWholeAndOnTheMark()
    {
        // The critic turns on the magician wherever it is, and at a unit a tick it catches a magician that walks
        // away to the left: by the act's end it has hurt it more than once.
        Tuning tuning = Scene with { CriticTurnRadius = 100f };
        var simulation = Shows.WithOneCritic(tuning);
        Run(simulation, ticks: ActTicks, Left);
        Assert.That(simulation.MagicianHitPoints, Is.LessThan(tuning.MagicianHitPoints - 1f));
        Assert.That(simulation.MagicianPosition.X, Is.LessThan(Mark.X - 1f));

        simulation.GoOn();

        // There is nothing between the two positions for the view to draw: the magician is on the mark at once.
        Assert.That(simulation.MagicianHitPoints, Is.EqualTo(tuning.MagicianHitPoints));
        Assert.That(simulation.MagicianPosition, Is.EqualTo(Mark));
        Assert.That(simulation.MagicianPreviousPosition, Is.EqualTo(Mark));
    }

    [Test]
    public void GoOn_AfterAVanishOnTheActsLastTick_TheVanishIsReadyAndTheMomentNothingHurtsIsOver()
    {
        var simulation = Shows.WithOneCritic(Scene);
        Run(simulation, ticks: ActTicks - 1);
        simulation.Step(Vanish);
        Assert.That(simulation.VanishCooldownLeft, Is.EqualTo(1f));
        Assert.That(simulation.MagicianIsInvulnerable, Is.True);

        simulation.GoOn();

        Assert.That(simulation.VanishCooldownLeft, Is.Zero);
        Assert.That(simulation.MagicianIsInvulnerable, Is.False);

        // And a press on the next act's first tick is taken.
        simulation.Step(Vanish);
        Assert.That(simulation.Events, Is.EqualTo(new[] { new TickEvent(TickEventKind.Vanish, Mark) }));
    }

    [Test]
    public void GoOn_AMagicianThatLastWalkedToTheLeft_FacesDownTheStageAgain()
    {
        var simulation = Shows.WithOneCritic(Scene);
        Run(simulation, ticks: ActTicks, Left);

        simulation.GoOn();

        // A Vanish from standing goes the way the magician faces: down the stage, as when the first act began.
        simulation.Step(Vanish);
        Assert.That(simulation.MagicianPosition, Is.EqualTo(Mark + new Vector2(0f, 6f)));
    }

    [Test]
    public void GoOn_AMagicianThatNeverLeftItsMark_TheStageGoesOnAsOneLongActWould()
    {
        // The committed stage with a critic every 42 ticks, so that none is due on the tick after the act, and
        // nobody turning on the magician, which throws from its mark beside the box office: cards so slow that one
        // is in the air when the act ends. Ten ticks before twenty seconds are up, a Vanish that goes nowhere leaves
        // its cloud on the critics nearest the mark. One show's act ends there; the other's is twice as long.
        // No curtain: it would stand between the two acts of one show and not in the one act of the other. And
        // no applause, whose pieces have no time: it would be cleared between the two acts, and pay for a card.
        Tuning tuning = CommittedTuning.Parse() with
        {
            CurtainTime = 0f,
            ApplauseTime = 0f,
            CriticTurnRadius = 0f,
            ThrownCardSpeed = 0.3f,
            VanishDistance = 0f,
        };
        const int twentySeconds = 20 * Simulation.TicksPerSecond;
        var twoActs = Shows.WithACriticEvery(42, tuning with { ActLength = 20f });
        var oneAct = Shows.WithACriticEvery(42, tuning with { ActLength = 40f });
        foreach (Simulation simulation in new[] { twoActs, oneAct })
        {
            Run(simulation, ticks: twentySeconds - 10);
            simulation.Step(Vanish);
            Run(simulation, ticks: 9);
        }

        // Everything the act's end could wrongly clear is on the stage.
        Assert.That(twoActs.Phase, Is.EqualTo(Phase.BetweenActs));
        Assert.That(twoActs.Critics.Count(critic => critic.IsStunned), Is.GreaterThan(0));
        Assert.That(twoActs.Critics.Count(critic => critic.HitPoints < tuning.Critic().HitPoints), Is.GreaterThan(0));
        Assert.That(twoActs.ThrownCards, Is.Not.Empty);
        Assert.That(twoActs.Clouds, Is.Not.Empty);
        Assert.That(twoActs.BoxOfficeHitPoints, Is.LessThan(tuning.BoxOfficeHitPoints));
        object[] left = Stage(twoActs);

        twoActs.GoOn();

        // Going on moves nothing but the magician, which was on its mark and whole already.
        Assert.That(Stage(twoActs), Is.EqualTo(left));

        // Nor does it touch a countdown: the next critic, each critic's next blow and the end of its stun all come
        // on the tick they would have come on. The second act has what one long act has not, an understudy that
        // throws and vanishes where the first act's magician did. So from here nobody has a range and a Vanish
        // leaves no cloud, in either show: what is in the air and on the floor already goes on as it was. (That the
        // magician's throw comes when it would have come is in UnderstudyTests, where a card says who threw it.)
        foreach (Simulation simulation in new[] { twoActs, oneAct })
        {
            simulation.Tuning = simulation.Tuning with { ThrowRange = 0f, VanishCloudTime = 0f };
        }

        for (int i = 0; i < twentySeconds; i++)
        {
            twoActs.Step(default);
            oneAct.Step(default);
            Assert.That(Stage(twoActs), Is.EqualTo(Stage(oneAct)), $"{i + 1} ticks into the second act");
        }
    }

    [Test]
    public void GoOn_WhileAnActIsPlayed_DoesNothing()
    {
        var simulation = Shows.WithOneCritic(Scene);
        Run(simulation, ticks: ActTicks / 2, Left);
        ulong state = simulation.ComputeStateHash();
        Vector2 magicianBefore = simulation.MagicianPreviousPosition;

        simulation.GoOn();

        // The first act still, half played, with the magician where it has walked to.
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.Act, Is.EqualTo(1));
        Assert.That(simulation.ActTicksLeft, Is.EqualTo(ActTicks / 2));
        Assert.That(simulation.ComputeStateHash(), Is.EqualTo(state));
        Assert.That(simulation.MagicianPreviousPosition, Is.EqualTo(magicianBefore));
    }

    [Test]
    public void Step_TheLastActsTimerRunsOut_ThePerformanceEndsInTheOvationAndNothingComesAfterIt()
    {
        var simulation = Shows.WithOneCritic(Scene);
        Run(simulation, ticks: ActTicks);
        simulation.GoOn();
        Run(simulation, ticks: ActTicks);
        simulation.GoOn();
        Run(simulation, ticks: ActTicks - 1);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.Act, Is.EqualTo(3));

        // The third act is the last of this performance, and it ends on a Vanish.
        simulation.Step(Vanish);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Ovation));
        Assert.That(simulation.ShowClosed, Is.False);
        Assert.That(simulation.Events, Is.Not.Empty);
        ulong state = simulation.ComputeStateHash();
        Vector2 magicianBefore = simulation.MagicianPreviousPosition;
        Vector2 criticBefore = simulation.Critics[0].PreviousPosition;

        // There is no fourth act to go on to, and no tick changes anything.
        simulation.GoOn();
        Run(simulation, ticks: 5 * Simulation.TicksPerSecond, new MagicianInput(new Vector2(1f, 0f), Vanish: true));

        Assert.That(simulation.Phase, Is.EqualTo(Phase.Ovation));
        Assert.That(simulation.Act, Is.EqualTo(3));
        Assert.That(simulation.ActTicksLeft, Is.Zero);
        Assert.That(simulation.ComputeStateHash(), Is.EqualTo(state));
        Assert.That(simulation.MagicianPreviousPosition, Is.EqualTo(magicianBefore));
        Assert.That(simulation.Critics[0].PreviousPosition, Is.EqualTo(criticBefore));
        Assert.That(simulation.Events, Is.Empty);
    }

    [Test]
    public void Step_TenActs_EndThePerformance()
    {
        // Ten acts of a second: after each of the first nine the player goes on, and the tenth ends it.
        var simulation = Shows.WithOneCritic(Scene with { ActLength = 1f, ActsInPerformance = 10 });

        for (int act = 1; act <= 9; act++)
        {
            Run(simulation, ticks: Simulation.TicksPerSecond);
            Assert.That(simulation.Phase, Is.EqualTo(Phase.BetweenActs), $"after act {act}");
            simulation.GoOn();
        }

        Run(simulation, ticks: Simulation.TicksPerSecond - 1);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.Act, Is.EqualTo(10));

        simulation.Step(default);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Ovation));
        Assert.That(simulation.Act, Is.EqualTo(10));
    }

    [Test]
    public void Step_TheBoxOfficeFallsInAnAct_TheShowClosesAtOnce()
    {
        // Three hit points: the critic's third strike, on tick 79, takes the last of them.
        var simulation = Shows.WithOneCritic(Scene with { BoxOfficeHitPoints = 3f });
        Run(simulation, ticks: 78);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));

        simulation.Step(default);

        Assert.That(simulation.BoxOfficeHitPoints, Is.Zero);
        Assert.That(simulation.ShowClosed, Is.True);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Closed));

        // The act's time stands where the show closed, and there is no going on from there.
        simulation.GoOn();
        Run(simulation, ticks: ActTicks);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Closed));
        Assert.That(simulation.Act, Is.EqualTo(1));
        Assert.That(simulation.ActTicksLeft, Is.EqualTo(ActTicks - 79));
    }

    [TestCase(3, TestName = "Step_TheBoxOfficeFallsOnAnActsLastTick_TheShowHasClosedAndTheActIsNotJustOver")]
    [TestCase(1, TestName = "Step_TheBoxOfficeFallsOnTheLastActsLastTick_TheShowHasClosedAndThereIsNoOvation")]
    public void Step_TheBoxOfficeFallsOnAnActsLastTick_TheShowHasClosed(int acts)
    {
        // An act of nineteen ticks: the critic's first strike is on the last of them, and one is all it takes.
        Tuning tuning = Scene with
        {
            ActLength = 19f / Simulation.TicksPerSecond, ActsInPerformance = acts, BoxOfficeHitPoints = 1f,
        };
        var simulation = Shows.WithOneCritic(tuning);

        Run(simulation, ticks: 19);

        Assert.That(simulation.ActTicksLeft, Is.Zero);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Closed));
    }

    /// <summary>All that is on the stage but the magician.</summary>
    private static object[] Stage(Simulation simulation) =>
    [
        simulation.BoxOfficeHitPoints,
        simulation.Critics.Select(critic => (critic.Id, critic.Position, critic.HitPoints, critic.IsStunned)).ToList(),
        simulation.ThrownCards.Select(card => card.Position).ToList(),
        simulation.Clouds.Select(cloud => (cloud.Position, cloud.TicksLeft)).ToList(),
    ];

    private static void Run(Simulation simulation, int ticks, MagicianInput input = default)
    {
        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(input);
        }
    }
}
