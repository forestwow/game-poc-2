using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Understudies.Core.Tests;

public class ScriptedPlayersTests
{
    private Tuning Tuning { get; } = CommittedTuning.Parse();

    /// <summary>
    /// The committed numbers on another budget, with a first act four times as full and acts that grow by less: the tests that play it
    /// assert what they play it for, a player that is crowded and vanishes and a box office that is struck.
    /// </summary>
    private Tuning Crowded => Tuning with { FirstActBudget = 60, BudgetGrowthPerAct = 40 };

    /// <summary>
    /// Plan decision 9's question, whether <c>float</c> gives one result on two machines: two scripted
    /// performances, each pinned at the end of its third act and at its end, here (macOS ARM) and in CI (Linux
    /// x64). The earlier pin says how early a disagreement starts. The first is the doors player on the committed
    /// numbers, asserted to have an encore in it; the second, the orbit player with a fuller first act, is
    /// asserted to have the Vanish and its cloud, stunned critics and blows on the box office in it. A change to tuning.json, to a
    /// rule or to a player changes them: pin them again from the failure's message, and say so in the pull
    /// request. If the two machines ever disagree, that is a finding for the owner and not a test to make pass.
    /// </summary>
    [Test]
    public void Play_TheDoorsPlayerOnSeedOne_EndsInThePinnedStateHashOnEveryMachine()
    {
        Performance performance = ScriptedPlayers.Play(Tuning, seed: 1, ScriptedPlayers.Doors);

        Assert.Multiple(() =>
        {
            // An act that stood for an encore is in what is pinned.
            Assert.That(performance.Acts.Sum(act => act.Encores), Is.GreaterThan(0), "encores");
            Assert.That(performance.Acts[2].StateHash, Is.EqualTo(4534225376748274154UL), "the end of act three");
            Assert.That(performance.Acts[^1].StateHash, Is.EqualTo(9503559715981885832UL), "the end of the performance");
        });
    }

    /// <inheritdoc cref="Play_TheDoorsPlayerOnSeedOne_EndsInThePinnedStateHashOnEveryMachine"/>
    [Test]
    public void Play_TheOrbitPlayerOnSeedOneWithAFullerFirstAct_EndsInThePinnedStateHashOnEveryMachine()
    {
        int vanishes = 0;
        int blows = 0;
        int stunned = 0;
        Func<Simulation, MagicianInput> orbit = ScriptedPlayers.Orbit(5f);

        Performance performance = ScriptedPlayers.Play(Crowded, seed: 1, simulation =>
        {
            vanishes += simulation.Events.Count(happened => happened.Kind == TickEventKind.Vanish);
            blows += simulation.Events.Count(happened => happened.Kind == TickEventKind.BoxOfficeStruck);
            stunned += simulation.Critics.Count(critic => critic.IsStunned);
            return orbit(simulation);
        });

        Assert.Multiple(() =>
        {
            // What this performance is pinned for is in it.
            Assert.That(vanishes, Is.GreaterThan(0), "Vanishes");
            Assert.That(blows, Is.GreaterThan(0), "blows on the box office");
            Assert.That(stunned, Is.GreaterThan(0), "stunned critics");
            Assert.That(performance.Acts[2].StateHash, Is.EqualTo(18205824393891740368UL), "the end of act three");
            Assert.That(performance.Acts[^1].StateHash, Is.EqualTo(8602346803663158055UL), "the end of the performance");
        });
    }

    [Test]
    public void Play_TheSameSeedAndPlayerTwice_GiveTheSameRecord()
    {
        Performance one = ScriptedPlayers.Play(Tuning, seed: 3, ScriptedPlayers.Orbit(5f));
        Performance other = ScriptedPlayers.Play(Tuning, seed: 3, ScriptedPlayers.Orbit(5f));

        Assert.Multiple(() =>
        {
            Assert.That(other.Ended, Is.EqualTo(one.Ended));
            Assert.That(other.Acts, Is.EqualTo(one.Acts));
            Assert.That(other.Encores, Is.EqualTo(one.Encores));
        });
    }

