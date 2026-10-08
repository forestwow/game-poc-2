using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Understudies.Core.Tests;

public class ScriptedPlayersTests
{
    private Tuning Tuning { get; } = CommittedTuning.Parse();

    /// <summary>
    /// The committed numbers with fuller acts, in which a player is crowded and vanishes: on the committed ones
    /// the doors player of seed 1 never has to, and the tests of where a player goes would say nothing of a Vanish.
    /// </summary>
    private Tuning Crowded => Tuning with { FirstActBudget = 45, BudgetGrowthPerAct = 40 };

    /// <summary>
    /// Plan decision 9's question, whether <c>float</c> gives one result on two machines: the doors player on seed
    /// 1 on the committed numbers, pinned at the end of its third act and at the end of the performance, here
    /// (macOS ARM) and in CI (Linux x64). The earlier pin says how early a disagreement starts. A change to
    /// tuning.json, to a rule or to the doors player changes both: pin them again, and say so in the pull request.
    /// If the two machines ever disagree, that is a finding for the owner and not a test to make pass.
    /// </summary>
    [Test]
    public void Play_TheDoorsPlayerOnSeedOne_EndsInThePinnedStateHashOnEveryMachine()
    {
        Performance performance = ScriptedPlayers.Play(Tuning, seed: 1, ScriptedPlayers.Doors);

        Assert.Multiple(() =>
        {
            Assert.That(performance.Acts[2].StateHash, Is.EqualTo(6411592599900307806UL), "the end of act three");
            Assert.That(performance.Acts[^1].StateHash, Is.EqualTo(9777240901492940968UL), "the end of the performance");
        });
    }

    [Test]
    public void Play_TheSameSeedAndPlayerTwice_GiveTheSameRecord()
    {
        Performance one = ScriptedPlayers.Play(Tuning, seed: 3, ScriptedPlayers.Orbit);
        Performance other = ScriptedPlayers.Play(Tuning, seed: 3, ScriptedPlayers.Orbit);

        Assert.Multiple(() =>
        {
            Assert.That(other.Ended, Is.EqualTo(one.Ended));
            Assert.That(other.Acts, Is.EqualTo(one.Acts));
        });
    }

    [Test]
    public void Play_ThePerActNumbers_AddUpToTheSimulationsOwn()
    {
        Performance performance = ScriptedPlayers.Play(Tuning, seed: 2, ScriptedPlayers.Doors);
        IReadOnlyList<IReadOnlyList<PlannedEntry>> plan = Waves.Plan(Tuning, seed: 2);

        // The plan is drawn apart from the performance; the pieces are counted from the events, and the
        // applause is the simulation's own count.
        Assert.That(performance.Acts.Sum(act => act.Applause), Is.GreaterThan(0), "the performance has applause to count");
        for (int i = 0; i < performance.Acts.Count; i++)
        {
            ActRecord act = performance.Acts[i];
            bool playedOut = i < performance.Acts.Count - 1 || performance.Ended == Phase.Ovation;
            Assert.Multiple(() =>
            {
                Assert.That(act.Entries, playedOut ? Is.EqualTo(plan[i].Count) : Is.LessThanOrEqualTo(plan[i].Count));
                Assert.That(act.FellInReach + act.WalkedTo, Is.EqualTo(act.Applause));
                Assert.That(act.Dropped, Is.GreaterThanOrEqualTo(act.Applause));
                Assert.That(act.Share, Is.EqualTo((float)act.Applause / act.Entries));
                Assert.That(act.Card is null, Is.EqualTo(act.Applause == 0 || i == performance.Acts.Count - 1));
            });
        }
    }

