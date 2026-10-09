using System.Text.Json;

namespace Understudies.Core.Tests;

/// <summary>The nights' overlays (plan T51): nights.json, and the tuning a night makes of tuning.json.</summary>
public class NightTests
{
    /// <summary>Seeds enough for a draw that could go wrong to go wrong in one of them.</summary>
    private static readonly ulong[] Seeds = [1, 2, 3, 4, 5];

    private const int Critic = 0;
    private const int Stagehand = 1;
    private const int Scalper = 4;

    [Test]
    public void Parse_TheCommittedFile_HasNightsOneTwoThreeAndTenAsTheLadderHasThem()
    {
        IReadOnlyList<Night> nights = CommittedNights.Parse();

        Assert.That(nights.Select(night => night.Number), Is.EqualTo(new[] { 1, 2, 3, 10 }));

        Assert.That(nights[0].ActsInPerformance, Is.EqualTo(5));
        Assert.That(nights[0].BudgetScale, Is.EqualTo(0.6m));
        Assert.That(nights[0].KindsAllowed, Is.EqualTo(new[] { "critic" }));
        Assert.That(nights[0].WaveBurstShare, Is.Zero);

        Assert.That(nights[1].ActsInPerformance, Is.EqualTo(7));
        Assert.That(nights[1].BudgetScale, Is.EqualTo(0.7m));
        Assert.That(nights[1].KindsAllowed, Is.EqualTo(new[] { "critic", "stagehand" }));
        Assert.That(nights[1].WaveBurstShare, Is.Zero);

        // Night 3 (plan T58): the document's row, and the one night whose poster has a note.
        Assert.That(nights[2].ActsInPerformance, Is.EqualTo(7));
        Assert.That(nights[2].BudgetScale, Is.EqualTo(0.75m));
        Assert.That(nights[2].KindsAllowed, Is.EqualTo(new[] { "critic", "stagehand", "scalper" }));
        Assert.That(nights[2].WaveBurstShare, Is.Zero);
        Assert.That(nights[2].Note, Does.StartWith("The quiet floor: "));
        Assert.That(nights.Where(night => night.Number != 3).Select(night => night.Note), Is.All.Null);

        // The plain night overrides nothing: it has its name, which no rule reads.
        Assert.That(nights[3], Is.EqualTo(new Night(10, null, null, null, null, "Opening night", null, null)));
        Assert.That(
            nights.Select(night => night.Name),
            Is.EqualTo(new[] { "A quiet Tuesday", "The crew arrives", "Touts outside", "Opening night" }));

        // No committed night has a house rule yet (plan T56): the spotlight night is the document's night 8.
        Assert.That(nights.Select(night => night.Rules), Is.All.Null);
    }

    [Test]
    public void Parse_EveryKeyANightMayHave_IsRead()
    {
        Night night = Night.Parse(
            """[{ "night": 3, "actsInPerformance": 4, "budgetScale": 1.25, "kindsAllowed": ["rival"], "waveBurstShare": 0.5, "name": "The tout", "rules": ["spotlight"], "note": "Mind the floor." }]""")
            .Single();

        Assert.That(night.Number, Is.EqualTo(3));
        Assert.That(night.ActsInPerformance, Is.EqualTo(4));
        Assert.That(night.BudgetScale, Is.EqualTo(1.25m));
        Assert.That(night.KindsAllowed, Is.EqualTo(new[] { "rival" }));
        Assert.That(night.WaveBurstShare, Is.EqualTo(0.5f));
        Assert.That(night.Name, Is.EqualTo("The tout"));
        Assert.That(night.Rules, Is.EqualTo(new[] { Night.Spotlight }));
        Assert.That(night.Note, Is.EqualTo("Mind the floor."));
    }

    [Test]
    public void ANightsRules_AreNames_AndChangeNoNumber()
    {
        // A house rule is a name (plan T56), and the first is the view's alone: a night with a rule and nothing
        // else is the plain night still, and a simulation is given nothing that says there is one. No rules and
        // the key left out are both a night without one.
        Tuning plain = CommittedTuning.Parse();
        IReadOnlyList<Night> nights = Night.Parse(
            """[{ "night": 1 }, { "night": 2, "rules": [] }, { "night": 8, "rules": ["spotlight"] }]""");

        Assert.That(nights[0].Has(Night.Spotlight), Is.False);
        Assert.That(nights[1].Has(Night.Spotlight), Is.False);
        Assert.That(nights[2].Has(Night.Spotlight), Is.True);
        Assert.That(Night.Compose(plain, nights, 8), Is.EqualTo(plain));
        Assert.That(Night.RuleNames, Is.EqualTo(new[] { "spotlight" }));
    }