    [Test]
    public void Play_TheRecordOfEveryAct_IsWhatThePlanTheEventsAndTheSimulationSay()
    {
        // The plan is drawn apart from the performance. The box office is read off the simulation as the next
        // act's curtain rises, where nothing has changed since the act was over, and its blows are counted
        // from the events.
        IReadOnlyList<IReadOnlyList<PlannedEntry>> plan = Waves.Plan(Crowded, seed: 2);
        var boxOfficeAtTheCurtain = new List<float>();
        Func<Simulation, MagicianInput> orbit = ScriptedPlayers.Orbit(5f);
        Performance performance = ScriptedPlayers.Play(Crowded, seed: 2, simulation =>
        {
            if (simulation.Act > boxOfficeAtTheCurtain.Count)
            {
                boxOfficeAtTheCurtain.Add(simulation.BoxOfficeHitPoints);
            }

            return orbit(simulation);
        });

        Assert.That(performance.Acts.Sum(act => act.Encores), Is.GreaterThan(0), "the performance has encores to count");
        Assert.That(performance.Acts[^1].BoxOffice, Is.LessThan(Crowded.BoxOfficeHitPoints), "and blows on the box office");
        int encoresBefore = 0;
        for (int i = 0; i < performance.Acts.Count; i++)
        {
            ActRecord act = performance.Acts[i];
            IReadOnlyList<Card> taken = performance.Encores[i];

            // Decision 26: every encore costs what the first does and more for each taken before it, in the
            // performance; an act pays with its own applause.
            int spent = Enumerable.Range(encoresBefore, taken.Count)
                .Sum(before => Crowded.EncoreFirstCost + (Crowded.EncoreCostGrowth * before));
            encoresBefore += taken.Count;
            bool last = i == performance.Acts.Count - 1;
            Assert.Multiple(() =>
            {
                Assert.That(
                    act.Entries,
                    !last || performance.Ended == Phase.Ovation ? Is.EqualTo(plan[i].Count) : Is.LessThanOrEqualTo(plan[i].Count),
                    $"act {i + 1}: entries");
                Assert.That(act.FellInReach + act.WalkedTo, Is.EqualTo(act.Applause), $"act {i + 1}: applause");
                Assert.That(act.Dropped, Is.GreaterThanOrEqualTo(act.Applause), $"act {i + 1}: dropped");
                Assert.That(act.Encores, Is.EqualTo(taken.Count), $"act {i + 1}: encores");
                Assert.That(spent, Is.LessThanOrEqualTo(act.Applause), $"act {i + 1}: what its encores cost");
                Assert.That(taken, Has.None.EqualTo(Card.ChorusDamage), $"act {i + 1}: an encore's cards");
                if (!last)
                {
                    Assert.That(act.BoxOffice, Is.EqualTo(boxOfficeAtTheCurtain[i + 1]), $"act {i + 1}: box office");
                }
            });
        }
    }

    [Test]
    public void Choose_TakesTheCardThatComesFirstInTheOrder_WhereverItIsOffered()
    {
        Assert.Multiple(() =>
        {
            // The order itself: another order is another instrument, and the guard's table is read anew.
            Assert.That(ScriptedPlayers.CardOrder, Is.EqualTo(new[]
            {
                Card.OneMoreCard, Card.ChorusDamage, Card.Damage, Card.AttackSpeed, Card.Range, Card.VanishCooldown,
            }));
            Assert.That(ScriptedPlayers.Choose([Card.Range, Card.OneMoreCard, Card.Damage]), Is.EqualTo(1));
            Assert.That(ScriptedPlayers.Choose([Card.VanishCooldown, Card.Range, Card.ChorusDamage]), Is.EqualTo(2));
            Assert.That(ScriptedPlayers.Choose([Card.AttackSpeed, Card.Damage]), Is.EqualTo(1));
            Assert.That(ScriptedPlayers.Choose([Card.VanishCooldown, Card.Range]), Is.EqualTo(1));
            Assert.That(ScriptedPlayers.Choose([Card.VanishCooldown]), Is.EqualTo(0));
        });
    }

    [TestCase(0.5f)]
    [TestCase(3f)]
    [TestCase(5f)]
    public void Orbit_GoesRoundTheBoxOffice_AndNeverLeavesItsCircle(float radius)
    {
        Vector2 centre = Tuning.BoxOfficePosition;
        Func<Simulation, MagicianInput> orbit = ScriptedPlayers.Orbit(radius);
        float furthest = 0f;
        var least = new Vector2(float.PositiveInfinity);
        var most = new Vector2(float.NegativeInfinity);
        int vanishes = 0;

        // The magician's mark is outside the smaller circles: it is watched from the moment it is in its circle.
        bool inside = false;
        ScriptedPlayers.Play(Crowded, seed: 1, simulation =>
        {
            float fromCentre = Vector2.Distance(simulation.MagicianPosition, centre);
            inside = simulation.Phase == Phase.Curtain ? false : inside || fromCentre <= radius;
            if (inside)
            {
                vanishes += simulation.Events.Count(happened => happened.Kind == TickEventKind.Vanish);
                furthest = MathF.Max(furthest, fromCentre);
                least = Vector2.Min(least, simulation.MagicianPosition);
                most = Vector2.Max(most, simulation.MagicianPosition);
            }

            return orbit(simulation);
        });

        Assert.Multiple(() =>
        {
            // A Vanish is as long as the circle of 3 is across, and longer than the smallest: only on the
            // widest is there room for one.
            if (radius >= 5f)
            {
                Assert.That(vanishes, Is.GreaterThan(0), "the performance has a Vanish in it");
            }

            // On the circle the float's last digit may fall either side of it.
            Assert.That(furthest, Is.LessThanOrEqualTo(radius + 0.001f));

            // All the way round: as far left, right, up and down as the circle goes.
            Assert.That(most.X - least.X, Is.EqualTo(2f * radius).Within(0.1f));
            Assert.That(most.Y - least.Y, Is.EqualTo(2f * radius).Within(0.1f));
        });
    }

