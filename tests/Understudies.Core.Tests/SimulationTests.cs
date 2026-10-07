using System.Numerics;

namespace Understudies.Core.Tests;

public class SimulationTests
{
    private const float Tolerance = 1e-4f;

    private Tuning Tuning { get; } = CommittedTuning.Parse();

    [Test]
    public void Step_NoInput_TheMagicianStaysOnTheMark()
    {
        var simulation = new Simulation(Tuning, seed: 1);

        Walk(simulation, Vector2.Zero, ticks: 10);

        Assert.That(simulation.MagicianPosition, Is.EqualTo(Tuning.MagicianMark));
    }

    [Test]
    public void Step_OneSecondInAStraightLine_CoversTheMagiciansSpeed()
    {
        var simulation = new Simulation(Tuning, seed: 1);

        Walk(simulation, new Vector2(1f, 0f), Simulation.TicksPerSecond);

        Vector2 walked = simulation.MagicianPosition - Tuning.MagicianMark;
        Assert.That(walked.X, Is.EqualTo(Tuning.MagicianSpeed).Within(Tolerance));
        Assert.That(walked.Y, Is.Zero);
    }

    [Test]
    public void Step_ADiagonal_IsNoFasterThanAStraightLine()
    {
        var straight = new Simulation(Tuning, seed: 1);
        var diagonal = new Simulation(Tuning, seed: 1);

        Walk(straight, new Vector2(1f, 0f), ticks: 30);
        Walk(diagonal, new Vector2(1f, 1f), ticks: 30);

        Assert.That(
            Vector2.Distance(diagonal.MagicianPosition, Tuning.MagicianMark),
            Is.EqualTo(Vector2.Distance(straight.MagicianPosition, Tuning.MagicianMark)).Within(Tolerance));
    }

    [Test]
    public void Step_AStraightInputLongerThanOne_IsNoFasterThanFullSpeed()
    {
        // The stick pushed right while D is held: the view sums them.
        var simulation = new Simulation(Tuning, seed: 1);

        Walk(simulation, new Vector2(2f, 0f), Simulation.TicksPerSecond);

        Vector2 walked = simulation.MagicianPosition - Tuning.MagicianMark;
        Assert.That(walked.X, Is.EqualTo(Tuning.MagicianSpeed).Within(Tolerance));
    }

    [TestCase(0.5f)]
    [TestCase(0.8f)]
    public void Step_AStickPushedPartWay_MovesAtThatShareOfTheSpeed(float share)
    {
        var simulation = new Simulation(Tuning, seed: 1);

        Walk(simulation, new Vector2(0f, share), Simulation.TicksPerSecond);

        Vector2 walked = simulation.MagicianPosition - Tuning.MagicianMark;
        Assert.That(walked.Y, Is.EqualTo(Tuning.MagicianSpeed * share).Within(Tolerance));
    }

    [Test]
    public void Step_IntoTheTopLeftCorner_TheMagiciansWholeCircleStaysOnTheStage()
    {
        var simulation = new Simulation(Tuning, seed: 1);

        Walk(simulation, new Vector2(-1f, -1f), ticks: 10 * Simulation.TicksPerSecond);

        Assert.That(simulation.MagicianPosition, Is.EqualTo(new Vector2(Tuning.MagicianRadius)));
    }

    [Test]
    public void Step_IntoTheBottomRightCorner_TheMagiciansWholeCircleStaysOnTheStage()
    {
        var simulation = new Simulation(Tuning, seed: 1);

        Walk(simulation, new Vector2(1f, 1f), ticks: 10 * Simulation.TicksPerSecond);

        Assert.That(simulation.MagicianPosition, Is.EqualTo(Tuning.StageSize - new Vector2(Tuning.MagicianRadius)));
    }

    [Test]
    public void Step_KeepsThePositionBeforeTheTickForTheView()
    {
        var simulation = new Simulation(Tuning, seed: 1);
        Walk(simulation, new Vector2(1f, 0f), ticks: 3);
        Vector2 before = simulation.MagicianPosition;

        simulation.Step(new MagicianInput(new Vector2(1f, 0f)));

        Assert.That(simulation.MagicianPreviousPosition, Is.EqualTo(before));
        Assert.That(simulation.MagicianPosition, Is.Not.EqualTo(before));
    }

    [Test]
    public void Step_AfterNewTuning_WalksOnFromWhereItStoodByTheNewNumbers()
    {
        var simulation = new Simulation(Tuning, seed: 1);
        var right = new MagicianInput(new Vector2(1f, 0f));
        simulation.Step(right);
        Vector2 stood = simulation.MagicianPosition;
        float oneTick = stood.X - Tuning.MagicianMark.X;

        simulation.Tuning = Tuning with { MagicianSpeed = Tuning.MagicianSpeed * 2f };
        simulation.Step(right);

        Assert.That(simulation.MagicianPreviousPosition, Is.EqualTo(stood));
        Assert.That(simulation.MagicianPosition.X - stood.X, Is.EqualTo(2f * oneTick).Within(Tolerance));
    }

    private static void Walk(Simulation simulation, Vector2 move, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(new MagicianInput(move));
        }
    }
}