    [Test]
    public void ANightsName_MayBeLeftOut_AndChangesNoNumber()
    {
        // The name is the poster's (plan T55): a night without one is read, and one with a name alone is the
        // plain night still.
        Tuning plain = CommittedTuning.Parse();
        IReadOnlyList<Night> nights = Night.Parse("""[{ "night": 1 }, { "night": 2, "name": "The crew arrives" }]""");

        Assert.That(nights[0].Name, Is.Null);
        Assert.That(Night.Compose(plain, nights, 2), Is.EqualTo(plain));
    }

    [Test]
    public void ANightsNote_MayBeLeftOut_AndChangesNoNumber()
    {
        // The note is the poster's (plan T58), as the name is: words of the night's own that no rule reads.
        Tuning plain = CommittedTuning.Parse();
        IReadOnlyList<Night> nights = Night.Parse("""[{ "night": 1 }, { "night": 3, "note": "The quiet floor is quiet." }]""");

        Assert.That(nights[0].Note, Is.Null);
        Assert.That(nights[1].Note, Is.EqualTo("The quiet floor is quiet."));
        Assert.That(Night.Compose(plain, nights, 3), Is.EqualTo(plain));
    }

    [TestCase("""[{ "night": 1, "doors": 2 }]""", "'doors'", TestName = "an unknown key")]
    [TestCase("""[{ "actsInPerformance": 5 }]""", "missing required properties including: 'night'", TestName = "a night with no number")]
    [TestCase("""[{ "night": 1, "kindsAllowed": [] }]""", "'kindsAllowed' of night 1", TestName = "a night that allows no kind")]
    [TestCase("""[{ "night": 1 }, { "night": 2, "kindsAllowed": ["critic", null] }]""", "'kindsAllowed' of night 2", TestName = "a kind that says null")]
    [TestCase("""[{ "night": 1, "budgetScale": 0.6, "budgetScale": 0.7 }]""", "'budgetScale'", TestName = "a key written twice")]
    [TestCase("""[{ "night": 1 }, { "night": 1 }]""", "night 1", TestName = "a night twice")]
    [TestCase("""[{ "night": 2 }, { "night": 1 }]""", "night 1", TestName = "a night out of order")]
    [TestCase("""[{ "night": 0 }]""", "night 0", TestName = "a night before the first")]
    [TestCase("""[{ "night": 1, "actsInPerformance": 5.5 }]""", "actsInPerformance", TestName = "a number of acts that is not whole")]
    [TestCase("""[{ "night": 1, "name": 1 }]""", "name", TestName = "a name that is not words")]
    [TestCase("""[{ "night": 1, "name": " " }]""", "'name' of night 1", TestName = "a name of no words")]
    [TestCase("""[{ "night": 1, "note": 1 }]""", "note", TestName = "a note that is not words")]
    [TestCase("""[{ "night": 1 }, { "night": 3, "note": "" }]""", "'note' of night 3", TestName = "a note of no words")]
    [TestCase("""[{ "night": 1 }, { "night": 2, "rules": ["spotlight", "limelight"] }]""", "'rules' of night 2 names 'limelight'", TestName = "a rule nobody knows")]
    [TestCase("""[{ "night": 1, "rules": [null] }]""", "'rules' of night 1", TestName = "a rule that says null")]
    [TestCase("""[{ "night": 1, "rules": ["spotlight", "spotlight"] }]""", "'rules' of night 1 names 'spotlight' twice", TestName = "a rule twice")]
    [TestCase("""[{ "night": 1, "rules": "spotlight" }]""", "rules", TestName = "rules that are not a list")]
    [TestCase("""[{ "night": 1 }, null]""", "null", TestName = "a night that says null")]
    [TestCase("null", "null", TestName = "a file that says null")]
    public void Parse_AFileThatIsNotTheNights_IsRefusedWithThePlace(string json, string place)
    {
        Assert.That(() => Night.Parse(json), Throws.TypeOf<JsonException>().With.Message.Contains(place));
    }

    [Test]
    public void Compose_ANightTheFileDoesNotHave_IsRefusedWithItsNumber()
    {
        // Nights 4 to 9 wait for kinds, stages and rules: nobody plays the plain night under their names.
        Assert.That(
            () => Night.Compose(CommittedTuning.Parse(), CommittedNights.Parse(), 4),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Message.Contains("night 4"));
    }

