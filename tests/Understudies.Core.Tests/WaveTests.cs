using System.Numerics;

namespace Understudies.Core.Tests;

public class WaveTests
{
    /// <summary>Seeds enough for a draw that could go wrong to go wrong in one of them.</summary>
    private static readonly ulong[] Seeds = [.. Enumerable.Range(1, 20).Select(seed => (ulong)seed)];

    private Tuning Tuning { get; } = CommittedTuning.Parse();

    /// <summary>
    /// Two kinds that do not divide a budget evenly: one for 3 from the first act, bought twice as readily, and one
    /// for 5 from the second act. The first act has 20 to spend and every act 7 more than the one before.
    /// </summary>
    private Tuning TwoKinds => Tuning with
    {
        FirstActBudget = 20,
        BudgetGrowthPerAct = 7,
        EnemyKinds =
        [
            Tuning.Critic() with { Cost = 3, Weight = 2, FromAct = 1 },
            Tuning.Critic() with { Name = "dear", Cost = 5, Weight = 1, FromAct = 2 },
        ],
    };

    /// <summary>
    /// The committed stage with acts of ten seconds, the last of them quiet, and critics left to themselves: nobody
    /// throws at them, they turn on nobody and their strikes take nothing, so every one that enters is still there.
    /// The curtain has no length, which is no curtain: these tests count their ticks from the first tick of an act.
    /// </summary>
    private Tuning ShortActs => Tuning with
    {
        CurtainTime = 0f,
        ActLength = 10f,
        ActQuietEnd = 1f,
        ThrowRange = 0f,
        CriticTurnRadius = 0f,
        CriticStrikeDamage = 0f,
    };

    [Test]
    public void Plan_AnActsPurchases_NeverExceedItsBudgetAndLeaveLessThanTheCheapestKindCosts()
    {
        foreach (ulong seed in Seeds)
        {
            IReadOnlyList<IReadOnlyList<PlannedEntry>> plan = Waves.Plan(TwoKinds, seed);

            Assert.That(plan, Has.Count.EqualTo(TwoKinds.ActsInPerformance));
            for (int act = 1; act <= plan.Count; act++)
            {
                int budget = 20 + (7 * (act - 1));
                int spent = plan[act - 1].Sum(entry => entry.Kind == 0 ? 3 : 5);

                Assert.That(spent, Is.LessThanOrEqualTo(budget), $"seed {seed}, act {act}");
                Assert.That(budget - spent, Is.LessThan(3), $"seed {seed}, act {act}");
            }
        }
    }

    [Test]
    public void Plan_AKind_IsBoughtFromItsActOnAndNotBefore()
    {
        foreach (ulong seed in Seeds)
        {
            IReadOnlyList<IReadOnlyList<PlannedEntry>> plan = Waves.Plan(TwoKinds, seed);

            Assert.That(plan[0].Select(entry => entry.Kind), Has.All.EqualTo(0), $"seed {seed}");
            Assert.That(plan.Skip(1).SelectMany(act => act).Select(entry => entry.Kind), Has.Some.EqualTo(1), $"seed {seed}");
        }
    }

    [Test]
    public void Plan_TheCommittedKinds_NoStagehandBeforeItsAct_AndFromItAMixWithCriticsTheLargerPart()
    {
        int from = Tuning.Stagehand().FromAct;
        Assert.That(from, Is.GreaterThan(1), "the first act has no stagehand");
        foreach (ulong seed in Seeds)
        {
            IReadOnlyList<IReadOnlyList<PlannedEntry>> plan = Waves.Plan(Tuning, seed);

            for (int act = 1; act <= plan.Count; act++)
            {
                int stagehands = plan[act - 1].Count(entry => entry.Kind == 1);

                Assert.That(
                    stagehands,
                    act < from ? Is.Zero : Is.InRange(1, (plan[act - 1].Count - 1) / 2),
                    $"seed {seed}, act {act}");
            }
        }
    }