    [Test]
    public void Doors_InEveryAct_GoesToThePostOfTheDoorThatOpenedLast()
    {
        // The tuning's own doors, whichever acts they open in: a throw's range in from the newest, towards the
        // box office.
        Vector2 Post(int act)
        {
            StageDoor newest = Tuning.StageDoors.Where(door => door.OpensInAct <= act).MaxBy(door => door.OpensInAct);
            return newest.Position
                + (Vector2.Normalize(Tuning.BoxOfficePosition - newest.Position) * Tuning.ThrowRange);
        }

        float step = Tuning.MagicianSpeed / Simulation.TicksPerSecond;
        var actsOnThePost = new List<int>();

        ScriptedPlayers.Play(Tuning, seed: 1, simulation =>
        {
            if (!actsOnThePost.Contains(simulation.Act)
                && Vector2.Distance(simulation.MagicianPosition, Post(simulation.Act)) <= step)
            {
                actsOnThePost.Add(simulation.Act);
            }

            return ScriptedPlayers.Doors(simulation);
        });

        Assert.Multiple(() =>
        {
            Assert.That(actsOnThePost, Is.EqualTo(Enumerable.Range(1, Tuning.ActsInPerformance)));
            Assert.That(
                Enumerable.Range(1, Tuning.ActsInPerformance).Select(Post).Distinct().Count(),
                Is.GreaterThan(1),
                "the tuning has more than one door to hold");
        });
    }

    [Test]
    public void Doors_StepsBackFromACriticThatHasTurnedOnIt_AndIsNotTouched()
    {
        // One critic that no card fells in the time: it walks in by the door the player holds, and turns on it.
        Tuning tuning = Tuning.WithCritic(critic => critic with { HitPoints = 1000f }) with { CurtainTime = 0f };
        Simulation simulation = Shows.WithOneCritic(tuning);
        float nearest = float.PositiveInfinity;

        for (int tick = 0; tick < 6 * Simulation.TicksPerSecond; tick++)
        {
            simulation.Step(ScriptedPlayers.Doors(simulation));
            nearest = MathF.Min(nearest, Vector2.Distance(simulation.Critics[0].Position, simulation.MagicianPosition));
        }

        Assert.Multiple(() =>
        {
            // The two met, and the magician kept its distance: it still has the critic on it, in its throw.
            Assert.That(nearest, Is.LessThan(tuning.CriticTurnRadius));
            Assert.That(simulation.MagicianHitPoints, Is.EqualTo(tuning.MagicianHitPoints));
            Assert.That(
                Vector2.Distance(simulation.Critics[0].Position, simulation.MagicianPosition),
                Is.LessThan(tuning.CriticTurnRadius));
        });
    }

    [Test]
    public void Doors_GoesBackToTheBoxOfficeWhileACriticIsAtIt_AndToItsDoorWhenNoneIs()
    {
        // One stagehand, which does not turn, with more hit points than the player takes off it on its way
        // past: it gets to the box office and strikes it.
        Tuning tuning = Tuning with
        {
            CurtainTime = 0f,
            EnemyKinds = [Tuning.Critic(), Tuning.Stagehand() with { HitPoints = 40f }],
        };
        Simulation simulation = Shows.WithOneOfKind(tuning, kind: 1);
        Vector2 post = ScriptedPlayers.DoorsPost(simulation);
        float step = tuning.MagicianSpeed / Simulation.TicksPerSecond;
        bool stoodByTheBoxOffice = false;

        for (int tick = 0; tick < 60 * Simulation.TicksPerSecond; tick++)
        {
            simulation.Step(ScriptedPlayers.Doors(simulation));
            float fromBoxOffice = Vector2.Distance(simulation.MagicianPosition, tuning.BoxOfficePosition);
            stoodByTheBoxOffice |= simulation.Critics.Count > 0
                && MathF.Abs(fromBoxOffice - ScriptedPlayers.DoorsDefenceDistance) <= step;
        }

        Assert.Multiple(() =>
        {
            Assert.That(simulation.BoxOfficeHitPoints, Is.LessThan(tuning.BoxOfficeHitPoints), "the stagehand struck");
            Assert.That(stoodByTheBoxOffice, "the player came back to it");
            Assert.That(simulation.Critics, Is.Empty, "and felled it from there");
            Assert.That(Vector2.Distance(simulation.MagicianPosition, post), Is.LessThanOrEqualTo(step), "and went back to its door");
        });
    }