    [Test]
    public void Compose_AKindTheTuningDoesNotHave_IsRefusedWithItsNameAndItsNight()
    {
        IReadOnlyList<Night> nights = Night.Parse("""[{ "night": 4, "kindsAllowed": ["critic", "tout"] }]""");

        Assert.That(
            () => Night.Compose(CommittedTuning.Parse(), nights, 4),
            Throws.TypeOf<JsonException>().With.Message.Contains("'tout'").And.Message.Contains("night 4"));
    }

    [Test]
    public void Compose_EveryNightOfTheCommittedFile_IsATuning()
    {
        // A kind's name is checked against the tuning, so only when a night is composed: a slip in the name of a
        // night nobody plays yet is seen here.
        Tuning plain = CommittedTuning.Parse();
        IReadOnlyList<Night> nights = CommittedNights.Parse();

        Assert.That(nights, Is.Not.Empty);
        foreach (Night night in nights)
        {
            Assert.That(() => Night.Compose(plain, nights, night.Number), Throws.Nothing, $"night {night.Number}");
        }
    }

    [Test]
    public void Compose_ThePlainNight_IsTheCommittedTuningItself()
    {
        // A record's lists are compared as the same list or not, and they are the same: a night replaces only
        // what it overrides.
        Tuning plain = CommittedTuning.Parse();

        Assert.That(Night.Compose(plain, CommittedNights.Parse(), 10), Is.EqualTo(plain));
    }

    [Test]
    public void Compose_ANight_ChangesOnlyWhatItOverrides()
    {
        Tuning plain = CommittedTuning.Parse();
        Tuning night = Night.Compose(plain, CommittedNights.Parse(), 1);

        // Everything night 1 overrides, put back, is the plain tuning: the stage's doors are the same list still.
        Assert.That(
            night with
            {
                ActsInPerformance = plain.ActsInPerformance,
                FirstActBudget = plain.FirstActBudget,
                BudgetGrowthPerAct = plain.BudgetGrowthPerAct,
                BudgetGrowthRise = plain.BudgetGrowthRise,
                EnemyKinds = plain.EnemyKinds,
                WaveBurstShare = plain.WaveBurstShare,
            },
            Is.EqualTo(plain));
    }

    [Test]
    public void Compose_ABudgetScale_ScalesTheFirstBudgetTheStepAndTheRise_AndAHalfIsRoundedUp()
    {
        Tuning plain = CommittedTuning.Parse() with { FirstActBudget = 40, BudgetGrowthPerAct = 5, BudgetGrowthRise = 3 };

        // 40 × 0.7 is 28 and not the 27 a float's 27.999998 would be cut to; 3.5 is 4 and 2.1 is 2.
        Tuning scaled = Night.Compose(plain, [new Night(1, null, 0.7m, null, null, null, null, null)], 1);
        Assert.That(
            (scaled.FirstActBudget, scaled.BudgetGrowthPerAct, scaled.BudgetGrowthRise),
            Is.EqualTo((28, 4, 2)));

        // 2.5 is 3 and 1.5 is 2: a half goes up, never to the even number.
        Tuning halved = Night.Compose(plain, [new Night(1, null, 0.5m, null, null, null, null, null)], 1);
        Assert.That(
            (halved.FirstActBudget, halved.BudgetGrowthPerAct, halved.BudgetGrowthRise),
            Is.EqualTo((20, 3, 2)));
    }

    [Test]
    public void Compose_AKindThatIsNotAllowed_KeepsItsPlaceInTheListAndIsNeverBought()
    {
        Tuning plain = CommittedTuning.Parse();
        Tuning night = CommittedNights.Tuning(2);

        // A planned entry and the view name a kind by its place: the list is as long and in the same order.
        Assert.That(night.EnemyKinds.Select(kind => kind.Name), Is.EqualTo(plain.EnemyKinds.Select(kind => kind.Name)));
        Assert.That(night.EnemyKinds[Critic], Is.EqualTo(plain.EnemyKinds[Critic]));
        Assert.That(night.EnemyKinds[Stagehand], Is.EqualTo(plain.EnemyKinds[Stagehand]));
        foreach (EnemyKind shut in night.EnemyKinds.Skip(2))
        {
            Assert.That((shut.Weight, shut.InAnAct), Is.EqualTo((0, 0)), shut.Name);
        }
    }

    [Test]
    public void Compose_AKindThatIsNotAllowed_LeavesThePlanAsAKindOfALaterActDoes()
    {
        // The rival and the headliner of night 2, shut out by the night, against the same two with a first act
        // the night never reaches: who enters when, by which door and of which kind is the same, entry for entry.
        Tuning night = CommittedNights.Tuning(2);
        Tuning later = night with
        {
            EnemyKinds = [.. CommittedTuning.Parse().EnemyKinds.Select((kind, place) => place < 2 ? kind : kind with { FromAct = 99 })],
        };

        foreach (ulong seed in Seeds)
        {
            Assert.That(Waves.Plan(night, seed), Is.EqualTo(Waves.Plan(later, seed)), $"seed {seed}");
        }
    }