    [Test]
    public void Plan_TwoKindsOfOneCostWithWeightsOfThreeToOne_AreBoughtThreeToOne()
    {
        // Both can be afforded for as long as either can, so the weights alone decide: of some ten thousand
        // purchases over the seeds three in four are of the first kind, and an even draw would make it two.
        Tuning tuning = Tuning with
        {
            EnemyKinds =
            [
                Tuning.Critic() with { Cost = 1, Weight = 3, FromAct = 1 },
                Tuning.Critic() with { Name = "rare", Cost = 1, Weight = 1, FromAct = 1 },
            ],
        };
        var bought = Seeds.SelectMany(seed => Waves.Plan(tuning, seed).SelectMany(act => act)).ToList();

        Assert.That(bought, Has.Count.GreaterThan(10_000));
        Assert.That((double)bought.Count(entry => entry.Kind == 0) / bought.Count, Is.InRange(0.72, 0.78));
    }

    [Test]
    public void Plan_AnActsEntries_AreNotEvenlySpaced_AndAnotherSeedSpacesThemAnotherWay()
    {
        // Each entry is somewhere in its own stretch of the act, by chance: the gaps between the first act's
        // twenty-five are of many lengths, and not the one length of a fixed place in every stretch.
        static int[] Ticks(ulong seed, Tuning tuning) => [.. Waves.Plan(tuning, seed)[0].Select(entry => entry.Tick)];
        int[] ticks = Ticks(1, Tuning);

        Assert.That(ticks.Zip(ticks.Skip(1), (earlier, later) => later - earlier).Distinct().Count(), Is.GreaterThan(5));
        Assert.That(Ticks(2, Tuning), Is.Not.EqualTo(ticks));
    }

    [Test]
    public void Plan_WithTwoDoorsOpen_TheDoorsDoNotTakeTurns_AndAnotherSeedOrdersThemAnotherWay()
    {
        // The third act has two doors open and forty-one critics. Each door is drawn: some critic enters by the
        // door of the one before it.
        static int[] Doors(ulong seed, Tuning tuning) => [.. Waves.Plan(tuning, seed)[2].Select(entry => entry.Door)];
        int[] doors = Doors(1, Tuning);

        Assert.That(doors.Zip(doors.Skip(1), (earlier, later) => later == earlier), Has.Some.True);
        Assert.That(Doors(2, Tuning), Is.Not.EqualTo(doors));
    }

    [Test]
    public void Plan_TheBudgetGrowsByTheSameWithEveryAct()
    {
        // The committed critic costs 1: an act has as many of them as its budget.
        Tuning tuning = Tuning with { FirstActBudget = 10, BudgetGrowthPerAct = 4, ActsInPerformance = 4 };

        IReadOnlyList<IReadOnlyList<PlannedEntry>> plan = Waves.Plan(tuning, seed: 1);

        Assert.That(plan.Select(act => act.Count), Is.EqualTo(new[] { 10, 14, 18, 22 }));
    }

    [Test]
    public void Plan_NoEntryIsByADoorThatIsShutInItsAct_AndEveryOpenDoorIsUsed()
    {
        // The committed doors open in acts 1, 3 and 6.
        foreach (ulong seed in Seeds)
        {
            IReadOnlyList<IReadOnlyList<PlannedEntry>> plan = Waves.Plan(Tuning, seed);

            for (int act = 1; act <= plan.Count; act++)
            {
                int[] open = act < 3 ? [0] : act < 6 ? [0, 1] : [0, 1, 2];
                Assert.That(
                    plan[act - 1].Select(entry => entry.Door).Distinct(), Is.EquivalentTo(open), $"seed {seed}, act {act}");
            }
        }
    }

    [Test]
    public void Plan_AnActsEntries_AreSpreadOverItAndLeaveItsLastSecondsFree()
    {
        // Seventy-five seconds, the last five of them quiet: the entries are in the first seventy, in the order of
        // their ticks, and of the first act's twenty-five there is one in every ten seconds of those.
        const int tenSeconds = 10 * Simulation.TicksPerSecond;
        foreach (ulong seed in Seeds)
        {
            IReadOnlyList<IReadOnlyList<PlannedEntry>> plan = Waves.Plan(Tuning, seed);

            foreach (IReadOnlyList<PlannedEntry> act in plan)
            {
                Assert.That(act.Select(entry => entry.Tick), Is.Ordered.And.All.InRange(0, (7 * tenSeconds) - 1));
            }

            Assert.That(
                plan[0].Select(entry => entry.Tick / tenSeconds).Distinct(),
                Is.EquivalentTo(new[] { 0, 1, 2, 3, 4, 5, 6 }),
                $"seed {seed}");
        }
    }

