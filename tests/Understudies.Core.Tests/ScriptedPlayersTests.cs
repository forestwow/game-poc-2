using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text.Json.Nodes;

namespace Understudies.Core.Tests;

public class ScriptedPlayersTests
{
    private Tuning Tuning { get; } = CommittedTuning.Parse();

    /// <summary>
    /// The committed numbers on another budget, with a first act half as full again and acts that grow by the
    /// same forty each: the tests that play it assert what they play it for, a player that is crowded and
    /// vanishes and a box office that is struck.
    /// </summary>
    private Tuning Crowded => Tuning with { FirstActBudget = 60, BudgetGrowthPerAct = 40, BudgetGrowthRise = 0 };

    /// <summary>
    /// Plan decision 9's question, whether <c>float</c> gives one result on two machines: two scripted
    /// performances, each pinned at the end of its third act and at its end, on macOS ARM and on Linux x64,
    /// both in CI. The earlier pin says how early a disagreement starts. The first is the doors player on the
    /// committed numbers, asserted to have an encore in it; the second, the orbit player on a circle of ten and a
    /// quarter with a fuller first act, is asserted to have the Vanish and its cloud, stunned critics and blows on
    /// the box office in it, and an end after its third act. A change to tuning.json, to a rule or to a player
    /// changes them: pin them again from the failure's message, and say so in the pull request. If the two
    /// machines ever disagree, that is a finding for the owner and not a test to make pass.
    /// </summary>
    [Test]
    public void Play_TheDoorsPlayerOnSeedOne_EndsInThePinnedStateHashOnEveryMachine()
    {
        Performance performance = ScriptedPlayers.Play(Tuning, seed: 1, ScriptedPlayers.Doors);

        Assert.Multiple(() =>
        {
            Assert.That(performance.Ended, Is.EqualTo(Phase.Ovation));

            // An act that stood for an encore is in what is pinned.
            Assert.That(performance.Acts.Sum(act => act.Encores), Is.GreaterThan(0), "encores");
            Assert.That(performance.Acts[2].StateHash, Is.EqualTo(15201573834497074747UL), "the end of act three");
            Assert.That(performance.Acts[^1].StateHash, Is.EqualTo(14669672916148839855UL), "the end of the performance");
        });
    }

