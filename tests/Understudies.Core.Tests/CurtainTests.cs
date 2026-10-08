using System.Numerics;

namespace Understudies.Core.Tests;

public class CurtainTests
{
    /// <summary>One second: the curtain of <see cref="Scene"/>.</summary>
    private const int CurtainTicks = Simulation.TicksPerSecond;

    /// <summary>Two seconds: the act of <see cref="Scene"/>.</summary>
    private const int ActTicks = 2 * Simulation.TicksPerSecond;

    private static readonly MagicianInput Left = new(new Vector2(-1f, 0f));

    private static readonly MagicianInput Vanish = new(Vector2.Zero, Vanish: true);

    /// <summary>
    /// The committed stage with a curtain of one second before every act of two, in a performance of three. The
    /// magician throws at nobody and no critic turns on it.
    /// </summary>
    private Tuning Scene { get; } = CommittedTuning.Parse() with
    {
        CurtainTime = 1f,
        ActLength = 2f,
        ActsInPerformance = 3,
        ThrowRange = 0f,
        CriticTurnRadius = 0f,
    };

    [Test]
    public void Phase_ANewPerformance_OpensWithTheCurtainOfItsFirstAct()
    {
        var simulation = new Simulation(Scene, seed: 1);

        Assert.That(simulation.Phase, Is.EqualTo(Phase.Curtain));
        Assert.That(simulation.Act, Is.EqualTo(1));
        Assert.That(simulation.CurtainLeft, Is.EqualTo(1f));
    }