    [Test]
    public void Plan_TheSameSeed_IsTheSamePlan_AndAnotherSeedAnother()
    {
        static PlannedEntry[] All(IReadOnlyList<IReadOnlyList<PlannedEntry>> plan) => [.. plan.SelectMany(act => act)];

        PlannedEntry[] first = All(Waves.Plan(Tuning, seed: 7));
        Assert.That(All(Waves.Plan(Tuning, seed: 7)), Is.EqualTo(first));
        Assert.That(All(Waves.Plan(Tuning, seed: 8)), Is.Not.EqualTo(first));
    }

    [Test]
    public void Step_TheSameSeed_IsTheSamePerformance_AndAnotherSeedAnother()
    {
        // Two acts and half of a third, by a magician that stands on its mark.
        ulong Play(ulong seed)
        {
            var simulation = new Simulation(ShortActs, seed);
            for (int i = 0; i < 25 * Simulation.TicksPerSecond; i++)
            {
                simulation.Pick(0);
                simulation.GoOn();
                simulation.Step(default);
            }

            return simulation.ComputeStateHash();
        }

        ulong first = Play(seed: 7);
        Assert.That(Play(seed: 7), Is.EqualTo(first));
        Assert.That(Play(seed: 8), Is.Not.EqualTo(first));
    }

    [Test]
    public void Step_EveryEntryOfThePlanEnters_OnItsTickOfItsActAndAtItsDoor()
    {
        // Six acts: by the sixth all three doors are open.
        var simulation = new Simulation(ShortActs, seed: 3);
        for (int act = 1; act <= 6; act++)
        {
            Assert.That(simulation.Act, Is.EqualTo(act));
            Assert.That(simulation.ActEntries, Is.EqualTo(simulation.Plan[act - 1]));
            Assert.That(simulation.ActEntriesMade, Is.Zero);

            for (int tick = 0; simulation.Phase == Phase.Act; tick++)
            {
                int before = simulation.Critics.Count;

                simulation.Step(default);

                // On the tick it enters a critic stands where it entered, somewhere along its door.
                PlannedEntry[] planned = [.. simulation.ActEntries.Where(entry => entry.Tick == tick)];
                Critic[] entered = [.. simulation.Critics.Skip(before)];
                Assert.That(entered, Has.Length.EqualTo(planned.Length), $"act {act}, tick {tick}");
                for (int i = 0; i < planned.Length; i++)
                {
                    Vector2 door = ShortActs.StageDoors[planned[i].Door].Position;
                    Assert.That(
                        Vector2.Distance(entered[i].Position, door),
                        Is.LessThanOrEqualTo(ShortActs.StageDoorWidth / 2f),
                        $"act {act}, tick {tick}");
                    Assert.That(entered[i].Kind, Is.EqualTo(planned[i].Kind));
                }
            }

            Assert.That(simulation.ActEntriesMade, Is.EqualTo(simulation.ActEntries.Count));
            simulation.Pick(0);
            simulation.GoOn();
        }
    }

    [Test]
    public void GoOn_TheCriticsLeftFromAnAct_AreJoinedByTheNextActs()
    {
        var simulation = new Simulation(ShortActs, seed: 1);
        // Every committed kind costs 1, so an act lets in as many as its budget; nobody throws in these acts.
        int first = ShortActs.FirstActBudget;
        int second = first + ShortActs.BudgetGrowthPerAct;
        Play(simulation);
        Assert.That(simulation.Critics, Has.Count.EqualTo(first));
        simulation.Pick(0);
        simulation.GoOn();

        Play(simulation);

        Assert.That(simulation.Critics, Has.Count.EqualTo(first + second));
    }

    [Test]
    public void ActEntriesMade_InTheMiddleOfAnAct_SaysHowManyAreStillToCome()
    {
        // Critics on the first, the third and the ninth tick: after three ticks one is still to come.
        Simulation simulation = Shows.WithCriticsOnTicks(ShortActs, 0, 2, 8);

        for (int i = 0; i < 3; i++)
        {
            simulation.Step(default);
        }

        Assert.That(simulation.Critics, Has.Count.EqualTo(2));
        Assert.That(simulation.ActEntries.Skip(simulation.ActEntriesMade), Is.EqualTo(new[] { new PlannedEntry(8, 0, 0) }));
    }