    /// <inheritdoc cref="Play_TheDoorsPlayerOnSeedOne_EndsInThePinnedStateHashOnEveryMachine"/>
    [Test]
    public void Play_TheOrbitPlayerOnSeedOneWithAFullerFirstAct_EndsInThePinnedStateHashOnEveryMachine()
    {
        int vanishes = 0;
        int blows = 0;
        int stunned = 0;

        // On a circle of ten and a quarter, wider than the guard's: on those no fall leaves applause (plan
        // decision 27), and an orbit without an encore pins less. Its budget is its own and has no rise: the rise
        // is pinned by the doors player's performance alone.
        // Why that circle (plan T49, probed on this seed and budget; it was nine before): every circle from 8.2
        // to 10 closes in its second act with no encore, and every one from 10.5 to 13 in its third, where the
        // two pins are one. Ten and a quarter has five encores and closes in its fourth. It is cut by the back
        // wall, where its walker crawls, as nine was and every circle over 8.4 is: pinned as well as anything.
        // ponytail: one circle in a narrow band. When a change closes this performance by act three again, the
        // test says so (its count of acts), and a budget of its own is the sturdier scene than another circle.
        Func<Simulation, MagicianInput> orbit = ScriptedPlayers.Orbit(10.25f);

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
            Assert.That(performance.Acts.Sum(act => act.Encores), Is.GreaterThan(0), "encores");
            Assert.That(performance.Acts, Has.Count.GreaterThan(3), "acts: the two pins are two");
            Assert.That(performance.Acts[2].StateHash, Is.EqualTo(4804231357754467498UL), "the end of act three");
            Assert.That(performance.Acts[^1].StateHash, Is.EqualTo(14242633899946872512UL), "the end of the performance");
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

        // A circle wide enough to earn encores: on the guard's own no fall leaves applause (plan decision 27).
        Func<Simulation, MagicianInput> orbit = ScriptedPlayers.Orbit(13f);
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
                Card.OneMoreCard, Card.ChorusDamage, Card.Damage, Card.AttackSpeed,
                Card.Pierce, Card.Ricochet, Card.Burst,
                Card.Range, Card.VanishCooldown,
            }));
            Assert.That(ScriptedPlayers.Choose([Card.Range, Card.OneMoreCard, Card.Damage]), Is.EqualTo(1));
            Assert.That(ScriptedPlayers.Choose([Card.VanishCooldown, Card.Range, Card.ChorusDamage]), Is.EqualTo(2));
            Assert.That(ScriptedPlayers.Choose([Card.AttackSpeed, Card.Damage]), Is.EqualTo(1));
            Assert.That(ScriptedPlayers.Choose([Card.VanishCooldown, Card.Range]), Is.EqualTo(1));
            Assert.That(ScriptedPlayers.Choose([Card.VanishCooldown]), Is.EqualTo(0));
            Assert.That(ScriptedPlayers.Choose([Card.Burst, Card.Pierce, Card.AttackSpeed]), Is.EqualTo(2));
            Assert.That(ScriptedPlayers.Choose([Card.Burst, Card.Ricochet, Card.Pierce]), Is.EqualTo(2));
            Assert.That(ScriptedPlayers.Choose([Card.Range, Card.VanishCooldown, Card.Burst]), Is.EqualTo(2));
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
        // The tuning's own doors, whichever acts they open in: in from the newest towards the box office, a
        // throw's range and no more than a third of the way.
        Vector2 Post(int act)
        {
            StageDoor newest = Tuning.StageDoors.Where(door => door.OpensInAct <= act).MaxBy(door => door.OpensInAct);
            Vector2 toBoxOffice = Tuning.BoxOfficePosition - newest.Position;
            return newest.Position
                + (Vector2.Normalize(toBoxOffice) * MathF.Min(Tuning.ThrowRange, toBoxOffice.Length() / 3f));
        }

        float step = Tuning.MagicianSpeed / Simulation.TicksPerSecond;
        var actsOnThePost = new List<int>();

        Performance performance = ScriptedPlayers.Play(Tuning, seed: 1, simulation =>
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
            Assert.That(performance.Ended, Is.EqualTo(Phase.Ovation));
            Assert.That(actsOnThePost, Is.EqualTo(Enumerable.Range(1, Tuning.ActsInPerformance)));
            Assert.That(
                Enumerable.Range(1, Tuning.ActsInPerformance).Select(Post).Distinct().Count(),
                Is.EqualTo(Tuning.StageDoors.Count),
                "the performance held every door of the tuning");
        });
    }

    /// <summary>
    /// Plan T37: the doors player stands at its doors, on the floor where a fall earns applause, in every act of
    /// the committed tuning. A throw's range in from the door, as it stood before, was 6.3 and 5.6 from the box
    /// office at the second and third doors.
    /// </summary>
    [Test]
    public void DoorsPost_AtEveryDoorOfTheCommittedTuning_IsAtMostAThirdOfTheWayInAndOutsideTheQuietFloor()
    {
        // Ten short acts that nobody enters: the doors open as the committed tuning has them.
        Tuning empty = Tuning with
        {
            CurtainTime = 0f, ActLength = 1f, FirstActBudget = 0, BudgetGrowthPerAct = 0, BudgetGrowthRise = 0,
        };
        var simulation = new Simulation(empty, seed: 1);
        var posts = new List<Vector2>();
        while (simulation.Phase is not (Phase.Ovation or Phase.Closed))
        {
            posts.Add(ScriptedPlayers.DoorsPost(simulation));
            while (simulation.Phase == Phase.Act)
            {
                simulation.Step(default);
            }

            simulation.GoOn();
        }

        Assert.That(posts.Distinct().Count(), Is.EqualTo(Tuning.StageDoors.Count), "a post for every door");
        Assert.Multiple(() =>
        {
            foreach (Vector2 post in posts.Distinct())
            {
                StageDoor door = Tuning.StageDoors.MinBy(door => Vector2.Distance(door.Position, post));
                float way = Vector2.Distance(door.Position, Tuning.BoxOfficePosition);
                Assert.That(Vector2.Distance(door.Position, post), Is.LessThanOrEqualTo((way / 3f) + 0.001f), $"{post}: from its door");
                Assert.That(
                    Vector2.Distance(post, Tuning.BoxOfficePosition),
                    Is.GreaterThan(Tuning.ApplauseBoxOfficeRadius),
                    $"{post}: from the box office");
            }
        });
    }

    [Test]
    public void Kiter_GoesRoundJustOutsideTheQuietFloor_FetchesItsApplauseAndComesBack()
    {
        Vector2 centre = Tuning.BoxOfficePosition;
        float radius = Tuning.ApplauseBoxOfficeRadius + ScriptedPlayers.KiterMargin;
        float step = Tuning.MagicianSpeed / Simulation.TicksPerSecond;
        var least = new Vector2(float.PositiveInfinity);
        var most = new Vector2(float.NegativeInfinity);
        int ticks = 0;
        int onTheCircle = 0;
        float furthest = 0f;

        // On how many ticks of the performance it stood nearer an open door than the doors player's post at
        // that door is. Its fetching never takes it there; a step back from a critic or a Vanish away from a
        // crowd can, for a moment (plan T37 measured under 0.3 % of its ticks in any act).
        int played = 0;
        int insideAPost = 0;

        Performance performance = ScriptedPlayers.Play(Tuning, seed: 1, simulation =>
        {
            float fromCentre = Vector2.Distance(simulation.MagicianPosition, centre);
            played++;
            insideAPost += Enumerable.Range(0, Tuning.StageDoors.Count).Any(door =>
            {
                Vector2 at = Tuning.StageDoors[door].Position;
                float post = MathF.Min(Tuning.ThrowRange, Vector2.Distance(at, centre) / 3f);
                return simulation.DoorIsOpen(door) && Vector2.Distance(simulation.MagicianPosition, at) < post;
            }) ? 1 : 0;

            if (simulation.Act == 1 && simulation.Phase == Phase.Act)
            {
                ticks++;
                onTheCircle += MathF.Abs(fromCentre - radius) <= step ? 1 : 0;
                furthest = MathF.Max(furthest, fromCentre);
                least = Vector2.Min(least, simulation.MagicianPosition);
                most = Vector2.Max(most, simulation.MagicianPosition);
            }

            return ScriptedPlayers.Kiter(simulation);
        });

        Assert.Multiple(() =>
        {
            // The first act, where forty critics leave it time on its circle: all the way round it, out for
            // applause and no further than its reach and a dodge, and back.
            Assert.That(most.X - least.X, Is.GreaterThanOrEqualTo(2f * radius - 0.1f));
            Assert.That(most.Y - least.Y, Is.GreaterThanOrEqualTo(2f * radius - 0.1f));
            Assert.That(performance.Acts[0].Encores, Is.GreaterThan(0), "encores in the first act");
            Assert.That(furthest, Is.GreaterThan(radius + 1f), "it left its circle");
            Assert.That(furthest, Is.LessThanOrEqualTo(radius + ScriptedPlayers.DoorsReach + Tuning.VanishDistance));
            Assert.That(onTheCircle, Is.GreaterThan(ticks / 4), "ticks on its circle");
            // A dodge or a Vanish can carry it inside for a tick or two (three in this performance): never to stay.
            Assert.That(insideAPost, Is.LessThan(played / 1000), "ticks on the floor before an open door, which it leaves alone");
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
    /// Plan decision 22, the guard the committed numbers are tuned to: the orbit player loses the box office by
    /// the end of act six and the doors player finishes act ten, each in at least 16 of the seeds 1 to 20, which
    /// the numbers were tuned on, and in at least 14 of 101 to 120 (plan T37). Hiding is whichever of the orbit's
    /// circles does best, so a seed counts for the orbit only when every circle lost on it. A change to
    /// tuning.json, to a rule or to a player that breaks this has made hiding pay or the doors lose:
    /// <see cref="PrintTheGuardsTable"/> says where.
    /// </summary>
    [TestCase(1, 16)]
    [TestCase(101, 14)]
    public void TheGuard_OnTheCommittedTuning_TheOrbitLosesByActSixAndTheDoorsPlayerFinishesActTen(int firstSeed, int atLeast)
    {
        Performance[] performances = TheGuard(firstSeed);

        Assert.Multiple(() =>
        {
            Assert.That(
                Enumerable.Range(0, Seeds).Count(seed => Orbits.All(orbit => LostByActSix(performances[(orbit * Seeds) + seed]))),
                Is.GreaterThanOrEqualTo(atLeast),
                "seeds on which the orbit player lost the box office by the end of act six on every circle");
            Assert.That(
                Of(performances, Doors).Count(Finished),
                Is.GreaterThanOrEqualTo(atLeast),
                "seeds on which the doors player finished act ten");
        });
    }

    /// <summary>
    /// Plan T29, two tripwires on the seeds 1 to 20, which the numbers are searched on. The first fails a step
    /// before the guard's count moves: the doors player's box office at the end is at least 350 of 400 on
    /// average. It was 392 before plan T41, when one seed that collapsed left some 370 and two some 350, and the
    /// guard's own floor of 16 finished lets four collapse, which is 320: a tuning between "fine" and "the
    /// guard's floor" is caught here. Since plan T41 it is 370, and no seed collapses: the headliner of acts
    /// eight to ten takes some thirty off every performance (284 at the worst), so the floor is twenty away and
    /// is now what a late threat is tuned against: plan T41 says what a stronger one would ask of it. Since
    /// plan T46 it is 363 (286 at the worst): the sixteen headliners of each of acts eight to ten take 4, 10
    /// and 18, and the floor is thirteen away. The
    /// second is the card order, which must not be a hidden hinge (T29's first numbers held by the committed
    /// order alone and lost act seven on every seed by any other): taking the longer arm first, the doors
    /// player still finishes act ten on at least 16 of 20. It finishes 20 (20 on the seeds 101 to 120 and on
    /// 401 to 420), with 348 of the box office: a break here is a finding about a card or a player, not a
    /// floor to lower unread.
    /// </summary>
    [Test]
    public void TheGuard_OnTheCommittedTuning_TheDoorsPlayerKeepsTheBoxOfficeAndFinishesByAnotherCardOrderToo()
    {
        Performance[] rangeFirst = PlayTheGuard(Tuning, firstSeed: 1, [GuardPlayers[Doors]], RangeFirst);

        Assert.Multiple(() =>
        {
            Assert.That(
                Of(TheGuard(1), Doors).Average(played => played.BoxOffice),
                Is.GreaterThanOrEqualTo(350f),
                "the doors player's box office at the end, on average");
            Assert.That(
                rangeFirst.Count(Finished),
                Is.GreaterThanOrEqualTo(16),
                "seeds on which the doors player finished act ten taking the longer arm first");
        });
    }

    /// <summary>
    /// Plan T37, the kiter as the rival of going out to the doors: over each set of seeds the doors player
    /// finishes act ten at least as often as the kiter, with at least half as many encores again a performance
    /// and at least fifty more of the box office left on average. Since plan T41 the doors player finishes
    /// every seed and the kiter 5 of 20 on the first set and 7 on the second, most of its losses in act eight,
    /// when the headliner comes; the doors player has 35.2 encores against 17.8 and 370 of the box office
    /// against 55 on the first set, 34.5 against 20.2 and 370 against 82 on the second. The encores are the
    /// thin one now (34.5 where 30.3 are asked). With the limit on copies and no headliner the comparison failed:
    /// 395 against 349. Since plan T46 (sixteen headliners an act, which the magician fells) the kiter finishes
    /// every seed of both sets again: 35.1 encores against 18.5 and 363 of the box office against 286 on the
    /// first, 34.9 against 18.8 and 361 against 306 on the second, where the box office is now the thin one
    /// (55 more where 50 are asked). Whether the kiter has to lose outright is the owner's question, open in
    /// the plan: nothing here says it loses, and by the committed card order it does not.
    /// </summary>
    [TestCase(1)]
    [TestCase(101)]
    public void TheGuard_OnTheCommittedTuning_TheDoorsPlayerDoesBetterThanTheKiter(int firstSeed)
    {
        List<Performance> doors = Of(TheGuard(firstSeed), Doors);
        List<Performance> kiter = Of(TheGuard(firstSeed), Kiter);

        Assert.Multiple(() =>
        {
            Assert.That(doors.Count(Finished), Is.GreaterThanOrEqualTo(kiter.Count(Finished)), "seeds finished");
            Assert.That(doors.Average(Encores), Is.GreaterThanOrEqualTo(1.5 * kiter.Average(Encores)), "encores a performance");
            Assert.That(
                doors.Average(played => played.BoxOffice),
                Is.GreaterThanOrEqualTo(kiter.Average(played => played.BoxOffice) + 50f),
                "the box office at the end");
        });
    }

    /// <summary>
    /// Plan T52, night 1 as the ladder's document asks it (its principles 2 and 6, its N4): five acts, critics
    /// alone, no crowds, six tenths of the budget. Whoever moves wins it, the doors player and the kiter on every
    /// seed, and hiding still loses: the orbit's box office falls within the five acts, on every one of its
    /// circles, on at least 10 seeds of 20. It falls on 20 of both sets, in act four on the circles of 3 and 5 and
    /// mostly in act five on the box office itself. The scale is the document's, and nothing was searched: on
    /// the seeds 1 to 20 both halves hold from 0.5 to 1.0, at 0.4 the orbit on the box office itself keeps it,
    /// and at 0.3 every circle does. A change that breaks this has made night 1 lose a player that moves, or
    /// let one that hides through: a finding for the owner, not a floor to loosen.
    /// </summary>
    [TestCase(1)]
    [TestCase(101)]
    public void NightOne_OnTheCommittedNights_WhoeverMovesWinsEverySeedAndTheOrbitStillLoses(int firstSeed)
    {
        Performance[] performances = TheNight(1, firstSeed);

        Assert.Multiple(() =>
        {
            Assert.That(Of(performances, Doors).Count(Finished), Is.EqualTo(Seeds), "seeds on which the doors player finished act five");
            Assert.That(Of(performances, Kiter).Count(Finished), Is.EqualTo(Seeds), "seeds on which the kiter finished act five");
            Assert.That(
                SeedsTheOrbitLostBy(performances, act: 5),
                Is.GreaterThanOrEqualTo(10),
                "seeds on which the orbit player lost the box office within the five acts on every circle");
        });
    }

    /// <summary>
    /// Plan T52, what night 2 was found to bear (seven acts, the stagehand from act four, no crowds, seven tenths
    /// of the budget; the document's scale, not searched). Whoever moves wins it: the doors player and the kiter
    /// each finish act seven on at least 18 seeds of 20 (both finish 20 on both sets). Hiding loses sooner than
    /// on night 1: the orbit's box office falls by the end of act five on every circle on at least 16 (it falls
    /// in act four on every seed of both sets). And the document's lesson of the night, "a lane left alone
    /// leaks", is in the counts: the kiter, which leaves the doors alone, is struck from act four on and ends
    /// with 377 and 376 of the box office, where the doors player ends with all 400; asserted as no less.
    /// </summary>
    [TestCase(1)]
    [TestCase(101)]
    public void NightTwo_OnTheCommittedNights_WhoeverMovesWinsTheOrbitLosesByActFiveAndTheDoorsLeakLeast(int firstSeed)
    {
        Performance[] performances = TheNight(2, firstSeed);
        List<Performance> doors = Of(performances, Doors);
        List<Performance> kiter = Of(performances, Kiter);

        Assert.Multiple(() =>
        {
            Assert.That(doors.Count(Finished), Is.GreaterThanOrEqualTo(18), "seeds on which the doors player finished act seven");
            Assert.That(kiter.Count(Finished), Is.GreaterThanOrEqualTo(18), "seeds on which the kiter finished act seven");
            Assert.That(
                SeedsTheOrbitLostBy(performances, act: 5),
                Is.GreaterThanOrEqualTo(16),
                "seeds on which the orbit player lost the box office by the end of act five on every circle");
            Assert.That(
                doors.Average(played => played.BoxOffice),
                Is.GreaterThanOrEqualTo(kiter.Average(played => played.BoxOffice)),
                "the box office at the end: the doors player's against the kiter's");
        });
    }

    /// <summary>
    /// The instrument, read out: the orbit player on each of its circles, the doors player and the kiter over
    /// the seeds 1 to 20 on the committed numbers, act by act, and the guard's counts at the foot.
    /// </summary>
    [Test]
    [Explicit("Prints how the orbit player, the doors player and the kiter end over 20 seeds on the committed tuning, and the guard's counts (a few seconds)")]
    public void PrintTheGuardsTable() => PrintTheTable(Tuning);

    /// <summary>
    /// The guard's table in short, for whoever weighs one tuning against another: every variant on the seeds 1
    /// to 20, which numbers are searched on, and on no others unless they are asked for: the environment variable
    /// <c>UNDERSTUDIES_SEED_SETS</c> names the first seed of every set to play, <c>1,101</c> for the second set
    /// as well and <c>401</c> for the next alone. The second is read once, on a candidate, and the next only
    /// for the last check of a tuning (plan T37): a set that every probe is read on is spent. A variant is the
    /// committed tuning.json with some of its keys given other values. They are read from the file the
    /// environment variable <c>UNDERSTUDIES_VARIANTS</c> names, a variant a line, a name and a JSON object of
    /// the keys: <c>gentle | { "firstActBudget": 60, "budgetGrowthPerAct": 120 }</c>. A line that is empty or
    /// starts with # is none. Without the variable it is the committed tuning alone. Beside the guard's players it
    /// plays the doors player with no applause in the first act, the roamer, and the orbit on two circles wider
    /// than the guard's: none of the four counts for the guard. For every player but the orbit it prints by act
    /// what share of the magician's own kills fell within three, five and eight seconds of entering. Then the doors
    /// player and the kiter by five other orders of taking cards ("range first", "one more card last", and each of
    /// plan T25's three cards first), with the spread over the six; and for the doors player by every order what
    /// plan T41 weighs a limit on copies by: by act the encores, the most cards of one throw, the cards held of
    /// each kind, its own kills and its understudies', and the encores that had fewer than three cards to offer
    /// or were earned and did not open. With the environment variable <c>UNDERSTUDIES_GUARD_ONLY</c> set the four
    /// players outside the guard are not played, which is some three times as quick. With the environment
    /// variable <c>UNDERSTUDIES_NIGHT</c> it is a night that is played (plan T52): the committed tuning under that
    /// night's committed overlay of nights.json, and a variant's keys that are a night's own
    /// (<see cref="NightKeys"/>: <c>{ "budgetScale": 0.5 }</c> is the scale to try) are that night's and the
    /// rest the tuning's. Nothing here counts on ten acts; the line headed "the guard" counts a loss by act six
    /// on a night of any length, which on one of five acts is any loss: read a player's own line for the act.
    /// </summary>
    [Test]
    [Explicit("Prints the guard in short for every variant of the tuning in the file UNDERSTUDIES_VARIANTS names, or of the night UNDERSTUDIES_NIGHT names, on the seeds 1 to 20 and on the sets UNDERSTUDIES_SEED_SETS names (a few seconds a variant and set)")]
    public void PrintTheVariants()
    {
        int? night = Environment.GetEnvironmentVariable("UNDERSTUDIES_NIGHT") is { } number
            ? int.Parse(number, CultureInfo.InvariantCulture)
            : null;
        string? file = Environment.GetEnvironmentVariable("UNDERSTUDIES_VARIANTS");
        IEnumerable<string> lines = file is null ? ["committed | {}"] : File.ReadLines(file);
        TextWriter table = TestContext.Out;
        table.WriteLine(
            "lost: the act a performance closed in and whether the magician stood (up) or had fallen (down) when the box office fell, and on how many seeds. "
            + "By act: averages over the performances that played the act, but the most critics at once, which is the most on any seed.");
        foreach (string line in lines.Select(line => line.Trim()).Where(line => line.Length > 0 && line[0] != '#'))
        {
            string[] parts = line.Split('|', 2);
            var json = JsonNode.Parse(CommittedTuning.Json)!.AsObject();
            JsonObject? overlay = night is null
                ? null
                : JsonNode.Parse(CommittedNights.Json)!.AsArray().Single(entry => (int)entry!["night"]! == night)!.DeepClone().AsObject();
            foreach ((string key, JsonNode? value) in JsonNode.Parse(parts[1])!.AsObject())
            {
                // Tuning.Parse refuses a key it does not know, so a misspelt one is not played in silence.
                (overlay is not null && NightKeys.Contains(key) ? overlay : json)[key] = value?.DeepClone();
            }

            Tuning tuning = Tuning.Parse(json.ToJsonString());
            PrintAVariant(
                table,
                $"{(night is null ? "" : $"night {night}: ")}{parts[0].Trim()} {parts[1].Trim()}",
                overlay is null ? tuning : Night.Compose(tuning, Night.Parse($"[{overlay.ToJsonString()}]"), night!.Value));
        }
    }

    /// <summary>
    /// The keys of a variant that are a night's own when <see cref="PrintTheVariants"/> plays a night.
    /// ponytail: the names of <see cref="Night"/>'s keys, written out. A key added to a night and not here is
    /// refused as a key of the tuning, unless the tuning has one of that name, and then the night's committed
    /// value is played over the variant's in silence: add it here with the key.
    /// </summary>
    private static readonly string[] NightKeys = ["actsInPerformance", "budgetScale", "kindsAllowed", "waveBurstShare"];

    /// <summary>
    /// What the one order the players take cards by decides (plan T37): the doors player and the kiter on the
    /// committed tuning, over the seeds 1 to 20 and the sets <c>UNDERSTUDIES_SEED_SETS</c> names, taking their
    /// cards by the committed order, by its reverse, with "one more card" last, and with the longer arm first;
    /// and, since plan T25, with each of its three cards first, and with each of them in the place of "one more
    /// card", which is then taken last; and with the three behind every other card, as they do what they do
    /// and made to do nothing.
    /// </summary>
    [Test]
    [Explicit("Prints how the doors player and the kiter end on the committed tuning when they take their cards by eleven different orders, one of them twice (some ten seconds a set)")]
    public void PrintTheCardOrders()
    {
        (string Name, IReadOnlyList<Card> Order)[] orders =
        [
            ("committed", ScriptedPlayers.CardOrder),
            ("reversed", [.. ScriptedPlayers.CardOrder.Reverse()]),
            ("one more card last", OneMoreCardLast),
            ("range first", RangeFirst),
            ("pierce first", First(Card.Pierce)),
            ("ricochet first", First(Card.Ricochet)),
            ("burst first", First(Card.Burst)),

            // Each of the three in the place of "one more card", which is then taken last: the one against the
            // other, where the rows above have both.
            .. new[] { Card.Pierce, Card.Ricochet, Card.Burst }.Select(card => (
                $"{card} for one more".ToLowerInvariant(),
                (IReadOnlyList<Card>)[card, .. OneMoreCardLast.Where(other => other != card)])),

            // The order plan T25 was asked for and did not take: the three behind the longer arm and the quicker
            // Vanish, which the players then take. Its second row has the three made to do nothing: what the
            // offer of eight does by itself.
            ("the three last", ThreeLast),
        ];
        (string Name, Func<Simulation, MagicianInput> Player)[] players = [GuardPlayers[Doors], GuardPlayers[Kiter]];
        TextWriter table = TestContext.Out;
        foreach (int firstSeed in (Environment.GetEnvironmentVariable("UNDERSTUDIES_SEED_SETS") ?? "1")
            .Split(',').Select(first => int.Parse(first, CultureInfo.InvariantCulture)))
        {
            table.WriteLine($"seeds {firstSeed}-{firstSeed + Seeds - 1}");
            foreach ((string name, IReadOnlyList<Card> order) in orders)
            {
                Print(name, PlayTheGuard(Tuning, firstSeed, players, order));
            }

            Tuning nothing = Tuning with { CardPierceLoss = 1000f, CardRicochetReach = 0f, CardBurstRadius = 0f };
            Print("the three last, null", PlayTheGuard(nothing, firstSeed, players, ThreeLast));

            void Print(string name, Performance[] performances)
            {
                for (int player = 0; player < players.Length; player++)
                {
                    List<Performance> mine = Of(performances, player);
                    table.WriteLine(
                        $"  {name,-21} {players[player].Name,-5}: finished {mine.Count(Finished),2}, lost {HowLost(mine)}; encores {Number(mine.Average(Encores))}; "
                        + $"box office at the end {Number(mine.Average(played => played.BoxOffice), "0")}, the worst {Number(mine.Min(played => played.BoxOffice), "0")}");
                }
            }
        }
    }

    private static void PrintAVariant(TextWriter table, string name, Tuning tuning)
    {
        // The doors player with no applause in the first act, and so no encore in it: whether a player with no
        // card yet lives through the second act. The simulation takes new numbers between two ticks.
        Tuning noApplause = tuning with { ApplauseTime = 0f };
        (string Name, Func<Simulation, MagicianInput> Player)[] players = Environment.GetEnvironmentVariable("UNDERSTUDIES_GUARD_ONLY") is not null ? GuardPlayers :
        [
            .. GuardPlayers,
            ("doors, no applause in act one", simulation =>
            {
                simulation.Tuning = simulation.Act == 1 ? noApplause : tuning;
                return ScriptedPlayers.Doors(simulation);
            }),

            // The kiter without its bound: it follows the applause out to the doors.
            ("roamer, outside the guard", ScriptedPlayers.Roamer),

            // Circles wider than the guard's: what a rule about the floor near the box office leaves to a player
            // that walks round just outside it.
            .. new[] { 9f, 13f }.Select(radius => ($"orbit {Number(radius)}, outside the guard", ScriptedPlayers.Orbit(radius))),
        ];
        table.WriteLine();
        table.WriteLine($"== {name}");
        table.WriteLine($"   enemies by act (seed 1): {string.Join(" ", Waves.Plan(tuning, seed: 1).Select(act => act.Count))}");
        foreach (int firstSeed in (Environment.GetEnvironmentVariable("UNDERSTUDIES_SEED_SETS") ?? "1")
            .Split(',').Select(first => int.Parse(first, CultureInfo.InvariantCulture)))
        {
            var clock = Stopwatch.StartNew();
            Performance[] performances = PlayTheGuard(tuning, firstSeed, players);
            clock.Stop();
            List<Performance> Of(int player) => [.. performances.Skip(player * Seeds).Take(Seeds)];

            int hidingLost = Enumerable.Range(0, Seeds).Count(seed =>
                Orbits.All(orbit => LostByActSix(performances[(orbit * Seeds) + seed])));
            table.WriteLine(
                $"   seeds {firstSeed}-{firstSeed + Seeds - 1}: the guard: orbit {hidingLost} ({string.Join(", ", Orbits.Select(orbit => Of(orbit).Count(LostByActSix)))}), "
                + $"kiter {Of(Kiter).Count(LostByActSix)}, doors {Of(Doors).Count(played => played.Ended == Phase.Ovation)}   [{Number(clock.Elapsed.TotalSeconds)} s]");
            for (int player = 0; player < players.Length; player++)
            {
                List<Performance> mine = Of(player);
                string ByAct(Func<List<ActRecord>, string> of) => string.Join(" ", Enumerable.Range(0, mine.Max(played => played.Act))
                    .Select(act => of([.. mine.Where(played => played.Act > act).Select(played => played.Acts[act])])));
                table.WriteLine(
                    $"     {players[player].Name}: lost {HowLost(mine)}; encores {Number(mine.Average(played => played.Acts.Sum(act => act.Encores)))}; "
                    + $"box office at the end {Number(mine.Average(played => played.BoxOffice), "0")}, the worst {Number(mine.Min(played => played.BoxOffice), "0")}; "
                    + $"past act two in {mine.Count(played => played.Act > 2)}");
                if (!players[player].Name.StartsWith("orbit", StringComparison.Ordinal))
                {
                    table.WriteLine($"       encores by act      {ByAct(acts => Number(acts.Average(act => act.Encores)))}");
                    table.WriteLine($"       picked up / dropped {ByAct(acts => $"{Number(acts.Average(act => act.Applause), "0")}/{Number(acts.Average(act => act.Dropped), "0")}")}");
                    table.WriteLine($"       box office by act   {ByAct(acts => Number(acts.Average(act => act.BoxOffice), "0"))}");
                    table.WriteLine($"       fell by act         {ByAct(acts => acts.Count(act => act.Fell).ToString(CultureInfo.InvariantCulture))}");
                    table.WriteLine($"       its kills by act    {ByAct(acts => Number(acts.Average(act => act.Kills), "0"))}");
                    table.WriteLine(
                        "       % of them within 3/5/8 s of entering "
                        + ByAct(acts => string.Join("/", new Func<ActRecord, int>[] { act => act.KillsWithin3, act => act.KillsWithin5, act => act.KillsWithin8 }
                            .Select(within => Number(100.0 * acts.Sum(within) / Math.Max(1, acts.Sum(act => act.Kills)), "0")))));
                }

                table.WriteLine($"       most critics by act {ByAct(acts => acts.Max(act => act.MostCritics).ToString(CultureInfo.InvariantCulture))}");
            }

            // The doors player and the kiter by other orders of taking cards (plan T29, and plan T41 for the three
            // new cards first): a tuning that holds by the committed order alone measures the order and not the
            // route. The doors player's cards are printed by act under every order, the committed one first.
            (string Name, Func<Simulation, MagicianInput> Player)[] two = [GuardPlayers[Doors], GuardPlayers[Kiter]];
            var doorsByOrder = new List<List<Performance>> { Of(Doors) };
            var kiterByOrder = new List<List<Performance>> { Of(Kiter) };
            PrintTheCards(table, "the committed order", Of(Doors));
            foreach ((string order, IReadOnlyList<Card> cards) in new[]
            {
                ("range first", RangeFirst), ("one more card last", OneMoreCardLast),
                ("pierce first", First(Card.Pierce)), ("ricochet first", First(Card.Ricochet)), ("burst first", First(Card.Burst)),
            })
            {
                Performance[] byOrder = PlayTheGuard(tuning, firstSeed, two, cards);
                for (int player = 0; player < two.Length; player++)
                {
                    List<Performance> mine = [.. byOrder.Skip(player * Seeds).Take(Seeds)];
                    (player == 0 ? doorsByOrder : kiterByOrder).Add(mine);
                    string boxOffice = string.Join(" ", Enumerable.Range(0, mine.Max(played => played.Act))
                        .Select(act => Number(mine.Where(played => played.Act > act).Average(played => played.Acts[act].BoxOffice), "0")));
                    table.WriteLine(
                        $"     by {order}, {two[player].Name}: finished {mine.Count(Finished)}, lost {HowLost(mine)}; encores {Number(mine.Average(Encores))}; "
                        + $"box office at the end {Number(mine.Average(played => played.BoxOffice), "0")}, the worst {Number(mine.Min(played => played.BoxOffice), "0")}; by act {boxOffice}");
                }

                PrintTheCards(table, order, doorsByOrder[^1]);
            }

            // How far the order moves a player (plan T41): the fewest and the most over the six orders.
            foreach ((string who, List<List<Performance>> orders) in new[] { ("doors", doorsByOrder), ("kiter", kiterByOrder) })
            {
                table.WriteLine(
                    $"     the spread over the six orders, {who}: finished {orders.Min(mine => mine.Count(Finished))} to {orders.Max(mine => mine.Count(Finished))}; "
                    + $"box office at the end {Number(orders.Min(mine => mine.Average(played => played.BoxOffice)), "0")} to {Number(orders.Max(mine => mine.Average(played => played.BoxOffice)), "0")}; "
                    + $"encores {Number(orders.Min(mine => mine.Average(Encores)))} to {Number(orders.Max(mine => mine.Average(Encores)))}");
            }

            table.WriteLine(
                $"     orders by which the kiter ends ahead of the doors player (finished, or the box office): "
                + $"{Enumerable.Range(0, doorsByOrder.Count).Count(order => kiterByOrder[order].Count(Finished) > doorsByOrder[order].Count(Finished) || kiterByOrder[order].Average(played => played.BoxOffice) > doorsByOrder[order].Average(played => played.BoxOffice))} of {doorsByOrder.Count}");
        }
    }

    /// <summary>
    /// What a player had and did with it, by act (plan T41): the encores it took, the cards it threw at once at the
    /// most, the self cards it held of each kind when the act was over, who made the kills, and whether an encore
    /// had fewer than three cards to offer or was earned and did not open. Averages over the performances that
    /// played the act.
    /// </summary>
    private static void PrintTheCards(TextWriter table, string order, List<Performance> mine)
    {
        string ByAct(Func<List<ActRecord>, string> of) => string.Join(" ", Enumerable.Range(0, mine.Max(played => played.Act))
            .Select(act => of([.. mine.Where(played => played.Act > act).Select(played => played.Acts[act])])));
        table.WriteLine($"       doors by {order}: encores by act {ByAct(acts => Number(acts.Average(act => act.Encores)))}");
        table.WriteLine($"         most cards a throw  {ByAct(acts => Number(acts.Average(act => act.MostCardsAThrow)))}");
        table.WriteLine(
            "         cards held (damage/attack speed/range/vanish/one more/pierce/ricochet/burst) "
            + ByAct(acts => string.Join("/", new Func<SelfCards, int>[]
            {
                cards => cards.Damage, cards => cards.AttackSpeed, cards => cards.Range, cards => cards.VanishCooldown,
                cards => cards.OneMoreCard, cards => cards.Pierce, cards => cards.Ricochet, cards => cards.Burst,
            }.Select(kind => Number(acts.Average(act => kind(act.Cards)), "0.#")))));
        table.WriteLine($"         kills, its own/understudies' {ByAct(acts => $"{Number(acts.Average(act => act.Kills), "0")}/{Number(acts.Average(act => act.UnderstudyKills), "0")}")}");
        table.WriteLine(
            "         headliners entered/felled by the magician/by an understudy, and seconds one that fell stood "
            + ByAct(acts => $"{Number(acts.Average(act => act.Headliners))}/{Number(acts.Average(act => act.HeadlinerKills))}/{Number(acts.Average(act => act.HeadlinerUnderstudyKills))}"
                + $" {Number(acts.Sum(act => act.HeadlinerTicksLived) / (double)Simulation.TicksPerSecond / Math.Max(1, acts.Sum(act => act.HeadlinerKills + act.HeadlinerUnderstudyKills)))}s"));
        table.WriteLine(
            $"         encores that offered fewer than three {ByAct(acts => Number(acts.Average(act => act.ShortOffers)))}; "
            + $"acts that ended with an encore earned and not opened {ByAct(acts => acts.Count(act => act.NextCost > 0 && act.Unspent >= act.NextCost).ToString(CultureInfo.InvariantCulture))}; "
            + $"applause unspent {ByAct(acts => Number(acts.Average(act => act.Unspent)))}");
    }

    /// <summary>
    /// How the lost of <paramref name="performances"/> were lost: the act the box office fell in, whether the
    /// magician stood then, and how many were lost so, the earliest act first.
    /// </summary>
    private static string HowLost(IEnumerable<Performance> performances)
    {
        var lost = performances.Where(played => played.Ended == Phase.Closed)
            .GroupBy(played => (played.Act, played.Acts[^1].Fell))
            .OrderBy(group => group.Key)
            .Select(group => $"act {group.Key.Act} {(group.Key.Fell ? "down" : "up")} x{group.Count()}")
            .ToList();
        return lost.Count == 0 ? "never" : string.Join(", ", lost);
    }

    /// <summary>The committed order with one card taken before anything else.</summary>
    private static IReadOnlyList<Card> First(Card first) =>
        [first, .. ScriptedPlayers.CardOrder.Where(card => card != first)];

    /// <summary>The committed order with the three cards of plan T25 behind every other.</summary>
    private static readonly IReadOnlyList<Card> ThreeLast =
    [
        .. ScriptedPlayers.CardOrder.Where(card => card is not (Card.Pierce or Card.Ricochet or Card.Burst)),
        Card.Pierce, Card.Ricochet, Card.Burst,
    ];

    /// <summary>The committed order with the longer arm taken before anything else.</summary>
    private static readonly IReadOnlyList<Card> RangeFirst = First(Card.Range);

    /// <summary>The committed order with "one more card" taken after everything else.</summary>
    private static readonly IReadOnlyList<Card> OneMoreCardLast =
        [.. ScriptedPlayers.CardOrder.Where(card => card != Card.OneMoreCard), Card.OneMoreCard];

    /// <summary>How many seeds the guard is read over: 1 to this, where nothing says where they start.</summary>
    private const int Seeds = 20;

    /// <summary>
    /// Who plays the guard: the orbit on each of its circles (<see cref="Orbits"/>), then the doors player
    /// (<see cref="Doors"/>) and the kiter (<see cref="Kiter"/>).
    /// </summary>
    private static readonly (string Name, Func<Simulation, MagicianInput> Player)[] GuardPlayers =
    [
        .. ScriptedPlayers.OrbitRadii.Select(radius => ($"orbit {Number(radius)}", ScriptedPlayers.Orbit(radius))),
        ("doors", ScriptedPlayers.Doors),
        ("kiter", ScriptedPlayers.Kiter),
    ];

    /// <summary>The places in <see cref="GuardPlayers"/> of the orbit's circles.</summary>
    private static IEnumerable<int> Orbits => Enumerable.Range(0, ScriptedPlayers.OrbitRadii.Count);

    private static int Doors => ScriptedPlayers.OrbitRadii.Count;

    private static int Kiter => Doors + 1;

    /// <summary>
    /// The guard's performances on the committed tuning over the seeds from 1 and from 101, played once for
    /// every test that reads them.
    /// </summary>
    private static readonly Lazy<Performance[]>[] Played =
    [
        new(() => PlayTheGuard(CommittedTuning.Parse(), firstSeed: 1)),
        new(() => PlayTheGuard(CommittedTuning.Parse(), firstSeed: 101)),
    ];

    private static Performance[] TheGuard(int firstSeed) => firstSeed switch
    {
        1 => Played[0].Value,
        101 => Played[1].Value,
        _ => throw new ArgumentOutOfRangeException(nameof(firstSeed), firstSeed, "the guard is read from the seeds 1 and 101"),
    };

    /// <summary>
    /// The guard's players on the committed nights 1 and 2 (plan T52), each over the seeds from 1 and from 101,
    /// played once for every test that reads them.
    /// </summary>
    private static readonly Lazy<Performance[]>[] NightsPlayed =
    [
        .. from night in new[] { 1, 2 }
           from firstSeed in new[] { 1, 101 }
           select new Lazy<Performance[]>(() => PlayTheGuard(CommittedNights.Tuning(night), firstSeed)),
    ];

    private static Performance[] TheNight(int night, int firstSeed) =>
        NightsPlayed[((night - 1) * 2) + (firstSeed == 1 ? 0 : 1)].Value;

    /// <summary>
    /// On how many seeds hiding lost by the end of <paramref name="act"/>: the box office fell by then on every
    /// one of the orbit's circles, as the guard counts it.
    /// </summary>
    private static int SeedsTheOrbitLostBy(Performance[] performances, int act) =>
        Enumerable.Range(0, Seeds).Count(seed => Orbits.All(orbit =>
            performances[(orbit * Seeds) + seed] is { Ended: Phase.Closed } played && played.Act <= act));

    /// <summary>The performances of the player at <paramref name="player"/> among those of one set of seeds.</summary>
    private static List<Performance> Of(Performance[] performances, int player) =>
        [.. performances.Skip(player * Seeds).Take(Seeds)];

    private static bool Finished(Performance played) => played.Ended == Phase.Ovation;

    private static int Encores(Performance played) => played.Acts.Sum(act => act.Encores);

    /// <summary>
    /// Every one of <paramref name="players"/> (<see cref="GuardPlayers"/> when none are named) over
    /// <see cref="Seeds"/> seeds from <paramref name="firstSeed"/>: a player's performances side by side, a seed
    /// after a seed.
    /// </summary>
    private static Performance[] PlayTheGuard(
        Tuning tuning,
        int firstSeed = 1,
        (string Name, Func<Simulation, MagicianInput> Player)[]? players = null,
        IReadOnlyList<Card>? order = null)
    {
        players ??= GuardPlayers;
        var performances = new Performance[players.Length * Seeds];

        // A lost performance fills the stage and is slow; each is its own simulation, so they are played side
        // by side.
        Parallel.For(0, performances.Length, i =>
            performances[i] = ScriptedPlayers.Play(tuning, (ulong)((i % Seeds) + firstSeed), players[i / Seeds].Player, order));
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
            $"first act's budget {tuning.FirstActBudget}, {tuning.BudgetGrowthPerAct} more in the second and that step {tuning.BudgetGrowthRise} bigger in every act after; how each performance ended, in which act, the box office left");
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
        table.WriteLine($"The guard (decision 22 and plan T37, at least 16 of {Seeds} each).");
        foreach (int player in Orbits)
        {
            table.WriteLine(
                $"  {players[player].Name} lost the box office by the end of act six in {Enumerable.Range(1, Seeds).Count(seed => LostByActSix(Of(player, seed)))} of {Seeds}.");
        }

        table.WriteLine(
            "  The orbit player, on whichever circle does best, lost the box office by the end of act six in "
            + $"{Enumerable.Range(1, Seeds).Count(seed => Orbits.All(player => LostByActSix(Of(player, seed))))} of {Seeds}.");
        table.WriteLine(
            $"  The kiter lost the box office by the end of act six in {Enumerable.Range(1, Seeds).Count(seed => LostByActSix(Of(Kiter, seed)))} of {Seeds}.");
        table.WriteLine(
            $"  The doors player finished act ten in {Enumerable.Range(1, Seeds).Count(seed => Of(Doors, seed).Ended == Phase.Ovation)} of {Seeds}.");
        table.WriteLine($"{performances.Length} performances in {Number(clock.Elapsed.TotalSeconds)} s.");
    }

    private static bool LostByActSix(Performance played) => played.Ended == Phase.Closed && played.Act <= 6;

    private static string Number(double value, string format = "0.0") => value.ToString(format, CultureInfo.InvariantCulture);
}