    [Test]
    public void NightOne_IsFiveActsOfCriticsAlone_SixTenthsAsManyInEveryAct_AndNoCrowds()
    {
        Tuning night = CommittedNights.Tuning(1);

        Assert.That(night.ActsInPerformance, Is.EqualTo(5));
        Assert.That(night.WaveBurstShare, Is.Zero);

        // The plain night's 40, 80, 180, 340 and 560, six tenths of each: every kind costs 1, so a budget is a
        // head count.
        int[] budgets = [24, 48, 108, 204, 336];
        foreach (ulong seed in Seeds)
        {
            var show = new Simulation(night, seed);

            Assert.That(show.Plan.Select(act => act.Count), Is.EqualTo(budgets), $"seed {seed}");
            Assert.That(show.Plan.SelectMany(act => act).Select(entry => entry.Kind), Is.All.EqualTo(Critic), $"seed {seed}");
            Assert.That(show.ActEntries, Is.EqualTo(show.Plan[0]));

            // Five acts open two doors by themselves: the third opens in act six, so the night needs no key for it.
            Assert.That(show.Plan.SelectMany(act => act).Select(entry => entry.Door), Is.All.LessThan(2), $"seed {seed}");
        }

        // No crowds: the plan is the one a tuning with no time between crowds has, and not the one with the
        // plain night's share of them.
        Assert.That(Waves.Plan(night, 1), Is.EqualTo(Waves.Plan(night with { WaveBurstTime = 0f }, 1)));
        Assert.That(
            Waves.Plan(night, 1)[4],
            Is.Not.EqualTo(Waves.Plan(night with { WaveBurstShare = CommittedTuning.Parse().WaveBurstShare }, 1)[4]));
    }

    [Test]
    public void NightTwo_IsSevenActs_TheStagehandFromActFour_NoRival_SevenTenthsAsManyInEveryAct()
    {
        Tuning night = CommittedNights.Tuning(2);

        Assert.That(night.ActsInPerformance, Is.EqualTo(7));
        Assert.That(night.WaveBurstShare, Is.Zero);

        // The plain night's 40, 80, 180, 340, 560, 840 and 1180, seven tenths of each.
        int[] budgets = [28, 56, 126, 238, 392, 588, 826];
        foreach (ulong seed in Seeds)
        {
            var show = new Simulation(night, seed);

            Assert.That(show.Plan.Select(act => act.Count), Is.EqualTo(budgets), $"seed {seed}");
            for (int act = 1; act <= 7; act++)
            {
                IEnumerable<int> kinds = show.Plan[act - 1].Select(entry => entry.Kind).Distinct().Order();

                Assert.That(kinds, Is.EqualTo(act < 4 ? [Critic] : new[] { Critic, Stagehand }), $"seed {seed}, act {act}");
            }

            // Seven acts open the third door, in act six.
            Assert.That(show.Plan[6].Select(entry => entry.Door), Does.Contain(2), $"seed {seed}");
        }
    }

    [Test]
    public void NightThree_IsSevenActs_TheScalperFromActThree_TheStagehandFromActFour_ThreeQuartersAsManyInEveryAct()
    {
        Tuning night = CommittedNights.Tuning(3);

        Assert.That(night.ActsInPerformance, Is.EqualTo(7));
        Assert.That(night.WaveBurstShare, Is.Zero);

        // The plain night's 40, 80, 180, 340, 560, 840 and 1180, three quarters of each.
        int[] budgets = [30, 60, 135, 255, 420, 630, 885];
        foreach (ulong seed in Seeds)
        {
            var show = new Simulation(night, seed);

            Assert.That(show.Plan.Select(act => act.Count), Is.EqualTo(budgets), $"seed {seed}");
            for (int act = 1; act <= 7; act++)
            {
                IEnumerable<int> kinds = show.Plan[act - 1].Select(entry => entry.Kind).Distinct().Order();

                // No rival's understudy and no headliner: the night names its kinds.
                Assert.That(
                    kinds,
                    Is.EqualTo(act < 3 ? [Critic] : act < 4 ? [Critic, Scalper] : new[] { Critic, Stagehand, Scalper }),
                    $"seed {seed}, act {act}");
            }

            Assert.That(show.Plan[6].Select(entry => entry.Door), Does.Contain(2), $"seed {seed}");
        }
    }
}