    [Test]
    public void Step_WhileTheCurtainIsUp_NobodyEnters_AndTheEntriesAreCountedFromTheActsFirstPlayedTick()
    {
        // A curtain of one second, and critics on the first and the third tick of the act.
        Simulation simulation = Shows.WithCriticsOnTicks(ShortActs with { CurtainTime = 1f }, 0, 2);

        for (int i = 0; i < Simulation.TicksPerSecond; i++)
        {
            simulation.Step(default);
            Assert.That(simulation.Critics, Is.Empty);
        }

        simulation.Step(default);
        Assert.That(simulation.Critics, Has.Count.EqualTo(1));

        simulation.Step(default);
        Assert.That(simulation.Critics, Has.Count.EqualTo(1));

        simulation.Step(default);
        Assert.That(simulation.Critics, Has.Count.EqualTo(2));
    }

    [Test]
    public void DoorIsOpen_FollowsTheActsTheDoorsOpenIn()
    {
        // Nobody enters: acts of one tick, and the committed doors, which open in acts 1, 3 and 6.
        Simulation simulation = Shows.WithCriticsOnTicks(ShortActs with { ActLength = 1f / Simulation.TicksPerSecond });
        var open = new List<bool[]>();
        for (int act = 1; act <= 6; act++)
        {
            open.Add([simulation.DoorIsOpen(0), simulation.DoorIsOpen(1), simulation.DoorIsOpen(2)]);
            simulation.Step(default);
            simulation.Pick(0);
            simulation.GoOn();
        }

        Assert.That(open[0], Is.EqualTo(new[] { true, false, false }));
        Assert.That(open[1], Is.EqualTo(new[] { true, false, false }));
        Assert.That(open[2], Is.EqualTo(new[] { true, true, false }));
        Assert.That(open[4], Is.EqualTo(new[] { true, true, false }));
        Assert.That(open[5], Is.EqualTo(new[] { true, true, true }));
    }

    [Test]
    public void Step_APlanThatDrawsMore_DoesNotMoveWhereACriticEntersAlongItsDoor()
    {
        // One seed and two budgets: the bigger plan draws for nine critics and the smaller for five. Where along
        // the door the first five enter is drawn from another stream, and is the same in both shows.
        List<Vector2> five = PlacesOfEntry(new Simulation(ShortActs with { FirstActBudget = 5 }, seed: 7));
        List<Vector2> nine = PlacesOfEntry(new Simulation(ShortActs with { FirstActBudget = 9 }, seed: 7));

        Assert.That(five, Has.Count.EqualTo(5));
        Assert.That(nine, Has.Count.EqualTo(9));
        Assert.That(nine.Take(5), Is.EqualTo(five));
        Assert.That(five, Is.Unique);
    }

    [Test]
    public void Tuning_NewNumbersInAPerformanceUnderWay_DoNotPlanItAgain()
    {
        // Every committed kind costs 1, so an act lets in as many as its budget.
        int first = ShortActs.FirstActBudget;
        int second = first + ShortActs.BudgetGrowthPerAct;
        var simulation = new Simulation(ShortActs, seed: 1);
        simulation.Step(default);

        simulation.Tuning = ShortActs with { FirstActBudget = 1, BudgetGrowthPerAct = 0 };
        Play(simulation);
        simulation.Pick(0);
        simulation.GoOn();

        Assert.That(simulation.Plan.Select(act => act.Count).Take(2), Is.EqualTo(new[] { first, second }));
        Assert.That(simulation.Critics, Has.Count.EqualTo(first));
        Assert.That(simulation.ActEntries, Has.Count.EqualTo(second));
    }

    /// <summary>Where each critic of the first act stood on the tick it entered, in the order they entered.</summary>
    private static List<Vector2> PlacesOfEntry(Simulation simulation)
    {
        var places = new List<Vector2>();
        while (simulation.Phase == Phase.Act)
        {
            int before = simulation.Critics.Count;
            simulation.Step(default);
            places.AddRange(simulation.Critics.Skip(before).Select(critic => critic.Position));
        }

        return places;
    }

    /// <summary>Plays the act to its end with a magician that stands on its mark.</summary>
    private static void Play(Simulation simulation)
    {
        while (simulation.Phase == Phase.Act)
        {
            simulation.Step(default);
        }
    }
}