    /// <summary>
    /// Plan decision 22, the guard the committed numbers are tuned to (plan T20): over the seeds 1 to 20 the
    /// orbit player loses the box office by the end of act six in at least 16, and the doors player finishes act
    /// ten in at least 16. Hiding is whichever of the orbit's circles does best, so a seed counts for the orbit
    /// only when every circle lost on it. A change to tuning.json, to a rule or to a player that breaks this has
    /// made hiding pay or the doors lose: <see cref="PrintTheGuardsTable"/> says where.
    /// </summary>
    [Test]
    public void TheGuard_OnTheCommittedTuning_TheOrbitLosesByActSixAndTheDoorsPlayerFinishesActTen()
    {
        const int AtLeast = 16;
        Performance[] performances = PlayTheGuard(Tuning);
        int orbits = GuardPlayers.Length - 1;
        Performance Of(int player, int seed) => performances[(player * Seeds) + seed - 1];

        Assert.Multiple(() =>
        {
            Assert.That(
                Enumerable.Range(1, Seeds).Count(seed => Enumerable.Range(0, orbits).All(orbit => LostByActSix(Of(orbit, seed)))),
                Is.GreaterThanOrEqualTo(AtLeast),
                "seeds on which the orbit player lost the box office by the end of act six on every circle");
            Assert.That(
                Enumerable.Range(1, Seeds).Count(seed => Of(orbits, seed).Ended == Phase.Ovation),
                Is.GreaterThanOrEqualTo(AtLeast),
                "seeds on which the doors player finished act ten");
        });
    }

    /// <summary>
    /// The instrument, read out: the orbit player on each of its circles and the doors player over the seeds 1
    /// to 20 on the committed numbers, act by act, and decision 22's two counts at the foot.
    /// </summary>
    [Test]
    [Explicit("Prints how the orbit player and the doors player end over 20 seeds on the committed tuning, and the guard's two counts (a few seconds)")]
    public void PrintTheGuardsTable() => PrintTheTable(Tuning);

    /// <summary>
    /// The same table on another budget and nothing else changed, for whoever tunes to the guard.
    /// </summary>
    [TestCase(45, 40)]
    [TestCase(60, 60)]
    [TestCase(40, 80)]
    [Explicit("Prints the guard's table on the committed tuning with another budget: the first act's, and what every act has more than the one before")]
    public void PrintTheTableOnAnotherBudget(int firstActBudget, int budgetGrowthPerAct) =>
        PrintTheTable(Tuning with { FirstActBudget = firstActBudget, BudgetGrowthPerAct = budgetGrowthPerAct });

    /// <summary>The seeds the guard is read over: 1 to this.</summary>
    private const int Seeds = 20;

    /// <summary>Who plays the guard: the orbit on each of its circles, and the doors player last.</summary>
    private static readonly (string Name, Func<Simulation, MagicianInput> Player)[] GuardPlayers =
    [
        .. ScriptedPlayers.OrbitRadii.Select(radius => ($"orbit {Number(radius)}", ScriptedPlayers.Orbit(radius))),
        ("doors", ScriptedPlayers.Doors),
    ];

    /// <summary>
    /// Every one of <see cref="GuardPlayers"/> over the seeds 1 to <see cref="Seeds"/>: a player's performances
    /// side by side, a seed after a seed.
    /// </summary>
    private static Performance[] PlayTheGuard(Tuning tuning)
    {
        var performances = new Performance[GuardPlayers.Length * Seeds];

        // A lost performance fills the stage and is slow; each is its own simulation, so they are played side
        // by side.
        Parallel.For(0, performances.Length, i =>
            performances[i] = ScriptedPlayers.Play(tuning, (ulong)(i % Seeds) + 1, GuardPlayers[i / Seeds].Player));
        return performances;
    }

