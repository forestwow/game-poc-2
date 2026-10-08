using System.Numerics;

namespace Understudies.Core.Tests;

public class CriticTests
{
    private const float Tolerance = 1e-3f;

    /// <summary>
    /// The committed numbers with a magician that reaches nobody and that no critic turns on: these are the critics
    /// left to themselves.
    /// The curtain has no length, which is no curtain: these tests count their ticks from the first
    /// tick of an act, and the curtain has tests of its own.
    /// </summary>
    private Tuning Tuning { get; } = CommittedTuning.Parse() with
    {
        CurtainTime = 0f,
        ThrowRange = 0f,
        CriticTurnRadius = 0f,
    };

    [Test]
    public void Step_TheFirstTick_ACriticEntersAtTheFirstDoor()
    {
        var simulation = Shows.WithOneCritic(Tuning);

        simulation.Step(default);

        // The committed first door is on the left edge, so it runs up and down.
        Vector2 door = Tuning.StageDoors[0].Position;
        Assert.That(simulation.Critics, Has.Count.EqualTo(1));
        Assert.That(simulation.Critics[0].Position.X, Is.EqualTo(door.X));
        Assert.That(simulation.Critics[0].Position.Y, Is.EqualTo(door.Y).Within(Tuning.StageDoorWidth / 2f));
        Assert.That(simulation.Critics[0].PreviousPosition, Is.EqualTo(simulation.Critics[0].Position));
    }

    [Test]
    public void Step_AFirstDoorInTheBackWall_TheCriticEntersOnTheFloorSomewhereAlongTheWall()
    {
        // The floor starts three units down the stage and the door is at the foot of the wall there: it runs along
        // the wall, so the critic enters on the floor's top edge and no higher.
        var door = new Vector2(20f, 3f);
        Tuning tuning = Tuning with { StageFloorTop = 3f, StageDoors = [new StageDoor(door, 1)] };
        var simulation = Shows.WithOneCritic(tuning);

        simulation.Step(default);

        Assert.That(simulation.Critics[0].Position.Y, Is.EqualTo(3f));
        Assert.That(simulation.Critics[0].Position.X, Is.EqualTo(door.X).Within(tuning.StageDoorWidth / 2f));
        Assert.That(simulation.Critics[0].Position.X, Is.Not.EqualTo(door.X));
    }

    [Test]
    public void Step_AFirstDoorInTheRightEdge_TheCriticEntersSomewhereAlongIt()
    {
        var door = new Vector2(Tuning.StageSize.X, 9f);
        Tuning tuning = Tuning with { StageDoors = [new StageDoor(door, 1)] };
        var simulation = Shows.WithOneCritic(tuning);

        simulation.Step(default);

        Assert.That(simulation.Critics[0].Position.X, Is.EqualTo(door.X));
        Assert.That(simulation.Critics[0].Position.Y, Is.EqualTo(door.Y).Within(tuning.StageDoorWidth / 2f));
        Assert.That(simulation.Critics[0].Position.Y, Is.Not.EqualTo(door.Y));
    }

    [Test]
    public void Step_AnotherSeed_TheCriticEntersAtAnotherPointOfTheDoor()
    {
        Simulation one = Shows.WithACriticEvery(Simulation.TicksPerSecond, Tuning, seed: 1);
        Simulation other = Shows.WithACriticEvery(Simulation.TicksPerSecond, Tuning, seed: 2);

        one.Step(default);
        other.Step(default);

        Assert.That(other.Critics[0].Position, Is.Not.EqualTo(one.Critics[0].Position));
    }

    [Test]
    public void Step_TheSameSeed_GivesTheSameCriticsAndTheSameBoxOffice()
    {
        var one = new Simulation(Tuning, seed: 7UL);
        var other = new Simulation(Tuning, seed: 7UL);

        Run(one, ticks: 40 * Simulation.TicksPerSecond);
        Run(other, ticks: 40 * Simulation.TicksPerSecond);

        Assert.That(
            other.Critics.Select(critic => (critic.Id, critic.Position)),
            Is.EqualTo(one.Critics.Select(critic => (critic.Id, critic.Position))));
        Assert.That(other.BoxOfficeHitPoints, Is.EqualTo(one.BoxOfficeHitPoints));
        Assert.That(one.BoxOfficeHitPoints, Is.LessThan(Tuning.BoxOfficeHitPoints));
    }