    [Test]
    public void Step_TheCurtain_IsUpForItsTimeAndThenTheActIsPlayed()
    {
        var simulation = new Simulation(Scene, seed: 1);

        Run(simulation, CurtainTicks / 4);
        Assert.That(simulation.CurtainLeft, Is.EqualTo(0.75f));

        Run(simulation, CurtainTicks - (CurtainTicks / 4) - 1);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Curtain));

        simulation.Step(default);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.CurtainLeft, Is.Zero);
    }

    [Test]
    public void Phase_ACurtainOfNoLength_IsNoCurtain()
    {
        var simulation = new Simulation(Scene with { CurtainTime = 0f }, seed: 1);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));

        Run(simulation, ActTicks);
        simulation.GoOn();

        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.Act, Is.EqualTo(2));
    }

    [Test]
    public void Step_WhileTheCurtainOfTheFirstActIsUp_NoCriticIsLetInAndTheActsTimeStands()
    {
        var simulation = new Simulation(Scene, seed: 1);

        for (int i = 0; i < CurtainTicks; i++)
        {
            simulation.Step(Left);

            Assert.That(simulation.Critics, Is.Empty);
            Assert.That(simulation.MagicianPosition, Is.EqualTo(Scene.MagicianMark));
            Assert.That(simulation.ActTicksLeft, Is.EqualTo(ActTicks));
        }

        // The first tick of the act is the first after the curtain: the first critic enters on it.
        simulation.Step(Left);

        Assert.That(simulation.Critics, Has.Count.EqualTo(1));
        Assert.That(simulation.MagicianPosition, Is.Not.EqualTo(Scene.MagicianMark));
        Assert.That(simulation.ActTicksLeft, Is.EqualTo(ActTicks - 1));
    }

    [Test]
    public void Step_WhileTheCurtainIsUp_NothingMovesStrikesOrIsReleased()
    {
        // The committed stage with a critic every 42 ticks and a magician that throws from its mark beside the box
        // office, cards so slow that some are in the air when the act ends. Ten ticks before the first act's twenty
        // seconds are up a Vanish that goes nowhere leaves its cloud. So the second act's curtain rises on critics
        // that walk in from the door, critics that strike the box office, cards in the air, a cloud that thins, a
        // critic that is due, and an understudy. Through the curtain the magician is asked to walk and to vanish.
        Tuning tuning = CommittedTuning.Parse() with
        {
            CurtainTime = 1f,
            ActLength = 20f,
            CriticEntryInterval = 0.7f,
            CriticTurnRadius = 0f,
            ThrownCardSpeed = 1.2f,
            VanishDistance = 0f,
        };
        const int twentySeconds = 20 * Simulation.TicksPerSecond;
        var simulation = new Simulation(tuning, seed: 1);
        Run(simulation, CurtainTicks + twentySeconds - 10);
        simulation.Step(Vanish);
        Run(simulation, 9);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.BetweenActs));
        simulation.GoOn();
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Curtain));
        Assert.That(simulation.Critics, Is.Not.Empty);
        Assert.That(simulation.ThrownCards, Is.Not.Empty);
        Assert.That(simulation.Clouds, Is.Not.Empty);
        Assert.That(simulation.Understudies, Has.Count.EqualTo(1));
        string stage = Stage(simulation);
        ulong hashOfTheLastTick = 0;

        var walkAndVanish = new MagicianInput(new Vector2(-1f, 0f), Vanish: true);
        for (int i = 0; i < CurtainTicks; i++)
        {
            simulation.Step(walkAndVanish);

            Assert.That(Stage(simulation), Is.EqualTo(stage));
            Assert.That(simulation.Events, Is.Empty);

            // And the curtain's own time does go by: no two of its ticks are one state.
            Assert.That(simulation.ComputeStateHash(), Is.Not.EqualTo(hashOfTheLastTick));
            hashOfTheLastTick = simulation.ComputeStateHash();
        }

        // What stood through the curtain was not at rest: a second of the act moves, lets in and strikes.
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        int lastToEnter = simulation.Critics[^1].Id;
        float boxOffice = simulation.BoxOfficeHitPoints;
        Run(simulation, CurtainTicks);
        Assert.That(Stage(simulation), Is.Not.EqualTo(stage));
        Assert.That(simulation.Critics[^1].Id, Is.GreaterThan(lastToEnter));
        Assert.That(simulation.BoxOfficeHitPoints, Is.LessThan(boxOffice));
    }

    [Test]
    public void Step_AVanishAskedForWhileTheCurtainIsUp_IsNotKeptForTheAct()
    {
        var simulation = new Simulation(Scene, seed: 1);
        Run(simulation, CurtainTicks - 1);

        simulation.Step(Vanish);
        simulation.Step(default);

        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.Events, Is.Empty);
        Assert.That(simulation.Clouds, Is.Empty);
        Assert.That(simulation.MagicianPosition, Is.EqualTo(Scene.MagicianMark));
        Assert.That(simulation.VanishCooldownLeft, Is.Zero);
    }

    [Test]
    public void GoOn_BetweenTwoActs_TheNextActBeginsWithItsCurtain()
    {
        var simulation = new Simulation(Scene, seed: 1);
        Run(simulation, CurtainTicks + ActTicks);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.BetweenActs));

        simulation.GoOn();

        Assert.That(simulation.Phase, Is.EqualTo(Phase.Curtain));
        Assert.That(simulation.Act, Is.EqualTo(2));
        Assert.That(simulation.CurtainLeft, Is.EqualTo(1f));
        Assert.That(simulation.ActTicksLeft, Is.EqualTo(ActTicks));
    }

    [Test]
    public void GoOn_WhileTheCurtainIsUp_DoesNothing()
    {
        var simulation = new Simulation(Scene, seed: 1);
        Run(simulation, CurtainTicks + ActTicks);
        simulation.GoOn();
        Run(simulation, 10);
        ulong hash = simulation.ComputeStateHash();

        simulation.GoOn();

        Assert.That(simulation.Act, Is.EqualTo(2));
        Assert.That(simulation.ComputeStateHash(), Is.EqualTo(hash));
    }

    [Test]
    public void GoOn_AnActPlayedAfterItsCurtain_IsRecordedFromItsFirstTickAndNotFromTheCurtains()
    {
        // Left is held from the first tick of the curtain, a quarter of a unit a tick. The understudy's route is
        // the act's two seconds and not the curtain's one as well, and its first place is one step from the mark.
        Tuning tuning = Scene with { MagicianMark = new Vector2(40f, 13f), MagicianSpeed = 15f };
        var simulation = new Simulation(tuning, seed: 1);
        for (int i = 0; i < CurtainTicks + ActTicks; i++)
        {
            simulation.Step(Left);
        }

        simulation.GoOn();

        Understudy understudy = simulation.Understudies[0];
        Assert.That(understudy.Route, Has.Count.EqualTo(ActTicks));
        Assert.That(understudy.Route[0], Is.EqualTo(new Vector2(39.75f, 13f)));

        // Through the next curtain it stands on that first place.
        Run(simulation, CurtainTicks);
        Assert.That(understudy.Position, Is.EqualTo(new Vector2(39.75f, 13f)));
        Assert.That(understudy.IsOnStage, Is.True);
    }

    [Test]
    public void GoOn_CriticsAndCardsOfTheLastAct_HaveNothingBehindThemForTheViewToDrawFrom()
    {
        // A critic that walks and a slow card in the air when the first act ends: each was somewhere else a tick
        // before. From going on, the view has nothing between that place and where each stands.
        Tuning tuning = Scene with { ThrowRange = 30f, ThrownCardSpeed = 1.2f, CurtainTime = 0f };
        var simulation = new Simulation(tuning, seed: 1);
        Run(simulation, ActTicks);
        Assert.That(simulation.Critics[0].PreviousPosition, Is.Not.EqualTo(simulation.Critics[0].Position));
        Assert.That(simulation.ThrownCards[0].PreviousPosition, Is.Not.EqualTo(simulation.ThrownCards[0].Position));

        simulation.GoOn();

        Assert.That(simulation.Critics[0].PreviousPosition, Is.EqualTo(simulation.Critics[0].Position));
        Assert.That(simulation.ThrownCards[0].PreviousPosition, Is.EqualTo(simulation.ThrownCards[0].Position));
    }

    [Test]
    public void ComputeStateHash_TwoCurtainsWithDifferentTimeLeft_AreTwoHashes()
    {
        // Both shows are in the curtain of their first act and nothing else has happened in either: one curtain
        // has a second left and the other two.
        var one = new Simulation(Scene with { CurtainTime = 1f }, seed: 7);
        var other = new Simulation(Scene with { CurtainTime = 2f }, seed: 7);

        Assert.That(other.ComputeStateHash(), Is.Not.EqualTo(one.ComputeStateHash()));
    }

    private static void Run(Simulation simulation, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(default);
        }
    }

    /// <summary>Everything on the stage that could move, strike or be released, written out.</summary>
    private static string Stage(Simulation simulation) => string.Join(
        '|',
        simulation.MagicianPosition,
        simulation.MagicianHitPoints,
        simulation.VanishCooldownLeft,
        simulation.BoxOfficeHitPoints,
        simulation.ActTicksLeft,
        string.Join(',', simulation.Critics.Select(c => (c.Id, c.Position, c.HitPoints, c.IsStunned))),
        string.Join(',', simulation.ThrownCards.Select(c => c.Position)),
        string.Join(',', simulation.Clouds.Select(c => (c.Position, c.TicksLeft))),
        string.Join(',', simulation.Understudies.Select(u => (u.Position, u.IsOnStage))));
}