    private static void PrintTheTable(Tuning tuning)
    {
        (string Name, Func<Simulation, MagicianInput> Player)[] players = GuardPlayers;
        var clock = Stopwatch.StartNew();
        Performance[] performances = PlayTheGuard(tuning);
        clock.Stop();
        Performance Of(int player, int seed) => performances[(player * Seeds) + seed - 1];

        TextWriter table = TestContext.Out;
        table.WriteLine(
            $"first act's budget {tuning.FirstActBudget}, {tuning.BudgetGrowthPerAct} more every act; how each performance ended, in which act, the box office left");
        table.WriteLine("seed  " + string.Join("  ", players.Select(player => $"{player.Name,-15}")));
        for (int seed = 1; seed <= Seeds; seed++)
        {
            table.WriteLine($"{seed,4}  " + string.Join("  ", players.Select((_, player) =>
                $"{(Of(player, seed).Ended == Phase.Ovation ? "ovation" : "closed"),-7} {Of(player, seed).Act,2} {Number(Of(player, seed).BoxOffice, "0"),4}")));
        }

        for (int player = 0; player < players.Length; player++)
        {
            var mine = Enumerable.Range(1, Seeds).Select(seed => Of(player, seed)).ToList();
            table.WriteLine();
            table.WriteLine($"{players[player].Name}: the acts' averages over the performances that played the act");
            table.WriteLine("act  played  entries  dropped  in reach  walked to  applause  encores  fell  box office  most critics at once  cards taken in the encores");
            for (int act = 0; act < mine.Max(played => played.Acts.Count); act++)
            {
                var acts = mine.Where(played => played.Acts.Count > act).Select(played => played.Acts[act]).ToList();
                var taken = mine.Where(played => played.Acts.Count > act).SelectMany(played => played.Encores[act]).ToList();
                string cards = string.Join(" ", ScriptedPlayers.CardOrder
                    .Select(card => (Card: card, Taken: taken.Count(t => t == card)))
                    .Where(count => count.Taken > 0)
                    .Select(count => $"{count.Card}:{count.Taken}"));
                table.WriteLine(
                    $"{act + 1,3}  {acts.Count,6}  {Number(acts.Average(a => a.Entries)),7}  {Number(acts.Average(a => a.Dropped)),7}  {Number(acts.Average(a => a.FellInReach)),8}  {Number(acts.Average(a => a.WalkedTo)),9}  {Number(acts.Average(a => a.Applause)),8}  {Number(acts.Average(a => a.Encores)),7}  {acts.Count(a => a.Fell),4}  {Number(acts.Average(a => a.BoxOffice)),10}  {acts.Max(a => a.MostCritics),21}  {cards}".TrimEnd());
            }

            table.WriteLine(
                $"encores taken in a performance: {Number(mine.Average(played => played.Acts.Sum(a => a.Encores)))}; in the first act in {mine.Count(played => played.Acts[0].Encores > 0)} of {Seeds}; the most critics at once: {mine.Max(played => played.Acts.Max(a => a.MostCritics))}");
        }

        table.WriteLine();
        table.WriteLine(
            $"in reach: a piece dropped within the pick-up reach ({Number(tuning.MagicianRadius + tuning.ApplausePickUpReach, "0.0#")}) of where the magician stood. "
            + $"A critic that touches the magician stands {Number(tuning.MagicianRadius + tuning.EnemyKinds[0].Radius, "0.0#")} from it, "
            + "so while the reach is the longer of the two this counts every critic that fell touching the magician, and says nothing of how safe a player stood.");
        table.WriteLine();
        int orbits = players.Length - 1;
        table.WriteLine($"The guard (decision 22, at least 16 of {Seeds} each).");
        for (int player = 0; player < orbits; player++)
        {
            table.WriteLine(
                $"  {players[player].Name} lost the box office by the end of act six in {Enumerable.Range(1, Seeds).Count(seed => LostByActSix(Of(player, seed)))} of {Seeds}.");
        }

        table.WriteLine(
            "  The orbit player, on whichever circle does best, lost the box office by the end of act six in "
            + $"{Enumerable.Range(1, Seeds).Count(seed => Enumerable.Range(0, orbits).All(player => LostByActSix(Of(player, seed))))} of {Seeds}.");
        table.WriteLine(
            $"  The doors player finished act ten in {Enumerable.Range(1, Seeds).Count(seed => Of(orbits, seed).Ended == Phase.Ovation)} of {Seeds}.");
        table.WriteLine($"{performances.Length} performances in {Number(clock.Elapsed.TotalSeconds)} s.");
    }

    private static bool LostByActSix(Performance played) => played.Ended == Phase.Closed && played.Act <= 6;

    private static string Number(double value, string format = "0.0") => value.ToString(format, CultureInfo.InvariantCulture);
}