    [Test]
    public void Step_CriticsEnterOneAfterAnother_EachWithItsOwnId()
    {
        // Critics enter on the first tick, then on the first tick after every two seconds.
        const int twoSeconds = 2 * Simulation.TicksPerSecond;
        Simulation simulation = Shows.WithACriticEvery(twoSeconds, Tuning);

        Run(simulation, ticks: twoSeconds);
        Assert.That(simulation.Critics, Has.Count.EqualTo(1));
        Critic first = simulation.Critics[0];

        Run(simulation, ticks: 1);
        Assert.That(simulation.Critics, Has.Count.EqualTo(2));

        Run(simulation, ticks: twoSeconds);
        Assert.That(simulation.Critics, Has.Count.EqualTo(3));
        Assert.That(simulation.Critics[0], Is.SameAs(first));
        Assert.That(simulation.Critics.Select(critic => critic.Id), Is.Unique);
    }

    [Test]
    public void Step_ATimeBetweenTwoTicks_CountsTheNearerNumberOfTicks()
    {
        // 1.6 ticks are two ticks, not one: so long an act is over after its second tick, and not after its first.
        Simulation simulation = Shows.WithOneCritic(Tuning with { ActLength = 1.6f / Simulation.TicksPerSecond });
        Assert.That(simulation.ActTicksLeft, Is.EqualTo(2));

        Run(simulation, ticks: 1);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));

        Run(simulation, ticks: 1);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Program));
    }

    [Test]
    public void Step_OneSecond_ACriticWalksItsSpeedStraightAtTheBoxOffice()
    {
        var simulation = Shows.WithOneCritic(Tuning);
        simulation.Step(default);
        Vector2 entered = simulation.Critics[0].Position;

        Run(simulation, Simulation.TicksPerSecond);

        Vector2 walked = simulation.Critics[0].Position - entered;
        Vector2 toBoxOffice = Vector2.Normalize(Tuning.BoxOfficePosition - entered);
        Assert.That(walked.Length(), Is.EqualTo(Tuning.Critic().Speed).Within(Tolerance));
        Assert.That(Vector2.Distance(Vector2.Normalize(walked), toBoxOffice), Is.Zero.Within(Tolerance));
    }

    [Test]
    public void Step_KeepsACriticsPositionBeforeTheTickForTheView()
    {
        var simulation = Shows.WithOneCritic(Tuning);
        Run(simulation, ticks: 3);
        Vector2 before = simulation.Critics[0].Position;

        simulation.Step(default);

        Assert.That(simulation.Critics[0].PreviousPosition, Is.EqualTo(before));
        Assert.That(simulation.Critics[0].Position, Is.Not.EqualTo(before));
    }

    [Test]
    public void Step_ACriticStopsWhereItsCircleTouchesTheBoxOffices()
    {
        var simulation = Shows.WithOneCritic(Tuning);

        Run(simulation, ticks: 20 * Simulation.TicksPerSecond);

        Critic critic = simulation.Critics[0];
        Assert.That(
            Vector2.Distance(critic.Position, Tuning.BoxOfficePosition),
            Is.EqualTo((Tuning.BoxOfficeSize / 2f) + Tuning.Critic().Radius).Within(Tolerance));
        Assert.That(Vector2.Distance(critic.PreviousPosition, critic.Position), Is.Zero.Within(Tolerance));
    }

    [Test]
    public void Step_ACriticAtTheBoxOffice_StrikesItOncePerCooldown()
    {
        const int cooldown = Simulation.TicksPerSecond / 2;
        Tuning tuning = Tuning with
        {
            BoxOfficeHitPoints = 100f, CriticStrikeDamage = 3f, CriticBlowCooldown = 0.5f,
        };
        var simulation = Shows.WithOneCritic(tuning);
        Assert.That(simulation.BoxOfficeHitPoints, Is.EqualTo(100f));

        // The first strike lands on the tick the critic arrives, and not before.
        RunUntil(simulation, () => simulation.BoxOfficeHitPoints < 100f);
        Assert.That(simulation.BoxOfficeHitPoints, Is.EqualTo(97f));
        Assert.That(
            Vector2.Distance(simulation.Critics[0].Position, tuning.BoxOfficePosition),
            Is.EqualTo((tuning.BoxOfficeSize / 2f) + tuning.Critic().Radius).Within(Tolerance));

        Run(simulation, ticks: cooldown - 1);
        Assert.That(simulation.BoxOfficeHitPoints, Is.EqualTo(97f));

        Run(simulation, ticks: 1);
        Assert.That(simulation.BoxOfficeHitPoints, Is.EqualTo(94f));

        Run(simulation, ticks: cooldown);
        Assert.That(simulation.BoxOfficeHitPoints, Is.EqualTo(91f));
    }

    [Test]
    public void Step_ACriticStrikesTheBoxOffice_ItIsReportedWhereTheCriticStandsForThatTickOnly()
    {
        var simulation = Shows.WithOneCritic(Tuning);

        // The walk to the box office reports nothing.
        while (simulation.BoxOfficeHitPoints == Tuning.BoxOfficeHitPoints)
        {
            Assert.That(simulation.Events, Is.Empty);
            simulation.Step(default);
        }

        Assert.That(
            simulation.Events,
            Is.EqualTo(new[] { new TickEvent(TickEventKind.BoxOfficeStruck, simulation.Critics[0].Position) }));

        simulation.Step(default);
        Assert.That(simulation.Events, Is.Empty);
    }

    [Test]
    public void Step_TheBoxOfficesHitPointsRunOut_TheShowCloses()
    {
        // Two strikes are not enough and the third is more than enough.
        Tuning tuning = Tuning with { BoxOfficeHitPoints = 5f, CriticStrikeDamage = 2f };
        var simulation = Shows.WithOneCritic(tuning);

        RunUntil(simulation, () => simulation.BoxOfficeHitPoints <= 1f);
        Assert.That(simulation.BoxOfficeHitPoints, Is.EqualTo(1f));
        Assert.That(simulation.ShowClosed, Is.False);

        RunUntil(simulation, () => simulation.BoxOfficeHitPoints < 1f);
        Assert.That(simulation.BoxOfficeHitPoints, Is.Zero);
        Assert.That(simulation.ShowClosed, Is.True);

        // It is the box office that fell, and not the magician.
        Assert.That(simulation.MagicianHasFallen, Is.False);
    }

    [Test]
    public void Step_AfterTheShowCloses_ChangesNothing()
    {
        Simulation simulation = Shows.WithACriticEvery(3 * Simulation.TicksPerSecond, Tuning with { BoxOfficeHitPoints = 5f });
        var right = new MagicianInput(new Vector2(1f, 0f));
        RunUntil(simulation, () => simulation.ShowClosed);
        Vector2 magician = simulation.MagicianPosition;
        Vector2 magicianBefore = simulation.MagicianPreviousPosition;
        var critics = simulation.Critics.Select(critic => (critic.Id, critic.Position, critic.PreviousPosition)).ToList();

        for (int i = 0; i < 10 * Simulation.TicksPerSecond; i++)
        {
            simulation.Step(right);
        }

        Assert.That(simulation.ShowClosed, Is.True);
        Assert.That(simulation.BoxOfficeHitPoints, Is.Zero);
        Assert.That(simulation.MagicianPosition, Is.EqualTo(magician));
        Assert.That(simulation.MagicianPreviousPosition, Is.EqualTo(magicianBefore));
        Assert.That(
            simulation.Critics.Select(critic => (critic.Id, critic.Position, critic.PreviousPosition)),
            Is.EqualTo(critics));
    }

    [Test]
    public void Step_AfterTheShowCloses_LeavesNoEventsBehind()
    {
        // The first strike is more than enough: the tick that closes the show reports it.
        Tuning tuning = Tuning with { BoxOfficeHitPoints = 1f, CriticStrikeDamage = 2f };
        var simulation = Shows.WithOneCritic(tuning);
        RunUntil(simulation, () => simulation.ShowClosed);
        Assert.That(simulation.Events.Select(e => e.Kind), Is.EqualTo(new[] { TickEventKind.BoxOfficeStruck }));

        // And the show that has closed reports it no more: whoever is driven by the events does it once.
        simulation.Step(default);
        Assert.That(simulation.Events, Is.Empty);
    }

    [Test]
    public void Step_TwoCriticsOnExactlyOnePoint_PartByTheNextTick()
    {
        // A door with no width lets every critic in at one point, and a critic with no speed stays there.
        Tuning tuning = Tuning.WithCritic(critic => critic with { Speed = 0f }) with { StageDoorWidth = 0f };
        Simulation simulation = Shows.WithCriticsOnTicks(tuning, 0, 1);
        Run(simulation, ticks: 2);
        Vector2 point = simulation.Critics[0].Position;
        Assert.That(simulation.Critics[1].Position, Is.EqualTo(point));

        simulation.Step(default);

        // Half the overlap each: the earlier one to the left, the later one to the right, until they only touch.
        Assert.That(simulation.Critics[0].Position.X, Is.EqualTo(point.X - tuning.Critic().Radius).Within(Tolerance));
        Assert.That(simulation.Critics[1].Position.X, Is.EqualTo(point.X + tuning.Critic().Radius).Within(Tolerance));
        Assert.That(simulation.Critics[0].Position.Y, Is.EqualTo(point.Y));
        Assert.That(simulation.Critics[1].Position.Y, Is.EqualTo(point.Y));
    }

    [Test]
    public void Step_ACrowdAtTheBoxOffice_NoCriticStandsOnAnother()
    {
        Simulation simulation = ACrowd();

        // Pushed softly, critics in a crowd overlap; standing on one another is a centre inside another's circle.
        var positions = simulation.Critics.Select(critic => critic.Position).ToList();
        for (int i = 0; i < positions.Count; i++)
        {
            for (int j = i + 1; j < positions.Count; j++)
            {
                Assert.That(Vector2.Distance(positions[i], positions[j]), Is.GreaterThan(Tuning.Critic().Radius));
            }
        }
    }

    [Test]
    public void Step_ACrowdAtTheBoxOffice_PushesNoCriticIntoIt()
    {
        Simulation simulation = ACrowd();

        float touching = (Tuning.BoxOfficeSize / 2f) + Tuning.Critic().Radius;
        Assert.That(
            simulation.Critics.Select(critic => Vector2.Distance(critic.Position, Tuning.BoxOfficePosition)),
            Has.All.GreaterThan(touching - Tolerance));
    }

    /// <summary>
    /// Forty seconds of the committed numbers with a critic entering every second and strikes that take nothing, so
    /// the show goes on: forty critics, most of them at the box office.
    /// </summary>
    private Simulation ACrowd()
    {
        Simulation simulation = Shows.WithACriticEvery(Simulation.TicksPerSecond, Tuning with { CriticStrikeDamage = 0f });
        Run(simulation, ticks: 40 * Simulation.TicksPerSecond);
        return simulation;
    }

    private static void RunUntil(Simulation simulation, Func<bool> reached)
    {
        for (int i = 0; !reached(); i++)
        {
            Assert.That(i, Is.LessThan(10 * 60 * Simulation.TicksPerSecond), "Ten minutes went by and it never came.");
            simulation.Step(default);
        }
    }

    private static void Run(Simulation simulation, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(default);
        }
    }
}