    [Test]
    public void Choose_TakesTheCardThatComesFirstInTheOrder_WhereverItIsOffered()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ScriptedPlayers.Choose([Card.Range, Card.OneMoreCard, Card.Damage]), Is.EqualTo(1));
            Assert.That(ScriptedPlayers.Choose([Card.VanishCooldown, Card.Range, Card.ChorusDamage]), Is.EqualTo(2));
            Assert.That(ScriptedPlayers.Choose([Card.AttackSpeed, Card.Damage]), Is.EqualTo(1));
            Assert.That(ScriptedPlayers.Choose([Card.VanishCooldown]), Is.EqualTo(0));

            // Every card has its place in the order: none is taken or passed over by chance.
            Assert.That(ScriptedPlayers.CardOrder, Is.EquivalentTo(Enum.GetValues<Card>()));
        });
    }

    [Test]
    public void Orbit_GoesRoundTheBoxOffice_AndNeverLeavesItsCircle()
    {
        Vector2 centre = Tuning.BoxOfficePosition;
        float furthest = 0f;
        var least = new Vector2(float.PositiveInfinity);
        var most = new Vector2(float.NegativeInfinity);
        int vanishes = 0;

        ScriptedPlayers.Play(Crowded, seed: 1, simulation =>
        {
            vanishes += simulation.Events.Count(happened => happened.Kind == TickEventKind.Vanish);
            furthest = MathF.Max(furthest, Vector2.Distance(simulation.MagicianPosition, centre));
            least = Vector2.Min(least, simulation.MagicianPosition);
            most = Vector2.Max(most, simulation.MagicianPosition);
            return ScriptedPlayers.Orbit(simulation);
        });

        Assert.Multiple(() =>
        {
            Assert.That(vanishes, Is.GreaterThan(0), "the performance has a Vanish in it");

            // On the circle the float's last digit may fall either side of it.
            Assert.That(furthest, Is.LessThanOrEqualTo(ScriptedPlayers.OrbitRadius + 0.001f));

            // All the way round: as far left, right, up and down as the circle goes.
            var across = new Vector2(2f * ScriptedPlayers.OrbitRadius);
            Assert.That(most.X - least.X, Is.EqualTo(across.X).Within(0.1f));
            Assert.That(most.Y - least.Y, Is.EqualTo(across.Y).Within(0.1f));
        });
    }

    [Test]
    public void Doors_HoldsThePostOfTheNewestDoor_AndIsFurtherThanItsReachOnlyAfterAVanish()
    {
        // From the mark it walks to the post; from then on in that act only a Vanish takes it further from the
        // post than its reach, and it walks straight back: no longer than it takes to walk a Vanish's distance.
        float step = Tuning.MagicianSpeed / Simulation.TicksPerSecond;
        int ticksBack = (int)(Tuning.VanishDistance / step) + 2;
        var postsHeld = new List<Vector2>();
        int act = 0;
        bool arrived = false;
        int ticksSinceVanish = int.MaxValue;
        int strays = 0;
        int vanishes = 0;

        ScriptedPlayers.Play(Crowded, seed: 1, simulation =>
        {
            Vector2 post = ScriptedPlayers.DoorsPost(simulation);
            float fromPost = Vector2.Distance(simulation.MagicianPosition, post);
            if (simulation.Act != act)
            {
                act = simulation.Act;
                arrived = false;
            }

            if (!arrived && fromPost <= step)
            {
                arrived = true;
                postsHeld.Add(post);
            }

            ticksSinceVanish = simulation.Events.Any(happened => happened.Kind == TickEventKind.Vanish)
                ? 0
                : ticksSinceVanish == int.MaxValue ? int.MaxValue : ticksSinceVanish + 1;
            vanishes += ticksSinceVanish == 0 ? 1 : 0;
            if (arrived
                && !simulation.MagicianHasFallen
                && fromPost > ScriptedPlayers.DoorsReach + 0.001f
                && ticksSinceVanish > ticksBack)
            {
                strays++;
            }

            return ScriptedPlayers.Doors(simulation);
        });

        Assert.Multiple(() =>
        {
            Assert.That(vanishes, Is.GreaterThan(0), "the performance has a Vanish in it");
            Assert.That(strays, Is.Zero);

            // The committed doors open in the acts 1, 3 and 6, and each is held from its act until the next
            // opens: a throw's range in from the door, on the straight line from it to the box office.
            StageDoor[] held = [.. new[] { 0, 0, 1, 1, 1, 2, 2, 2, 2, 2 }.Select(door => Tuning.StageDoors[door])];
            Assert.That(postsHeld, Has.Count.EqualTo(held.Length));
            for (int i = 0; i < Math.Min(held.Length, postsHeld.Count); i++)
            {
                float fromDoor = Vector2.Distance(held[i].Position, postsHeld[i]);
                float toBoxOffice = Vector2.Distance(postsHeld[i], Tuning.BoxOfficePosition);
                Assert.That(fromDoor, Is.EqualTo(Tuning.ThrowRange).Within(0.001f), $"act {i + 1}");
                Assert.That(
                    fromDoor + toBoxOffice,
                    Is.EqualTo(Vector2.Distance(held[i].Position, Tuning.BoxOfficePosition)).Within(0.001f),
                    $"act {i + 1}");
            }
        });
    }

    /// <summary>
    /// The instrument, read out and not asserted (asserting it is plan T20): both players over the seeds 1 to 20
    /// on the committed numbers, and decision 22's two counts at the foot.
    /// </summary>
    [Test]
    [Explicit("Prints how the orbit player and the doors player end over 20 seeds on the committed tuning, and the guard's two counts (a few seconds)")]
    public void PrintTheGuardsTable()
    {
        const int Seeds = 20;
        (string Name, Func<Simulation, MagicianInput> Player)[] players =
        [
            ("orbit", ScriptedPlayers.Orbit), ("doors", ScriptedPlayers.Doors),
        ];
        Tuning tuning = Tuning;
        var performances = new Performance[players.Length * Seeds];
        var clock = Stopwatch.StartNew();

        // A lost performance fills the stage and is slow; each is its own simulation, so they are played side
        // by side.
        Parallel.For(0, performances.Length, i =>
            performances[i] = ScriptedPlayers.Play(tuning, (ulong)(i % Seeds) + 1, players[i / Seeds].Player));
        clock.Stop();

        TextWriter table = TestContext.Out;
        table.WriteLine("seed  player  ended    act  box office");
        for (int i = 0; i < performances.Length; i++)
        {
            Performance played = performances[i];
            table.WriteLine(
                $"{(i % Seeds) + 1,4}  {players[i / Seeds].Name,-6}  {(played.Ended == Phase.Ovation ? "ovation" : "closed"),-7}  {played.Act,3}  {Number(played.BoxOffice),10}");
        }

        for (int player = 0; player < players.Length; player++)
        {
            var mine = performances.Skip(player * Seeds).Take(Seeds).ToList();
            table.WriteLine();
            table.WriteLine($"{players[player].Name}: the acts' averages over the performances that played the act");
            table.WriteLine("act  played  entries  share  band 0/<1/1/2  dropped  in reach  walked to  fell  box office  cards taken");
            for (int act = 0; act < mine.Max(played => played.Acts.Count); act++)
            {
                var acts = mine.Where(played => played.Acts.Count > act).Select(played => played.Acts[act]).ToList();
                string bands = string.Join("/", Enum.GetValues<ApplauseBand>().Select(band => acts.Count(a => a.Band == band)));
                string cards = string.Join(" ", ScriptedPlayers.CardOrder
                    .Select(card => (Card: card, Taken: acts.Count(a => a.Card == card)))
                    .Where(taken => taken.Taken > 0)
                    .Select(taken => $"{taken.Card}:{taken.Taken}"));
                table.WriteLine(
                    $"{act + 1,3}  {acts.Count,6}  {Number(acts.Average(a => a.Entries)),7}  {Number(acts.Average(a => a.Share), "0.00"),5}  {bands,13}  {Number(acts.Average(a => a.Dropped)),7}  {Number(acts.Average(a => a.FellInReach)),8}  {Number(acts.Average(a => a.WalkedTo)),9}  {acts.Count(a => a.Fell),4}  {Number(acts.Average(a => a.BoxOffice)),10}  {cards}");
            }
        }

        table.WriteLine();
        table.WriteLine(
            $"The guard (decision 22, at least 16 of {Seeds} each): the orbit player lost the box office by the end of act six in "
            + $"{performances.Take(Seeds).Count(played => played.Ended == Phase.Closed && played.Act <= 6)} of {Seeds}; "
            + $"the doors player finished act ten in {performances.Skip(Seeds).Count(played => played.Ended == Phase.Ovation)} of {Seeds}.");
        table.WriteLine($"{performances.Length} performances in {Number(clock.Elapsed.TotalSeconds)} s.");

        static string Number(double value, string format = "0.0") => value.ToString(format, CultureInfo.InvariantCulture);
    }
}
