using System.Numerics;

namespace Understudies.Core.Tests;

public class CardTests
{
    /// <summary>Two seconds: the act of <see cref="Scene"/>.</summary>
    private const int ActTicks = 2 * Simulation.TicksPerSecond;

    /// <summary>Twelve seconds: the program of <see cref="Scene"/>.</summary>
    private const int ProgramTicks = 12 * Simulation.TicksPerSecond;

    /// <summary>In the stage's top edge: the first door, where a critic enters, stands and falls.</summary>
    private static readonly Vector2 Door = new(20f, 0f);

    /// <summary>
    /// The second door, six units to the left along the edge: in range of whoever stands at the first, with a
    /// piece that lies there out of its reach.
    /// </summary>
    private static readonly Vector2 OtherDoor = new(14f, 0f);

    /// <summary>
    /// Right below the first door, as high as the floor lets the magician stand: a piece at the door is in its reach.
    /// </summary>
    private static readonly Vector2 AtTheDoor = new(20f, 0.6f);

    private static readonly MagicianInput Right = new(new Vector2(1f, 0f));

    private static readonly MagicianInput Down = new(new Vector2(0f, 1f));

    /// <summary>A critic that enters by the first door on the first tick of its act.</summary>
    private static readonly PlannedEntry AtOnce = new(Tick: 0, Door: 0, Kind: 0);

    /// <summary>
    /// A critic that enters by the first door a second into its act: a magician that left the door after four
    /// ticks, and its understudy in every later act, is fourteen units away by then.
    /// </summary>
    private static readonly PlannedEntry Later = new(Tick: Simulation.TicksPerSecond, Door: 0, Kind: 0);

    /// <summary>
    /// A performance of six acts of two seconds whose critics stand still and fall to one card. The doors have
    /// no width and a critic no speed, so each enters exactly at its door and stays there, and no critic turns on
    /// the magician. The magician stands on <see cref="AtTheDoor"/>, walks a quarter of a unit a tick, throws every
    /// six ticks at what is within nine units, and its card flies a unit a tick: a critic that enters by the first
    /// door on the first tick falls on the third, and on the fourth its piece is picked up, if the magician still
    /// stands there. That one piece for the act's one critic is the second band. A Vanish is ready again half a
    /// second later and leaves no cloud. A card gives a round number, and no place of an offer is the chorus card.
    /// The stage has no back wall and the curtain no length: these tests count their ticks from the first tick of
    /// an act.
    /// </summary>
    private Tuning Scene { get; } = CommittedTuning.Parse().WithCritic(
        critic => critic with { Speed = 0f, Radius = 0.5f, HitPoints = 1f }) with
    {
        CurtainTime = 0f,
        ActLength = 2f,
        ActsInPerformance = 6,
        StageFloorTop = 0f,
        StageDoors = [new StageDoor(Door, 1), new StageDoor(OtherDoor, 1)],
        StageDoorWidth = 0f,
        MagicianMark = AtTheDoor,
        MagicianSpeed = 15f,
        MagicianRadius = 0.6f,
        CriticTurnRadius = 0f,
        VanishDistance = 6f,
        VanishCooldown = 0.5f,
        VanishCloudTime = 0f,
        ThrowRange = 9f,
        ThrowCooldown = 0.1f,
        ThrownCardSpeed = 60f,
        ThrownCardDamage = 1f,
        ApplauseTime = 4f,
        ApplausePickUpReach = 0.4f,
        ApplauseFirstThreshold = 0.15f,
        ApplauseSecondThreshold = 0.35f,
        ProgramTime = 12f,
        CardDamage = 1f,
        CardAttackSpeed = 0.2f,
        CardRange = 1.5f,
        CardVanishCooldown = 0.25f,
        CardChorusDamage = 1f,
        CardChorusChance = 0f,
    };

    // The magician picks up the piece of the critic at its own door and of no other, so the act's share is one
    // over its critics: of one critic the whole, of four a quarter, of ten a tenth. A magician that throws at
    // nobody picks up nothing.
    [TestCase(1, 0f, ApplauseBand.None, 0, TestName = "Step_AnActThatPickedUpNothing_HasNoProgram")]
    [TestCase(10, 9f, ApplauseBand.UnderTheFirst, 1, TestName = "Step_AnActUnderTheFirstThreshold_IsOfferedOneCard")]
    [TestCase(4, 9f, ApplauseBand.First, 2, TestName = "Step_AnActOfTheFirstBand_IsOfferedTwoCards")]
    [TestCase(1, 9f, ApplauseBand.Second, 3, TestName = "Step_AnActOfTheSecondBand_IsOfferedThreeCards")]
    public void Step_TheActIsOver_TheProgramOffersWhatItsBandPaysFor(int critics, float range, ApplauseBand band, int cards)
    {
        Simulation simulation = AfterAFirstActOf(critics, Scene with { ThrowRange = range });

        Assert.That(simulation.ActApplauseBand, Is.EqualTo(band));
        Assert.That(simulation.Offer, Has.Count.EqualTo(cards));
        if (cards == 0)
        {
            // No applause, no program: the stage is between two acts at once, as it was before there were cards.
            Assert.That(simulation.Phase, Is.EqualTo(Phase.BetweenActs));
            Assert.That(simulation.ProgramTicksLeft, Is.Zero);
        }
        else
        {
            Assert.That(simulation.Phase, Is.EqualTo(Phase.Program));
            Assert.That(simulation.ProgramTicksLeft, Is.EqualTo(ProgramTicks));
        }
    }

    [Test]
    public void Offer_WhileAnActIsPlayed_IsEmpty()
    {
        var simulation = new Simulation(Scene, seed: 1, [[AtOnce]]);

        Run(simulation, ActTicks - 1);

        // The second band is earned already; the offer is made when the act is over.
        Assert.That(simulation.ActApplauseBand, Is.EqualTo(ApplauseBand.Second));
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.Offer, Is.Empty);
    }

    // The chance of the chorus card is as high as it can be, and still no place under the second band is one.
    [TestCase(10, 1)]
    [TestCase(4, 2)]
    public void Offer_UnderTheSecondBand_IsOfDifferentSelfCardsOnly(int critics, int cards)
    {
        var offered = new HashSet<Card>();
        for (ulong seed = 1; seed <= 100; seed++)
        {
            Simulation simulation = AfterAFirstActOf(critics, Scene with { CardChorusChance = 1f }, seed);

            Assert.That(simulation.Offer, Has.Count.EqualTo(cards));
            Assert.That(simulation.Offer, Is.Unique);
            Assert.That(simulation.Offer, Has.None.EqualTo(Card.ChorusDamage));
            offered.UnionWith(simulation.Offer);
        }

        // Over a hundred shows every self card has been offered.
        Assert.That(offered, Has.Count.EqualTo(5));
    }

    // With no chance there is never a chorus card, and with every chance exactly one, in the leftmost place: a
    // place after it has none. With half a chance some offers have one and some none.
    [TestCase(0f, 0, 0)]
    [TestCase(1f, 100, 100)]
    [TestCase(0.5f, 60, 99)]
    public void Offer_OfTheSecondBand_IsOfThreeDifferentCards_AtMostOneOfThemTheChorusCard(
        float chance, int atLeast, int atMost)
    {
        int withAChorusCard = 0;
        for (ulong seed = 1; seed <= 100; seed++)
        {
            Simulation simulation = AfterAFirstActOf(1, Scene with { CardChorusChance = chance }, seed);

            Assert.That(simulation.Offer, Has.Count.EqualTo(3));
            Assert.That(simulation.Offer, Is.Unique);
            withAChorusCard += simulation.Offer.Count(card => card == Card.ChorusDamage);
        }

        // Each of three places with half a chance: seven offers in eight have one.
        Assert.That(withAChorusCard, Is.InRange(atLeast, atMost));
    }

    [Test]
    public void Offer_TheSameSeedAndTheSamePlay_GiveTheSameOffers()
    {
        // Two programs each: what the second offers follows from the seed as what the first does.
        List<Card> Offers(ulong seed)
        {
            Simulation simulation = AfterAFirstActOf(1, Scene, seed);
            List<Card> offers = [.. simulation.Offer];
            simulation.Pick(0);
            simulation.GoOn();
            Run(simulation, ActTicks);
            offers.AddRange(simulation.Offer);
            return offers;
        }

        List<Card> once = Offers(seed: 7);
        List<Card> again = Offers(seed: 7);

        Assert.That(once, Has.Count.EqualTo(6));
        Assert.That(again, Is.EqualTo(once));
        Assert.That(Enumerable.Range(8, 5).Select(seed => Offers((ulong)seed)), Has.Some.Not.EqualTo(once));
    }

    [Test]
    public void Step_InTheProgram_OnlyTheCountdownRuns()
    {
        // A second critic enters by the other door three ticks before the act is over: it is on the stage, and
        // the card thrown at it in the air, when the program comes.
        var simulation = new Simulation(Scene, seed: 1, [[AtOnce, new PlannedEntry(ActTicks - 3, Door: 1, Kind: 0)], []]);
        Run(simulation, ActTicks);
        List<Vector2> critics = [.. simulation.Critics.Select(critic => critic.Position)];
        List<Vector2> cards = [.. simulation.ThrownCards.Select(card => card.Position)];
        Assert.That(critics, Is.Not.Empty);
        Assert.That(cards, Is.Not.Empty);

        simulation.Step(Right with { Vanish = true });

        Assert.That(simulation.Phase, Is.EqualTo(Phase.Program));
        Assert.That(simulation.ProgramTicksLeft, Is.EqualTo(ProgramTicks - 1));
        Assert.That(simulation.Events, Is.Empty);
        Assert.That(simulation.MagicianPosition, Is.EqualTo(AtTheDoor));
        Assert.That(simulation.Critics.Select(critic => critic.Position), Is.EqualTo(critics));
        Assert.That(simulation.ThrownCards.Select(card => card.Position), Is.EqualTo(cards));
        Assert.That(simulation.Offer, Has.Count.EqualTo(3));
    }

    [Test]
    public void GoOn_InTheProgram_IsRefused()
    {
        Simulation simulation = AfterAFirstActOf(1, Scene);

        simulation.GoOn();

        Assert.That(simulation.Phase, Is.EqualTo(Phase.Program));
        Assert.That(simulation.Act, Is.EqualTo(1));
    }

    [Test]
    public void Pick_InTheProgram_TakesTheCard_AndTheStageIsBetweenTwoActs()
    {
        Simulation simulation = AfterAFirstActOf(1, Scene);
        Assert.That(simulation.MagicianCards, Is.EqualTo(default(SelfCards)));

        simulation.Pick(2);

        // A card of some kind, once.
        Assert.That(simulation.MagicianCards, Is.Not.EqualTo(default(SelfCards)));
        Assert.That(simulation.Phase, Is.EqualTo(Phase.BetweenActs));
        Assert.That(simulation.Offer, Is.Empty);
        Assert.That(simulation.ProgramTicksLeft, Is.Zero);

        // What the act earned is still there to be read, until going on.
        Assert.That(simulation.ActApplauseBand, Is.EqualTo(ApplauseBand.Second));

        simulation.GoOn();
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.Act, Is.EqualTo(2));
    }

    [TestCase(-1)]
    [TestCase(2)]
    public void Pick_APlaceTheOfferDoesNotHave_IsRefused(int place)
    {
        // An offer of two: its places are 0 and 1.
        Simulation simulation = AfterAFirstActOf(4, Scene);

        simulation.Pick(place);

        Assert.That(simulation.Phase, Is.EqualTo(Phase.Program));
        Assert.That(simulation.Offer, Has.Count.EqualTo(2));
        Assert.That(simulation.MagicianCards, Is.EqualTo(default(SelfCards)));
    }

    [Test]
    public void Pick_OutsideTheProgram_IsRefused()
    {
        var simulation = new Simulation(Scene, seed: 1, [[AtOnce]]);

        // In an act.
        simulation.Pick(0);
        Assert.That(simulation.MagicianCards, Is.EqualTo(default(SelfCards)));

        // Between two acts, when a card has been taken: a program gives one card.
        Run(simulation, ActTicks);
        simulation.Pick(0);
        SelfCards taken = simulation.MagicianCards;
        simulation.Pick(0);
        simulation.Pick(1);
        Assert.That(simulation.MagicianCards, Is.EqualTo(taken));
        Assert.That(simulation.ChorusCards, Is.Zero);
    }

    [Test]
    public void Step_TheProgramsTimeRunsOut_TheLeftmostCardIsTaken()
    {
        Simulation waited = AfterAFirstActOf(1, Scene);
        Simulation picked = AfterAFirstActOf(1, Scene);
        Simulation pickedAnother = AfterAFirstActOf(1, Scene);
        picked.Pick(0);
        pickedAnother.Pick(1);

        // On the last tick of the twelve seconds the choice is still open.
        Run(waited, ProgramTicks - 1);
        Assert.That(waited.Phase, Is.EqualTo(Phase.Program));
        Assert.That(waited.ProgramTicksLeft, Is.EqualTo(1));
        Assert.That(waited.MagicianCards, Is.EqualTo(default(SelfCards)));

        waited.Step(default);

        Assert.That(waited.Phase, Is.EqualTo(Phase.BetweenActs));
        Assert.That(waited.Offer, Is.Empty);
        Assert.That(waited.MagicianCards, Is.EqualTo(picked.MagicianCards));
        Assert.That(waited.MagicianCards, Is.Not.EqualTo(pickedAnother.MagicianCards));

        // And nothing else came of the wait: the show goes on as one whose leftmost card was picked at once.
        Assert.That(waited.ComputeStateHash(), Is.EqualTo(picked.ComputeStateHash()));
    }

    [Test]
    public void Step_TheLastActIsOver_NothingIsOffered()
    {
        Simulation simulation = AfterAFirstActOf(1, Scene with { ActsInPerformance = 1 });

        Assert.That(simulation.ActApplauseBand, Is.EqualTo(ApplauseBand.Second));
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Ovation));
        Assert.That(simulation.Offer, Is.Empty);
    }

    [Test]
    public void Step_TheBoxOfficeFallsOnTheLastTickOfAnActThatPickedUpApplause_NothingIsOffered()
    {
        // The box office stands against the second door with one hit point. A critic enters there on the tick
        // before the act's last, touching it, and strikes on the last: the show closes with the act's applause
        // picked up and another act to come.
        Tuning tuning = Scene with
        {
            BoxOfficePosition = OtherDoor + new Vector2(0f, 2f),
            BoxOfficeSize = 4f,
            BoxOfficeHitPoints = 1f,
            CriticStrikeDamage = 1f,
        };
        var simulation = new Simulation(tuning, seed: 1, [[AtOnce, new PlannedEntry(ActTicks - 2, Door: 1, Kind: 0)], []]);
        Run(simulation, ActTicks - 1);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.ActApplauseBand, Is.EqualTo(ApplauseBand.Second));

        simulation.Step(default);

        Assert.That(simulation.ActTicksLeft, Is.Zero);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Closed));
        Assert.That(simulation.Offer, Is.Empty);
        Assert.That(simulation.ProgramTicksLeft, Is.Zero);

        // And there is nothing to take.
        simulation.Pick(0);
        Assert.That(simulation.MagicianCards, Is.EqualTo(default(SelfCards)));
    }

    [Test]
    public void Step_ACardInTheAirWhenTheTuningIsReloaded_HurtsAsItWasThrown()
    {
        // A critic of two hit points, and a card thrown with a damage card that adds one. While the card is in
        // the air the tuning is given a damage card that adds nothing: the card still takes two, and the critic
        // falls to it.
        Simulation simulation = ShowThatTook(Scene, [Later], Card.Damage);
        simulation.Tuning = simulation.Tuning.WithCritic(critic => critic with { HitPoints = 2f });
        simulation.GoOn();
        Run(simulation, ticks: 62);
        Assert.That(simulation.ThrownCards, Has.Count.EqualTo(1));
        Assert.That(simulation.Critics, Has.Count.EqualTo(1));

        simulation.Tuning = simulation.Tuning with { CardDamage = 0f };
        simulation.Step(default);

        Assert.That(simulation.Events.Select(happened => happened.Kind), Does.Contain(TickEventKind.Kill));
        Assert.That(simulation.Critics, Is.Empty);
    }

    [Test]
    public void Pick_ASelfCard_ChangesTheMagicianAndTheNextRecording_AndNoOlderUnderstudy()
    {
        // The first act: the piece, then down to ten units below the door, out of a throw's nine and within the
        // ten and a half of a throw with one range card. The second, with that card: the piece, two units to the
        // right and as far down, which is as far from the door within a third of a unit. In the third the magician
        // walks away to the right, and when a critic that outlasts the act enters, a second and two thirds into
        // it, both understudies have long stood where their acts ended and the magician is 25 units off.
        ulong seed = SeedWhosePrograms([Card.Range]);
        var simulation = new Simulation(Scene, seed, [[AtOnce], [AtOnce], [new PlannedEntry(100, Door: 0, Kind: 0)]]);
        PlayTheAct(simulation, tick => tick is >= 4 and < 42 ? Down : default);
        Take(simulation, Card.Range);
        Assert.That(simulation.MagicianCards, Is.EqualTo(new SelfCards(Range: 1)));

        simulation.GoOn();
        Assert.That(simulation.Understudies[0].Cards, Is.EqualTo(default(SelfCards)));
        PlayTheAct(simulation, tick => tick switch
        {
            < 4 => default,
            < 12 => Right,
            < 50 => Down,
            _ => default,
        });

        // Whatever the magician takes now is not the second act's: that act was played with one range card.
        simulation.Pick(0);
        simulation.Tuning = Scene.WithCritic(critic => critic with { HitPoints = 1000f });
        simulation.GoOn();
        Assert.That(simulation.MagicianCards, Is.Not.EqualTo(new SelfCards(Range: 1)));
        Assert.That(simulation.Understudies.Select(understudy => understudy.Cards), Is.EqualTo(new[]
        {
            default(SelfCards),
            new SelfCards(Range: 1),
        }));

        List<(int Tick, TickEvent Event)> events = PlayTheAct(simulation, _ => Right);

        // Only the second act's understudy reaches the critic, and its cards fly that far.
        Vector2 second = simulation.Understudies[1].Position;
        Assert.That(simulation.Understudies[0].Position, Is.EqualTo(new Vector2(20f, 10.1f)).Using<Vector2>(Near));
        Assert.That(second, Is.EqualTo(new Vector2(22f, 10.1f)).Using<Vector2>(Near));
        Assert.That(Of(events, TickEventKind.Throw), Is.Not.Empty);
        Assert.That(Of(events, TickEventKind.Throw).Select(thrown => thrown.Event.Position), Is.All.EqualTo(second));
        Assert.That(Of(events, TickEventKind.Hit), Is.Not.Empty);
    }

    // A critic of three hit points and a card that takes one: three cards fell it, and two when a damage card
    // adds one. A damage card that adds nothing changes nothing.
    [TestCase(1f, 1)]
    [TestCase(0f, 2)]
    public void Pick_TheDamageCard_TheMagiciansCardsHurtMore(float cardDamage, int hitsBeforeTheFall)
    {
        Simulation simulation = ShowThatTook(Scene with { CardDamage = cardDamage }, [Later], Card.Damage);
        simulation.Tuning = simulation.Tuning.WithCritic(critic => critic with { HitPoints = 3f });
        simulation.GoOn();

        List<(int Tick, TickEvent Event)> events = PlayTheAct(simulation);

        Assert.That(Of(events, TickEventKind.Kill), Has.Count.EqualTo(1));
        Assert.That(Of(events, TickEventKind.Hit), Has.Count.EqualTo(hitsBeforeTheFall));
    }

    [Test]
    public void Pick_TheSameCardTwice_AddsTwice()
    {
        // One and two more: a critic of three hit points falls to the first card.
        Simulation simulation = ShowThatTook(Scene, [Later], Card.Damage, Card.Damage);
        Assert.That(simulation.MagicianCards, Is.EqualTo(new SelfCards(Damage: 2)));
        simulation.Tuning = simulation.Tuning.WithCritic(critic => critic with { HitPoints = 3f });
        simulation.GoOn();

        List<(int Tick, TickEvent Event)> events = PlayTheAct(simulation);

        Assert.That(Of(events, TickEventKind.Kill), Has.Count.EqualTo(1));
        Assert.That(Of(events, TickEventKind.Hit), Is.Empty);
    }

    // Six ticks from throw to throw; a fifth more often is five, and two fifths four and a bit. Five cards are
    // twice as often, three ticks: a cooldown that lost a fifth of itself with every card would be none by then,
    // a throw on every tick, and a rate that grew by a fifth of itself with every card would make it two and a
    // half times as often, two ticks.
    [TestCase(0, 6)]
    [TestCase(1, 5)]
    [TestCase(2, 4)]
    [TestCase(5, 3)]
    public void Pick_TheAttackSpeedCard_TheMagicianThrowsSooner(int cards, int ticksBetweenThrows)
    {
        Simulation simulation = ShowThatTook(
            Scene with { CardAttackSpeed = cards == 0 ? 0f : 0.2f },
            [Later],
            [.. Enumerable.Repeat(Card.AttackSpeed, Math.Max(1, cards))]);
        simulation.Tuning = simulation.Tuning.WithCritic(critic => critic with { HitPoints = 1000f });
        simulation.GoOn();

        List<int> throws = [.. Of(PlayTheAct(simulation), TickEventKind.Throw).Select(thrown => thrown.Tick)];

        Assert.That(throws, Has.Count.GreaterThan(2));
        Assert.That(throws.Zip(throws.Skip(1), (one, next) => next - one), Is.All.EqualTo(ticksBetweenThrows));
    }

    // Ten units from the critic and a throw of nine: with a card that adds one and a half the magician throws,
    // and its cards get there.
    [TestCase(1.5f, true)]
    [TestCase(0f, false)]
    public void Pick_TheRangeCard_TheMagiciansThrowReachesFurther(float cardRange, bool reaches)
    {
        Simulation simulation = ShowThatTook(Scene with { CardRange = cardRange }, [Later], Card.Range);
        simulation.Tuning = simulation.Tuning.WithCritic(critic => critic with { HitPoints = 1000f }) with
        {
            MagicianMark = Door + new Vector2(0f, 10f),
        };
        simulation.GoOn();

        List<(int Tick, TickEvent Event)> events = PlayTheAct(simulation);

        Assert.That(Of(events, TickEventKind.Throw).Count > 0, Is.EqualTo(reaches));
        Assert.That(Of(events, TickEventKind.Hit).Count > 0, Is.EqualTo(reaches));
    }

    // Half a second from Vanish to Vanish, thirty ticks; a quarter sooner is twenty-four. Five cards are two and
    // a quarter times as soon, thirteen ticks and a bit: a cooldown that lost a quarter of itself with every card
    // would be none by the fourth, a Vanish on every tick, and a rate that grew by a quarter of itself with every
    // card would make it ten ticks.
    [TestCase(1, 0.25f, 24)]
    [TestCase(1, 0f, 30)]
    [TestCase(5, 0.25f, 13)]
    public void Pick_TheVanishCooldownCard_TheVanishComesBackSooner(
        int cards, float cardVanishCooldown, int ticksBetweenVanishes)
    {
        Simulation simulation = ShowThatTook(
            Scene with { CardVanishCooldown = cardVanishCooldown },
            [],
            [.. Enumerable.Repeat(Card.VanishCooldown, cards)]);
        simulation.GoOn();
        var vanish = new MagicianInput(Vector2.Zero, Vanish: true);

        // The bar is whole on the tick of a Vanish, whatever the cooldown's length.
        simulation.Step(vanish);
        Assert.That(simulation.VanishCooldownLeft, Is.EqualTo(1f));
        List<int> vanishes = [.. Of(PlayTheAct(simulation, _ => vanish), TickEventKind.Vanish).Select(v => v.Tick)];

        Assert.That(vanishes.Take(2), Is.EqualTo(new[] { ticksBetweenVanishes, 2 * ticksBetweenVanishes }));
    }

    [Test]
    public void Pick_OneMoreCard_EachThrowSendsASecondCardAtTheNextNearest()
    {
        // Two critics a second into the act, one at each door, and a throw a second: without the card the one
        // throw of the act fells the nearer critic alone.
        PlannedEntry[] oneAtEachDoor = [Later, Later with { Door = 1 }];
        Simulation simulation = ShowThatTook(Scene, oneAtEachDoor, Card.OneMoreCard);
        simulation.Tuning = simulation.Tuning with { ThrowCooldown = 1f };
        simulation.GoOn();

        List<(int Tick, TickEvent Event)> events = PlayTheAct(simulation);

        // Both on one tick, from where the magician stands, and each fells its own.
        Assert.That(Of(events, TickEventKind.Throw).Select(thrown => thrown.Tick), Is.EqualTo(new[] { 61, 61 }));
        Assert.That(
            Of(events, TickEventKind.Kill).Select(kill => kill.Event.Position),
            Is.EqualTo(new[] { Door, OtherDoor }));
        Assert.That(simulation.Critics, Is.Empty);
    }

    [Test]
    public void Pick_OneMoreCard_WithOneCriticInRange_OneCardIsThrown()
    {
        Simulation simulation = ShowThatTook(Scene, [Later], Card.OneMoreCard);
        simulation.Tuning = simulation.Tuning with { ThrowCooldown = 1f };
        simulation.GoOn();

        List<(int Tick, TickEvent Event)> events = PlayTheAct(simulation);

        Assert.That(Of(events, TickEventKind.Throw), Has.Count.EqualTo(1));
    }

    [Test]
    public void Pick_TheChorusCard_EveryUnderstudysCardsHurtMore()
    {
        // The first act's magician stands at the door to the end, and takes the chorus card. The second act has
        // no critic, and no program: its magician stands there too. In the third the magician walks away, and a
        // critic of four hit points enters a second in, in range of the two understudies alone, which throw on
        // one tick. A card of an understudy takes one, and two with the chorus card.
        var simulation = new Simulation(Scene with { CardChorusChance = 1f }, seed: 1, [[AtOnce], [], [Later]]);
        PlayTheAct(simulation);
        Take(simulation, Card.ChorusDamage);
        Assert.That(simulation.ChorusCards, Is.EqualTo(1));
        Assert.That(simulation.MagicianCards, Is.EqualTo(default(SelfCards)));
        simulation.GoOn();
        PlayTheAct(simulation);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.BetweenActs));
        simulation.Tuning = simulation.Tuning.WithCritic(critic => critic with { HitPoints = 4f });
        simulation.GoOn();

        List<(int Tick, TickEvent Event)> events = PlayTheAct(simulation, _ => Right);

        // The understudy of the act before the card was taken and the one of the act after it: the first of
        // their two cards hurts and the second fells. Had one of them thrown a card of one, a third was needed.
        Assert.That(Of(events, TickEventKind.Throw).Take(2).Select(thrown => thrown.Event.Position), Is.All.EqualTo(AtTheDoor));
        Assert.That(Of(events, TickEventKind.Hit), Has.Count.EqualTo(1));
        Assert.That(Of(events, TickEventKind.Kill), Has.Count.EqualTo(1));
    }

    [Test]
    public void Pick_TheChorusCard_TheMagiciansOwnCardsHurtAsBefore()
    {
        // A critic of two hit points and the magician alone in range of it: two cards of one, as without the
        // chorus card.
        Simulation simulation = ShowThatTook(Scene with { CardChorusChance = 1f }, [Later], Card.ChorusDamage);
        simulation.Tuning = simulation.Tuning.WithCritic(critic => critic with { HitPoints = 2f });
        simulation.GoOn();

        List<(int Tick, TickEvent Event)> events = PlayTheAct(simulation);

        Assert.That(Of(events, TickEventKind.Hit), Has.Count.EqualTo(1));
        Assert.That(Of(events, TickEventKind.Kill), Has.Count.EqualTo(1));
    }

    [Test]
    public void Pick_TheChorusCard_AddsToAnUnderstudysOwnDamageCard()
    {
        // The first act's program gives a damage card; the second act, played at the door to its end with that
        // card, gives the chorus card. In the third the magician walks away and a critic of five hit points
        // enters: the first act's understudy takes two with a card and the second's three, the five between
        // them.
        ulong seed = SeedWhosePrograms([Card.Damage]);
        var simulation = new Simulation(Scene, seed, [[AtOnce], [AtOnce], [Later]]);
        PlayTheAct(simulation);
        Take(simulation, Card.Damage);
        simulation.Tuning = Scene with { CardChorusChance = 1f };
        simulation.GoOn();
        PlayTheAct(simulation);
        Take(simulation, Card.ChorusDamage);
        simulation.Tuning = simulation.Tuning.WithCritic(critic => critic with { HitPoints = 5f });
        simulation.GoOn();

        List<(int Tick, TickEvent Event)> events = PlayTheAct(simulation, _ => Right);

        Assert.That(Of(events, TickEventKind.Hit), Has.Count.EqualTo(1));
        Assert.That(Of(events, TickEventKind.Kill), Has.Count.EqualTo(1));
    }

    [Test]
    public void ComputeStateHash_TwoProgramsWithTwoTimesLeft_AreTwoHashes()
    {
        Simulation one = AfterAFirstActOf(1, Scene);
        Simulation other = AfterAFirstActOf(1, Scene);

        other.Step(default);

        Assert.That(other.ComputeStateHash(), Is.Not.EqualTo(one.ComputeStateHash()));
    }

    [Test]
    public void ComputeStateHash_TwoMagiciansWithTwoCards_AreTwoHashes()
    {
        Simulation one = AfterAFirstActOf(1, Scene);
        Simulation other = AfterAFirstActOf(1, Scene);

        one.Pick(0);
        other.Pick(1);

        Assert.That(other.ComputeStateHash(), Is.Not.EqualTo(one.ComputeStateHash()));
    }

    [Test]
    public void ComputeStateHash_TheSameCardsTakenInTwoOrders_AreTwoHashes_ForTheActsWerePlayedWithOthers()
    {
        // Two cards that leave nothing behind in an act played at the door: a longer throw, and a Vanish nobody
        // asks for. Both shows are offered both, twice, and take them in two orders.
        Card[] both = [Card.Range, Card.VanishCooldown];
        ulong seed = SeedWhosePrograms(both, both);
        Simulation Show(Card first, Card second)
        {
            var simulation = new Simulation(Scene, seed, [[AtOnce], [AtOnce]]);
            PlayTheAct(simulation);
            Take(simulation, first);
            simulation.GoOn();
            PlayTheAct(simulation);
            Take(simulation, second);
            return simulation;
        }

        Simulation one = Show(Card.Range, Card.VanishCooldown);
        Simulation other = Show(Card.VanishCooldown, Card.Range);

        // The magicians are alike now. The recording of the second act, the next understudy, is not.
        Assert.That(other.MagicianCards, Is.EqualTo(one.MagicianCards));
        Assert.That(other.ComputeStateHash(), Is.Not.EqualTo(one.ComputeStateHash()));

        // Nor are the two understudies of that act.
        one.GoOn();
        other.GoOn();
        Assert.That(other.Understudies[1].Cards, Is.Not.EqualTo(one.Understudies[1].Cards));
        Assert.That(other.ComputeStateHash(), Is.Not.EqualTo(one.ComputeStateHash()));
    }

    [Test]
    public void ComputeStateHash_TwoCardsInTheAirThatHurtUnlike_AreTwoHashes()
    {
        // A critic that outlasts the act enters a second into it, and the magician throws on the next tick: its
        // first card is in the air, and has not flown yet.
        Simulation Show(float cardDamage)
        {
            Simulation simulation = ShowThatTook(Scene with { CardDamage = cardDamage }, [Later], Card.Damage);
            simulation.Tuning = simulation.Tuning.WithCritic(critic => critic with { HitPoints = 1000f });
            simulation.GoOn();
            Run(simulation, ticks: 62);
            Assert.That(simulation.ThrownCards, Has.Count.EqualTo(1));
            return simulation;
        }

        Assert.That(Show(cardDamage: 1f).ComputeStateHash(), Is.Not.EqualTo(Show(cardDamage: 2f).ComputeStateHash()));
    }

    [Test]
    public void ComputeStateHash_TwoProgramsThatOfferUnlike_AreTwoHashes()
    {
        // The same act, and a program of three cards in both: one with a chorus card and one without. The two
        // differ in what is offered and in what the program's generator has left, which go together: no play
        // has one without the other.
        Simulation one = AfterAFirstActOf(1, Scene);
        Simulation other = AfterAFirstActOf(1, Scene with { CardChorusChance = 1f });

        Assert.That(other.Offer, Is.Not.EqualTo(one.Offer));
        Assert.That(other.ComputeStateHash(), Is.Not.EqualTo(one.ComputeStateHash()));
    }

    [Test]
    public void ComputeStateHash_TwoProgramsThatDrewUnlike_AreTwoHashes_WhenTheSameCardWasTaken()
    {
        // One seed and one act, a quarter of whose critics' pieces were picked up, under two first thresholds:
        // one show is offered two cards and the other one. The leftmost is the first drawn in both, and both take
        // it. Nothing tells the two apart now but what the program's generator has left: the next program of one
        // will not offer what the other's does.
        Simulation one = AfterAFirstActOf(4, Scene with { ApplauseFirstThreshold = 0.15f });
        Simulation other = AfterAFirstActOf(4, Scene with { ApplauseFirstThreshold = 0.3f });
        Assert.That((one.Offer.Count, other.Offer.Count), Is.EqualTo((2, 1)));
        Assert.That(other.Offer[0], Is.EqualTo(one.Offer[0]));

        one.Pick(0);
        other.Pick(0);

        Assert.That(other.MagicianCards, Is.EqualTo(one.MagicianCards));
        Assert.That(other.ComputeStateHash(), Is.Not.EqualTo(one.ComputeStateHash()));
    }

    /// <summary>Two places are one when they are within a hundredth of a unit: a walk is a sum of floats.</summary>
    private static bool Near(Vector2 one, Vector2 other) => Vector2.Distance(one, other) < 0.01f;

    private static List<(int Tick, TickEvent Event)> Of(List<(int Tick, TickEvent Event)> events, TickEventKind kind) =>
        [.. events.Where(happened => happened.Event.Kind == kind)];

    private static void Run(Simulation simulation, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(default);
        }
    }

    /// <summary>
    /// Plays what is left of the act by <paramref name="script"/>, which is asked for each tick of the act by its
    /// number, the first being 0; with no script the magician stands. All that the ticks reported, in order, each
    /// with the number of its tick.
    /// </summary>
    private static List<(int Tick, TickEvent Event)> PlayTheAct(
        Simulation simulation, Func<int, MagicianInput>? script = null)
    {
        var events = new List<(int Tick, TickEvent Event)>();
        int first = ActTicks - simulation.ActTicksLeft;
        for (int tick = first; simulation.Phase == Phase.Act; tick++)
        {
            simulation.Step(script?.Invoke(tick) ?? default);
            events.AddRange(simulation.Events.Select(happened => (tick, happened)));
        }

        return events;
    }

    /// <summary>Takes a card that the program must be offering.</summary>
    private static void Take(Simulation simulation, Card card)
    {
        Assert.That(simulation.Offer, Does.Contain(card));
        simulation.Pick(simulation.Offer.ToList().IndexOf(card));
    }

    /// <summary>
    /// A show whose first act is over: <paramref name="critics"/> entered in it, the first by the first door and
    /// the rest by the second, ten ticks apart, and the magician stood at the first door, felled who it could and
    /// picked up the one piece in its reach.
    /// </summary>
    private static Simulation AfterAFirstActOf(int critics, Tuning tuning, ulong seed = 1)
    {
        IEnumerable<PlannedEntry> rest =
            Enumerable.Range(1, critics - 1).Select(i => new PlannedEntry(10 * i, Door: 1, Kind: 0));
        var simulation = new Simulation(tuning, seed, [[AtOnce, .. rest], [AtOnce]]);
        Run(simulation, ActTicks);
        Assert.That(simulation.ActEntriesMade, Is.EqualTo(critics));
        Assert.That(simulation.ActApplause, Is.LessThanOrEqualTo(1));
        return simulation;
    }

    /// <summary>
    /// The first seed whose programs, one after each act of <see cref="Scene"/> played at the door, offer these
    /// cards: the first program all of <paramref name="programs"/>' first, the second all of its second. What a
    /// program offers follows from the seed, the bands of the acts so far and the tuning's chance of a chorus
    /// card, and from nothing else in the play: a show of that seed whose acts earn the second band is offered
    /// the same, wherever its magician walks and whichever card it takes.
    /// </summary>
    private ulong SeedWhosePrograms(params Card[][] programs)
    {
        for (ulong seed = 1; seed <= 1000; seed++)
        {
            // An act more than the programs: nothing is offered after the last act.
            var simulation = new Simulation(
                Scene with { ActsInPerformance = programs.Length + 1 },
                seed,
                [.. programs.Select(_ => new[] { AtOnce })]);
            bool offered = true;
            foreach (Card[] cards in programs)
            {
                Run(simulation, ActTicks);
                offered &= cards.All(simulation.Offer.Contains);
                simulation.Pick(0);
                simulation.GoOn();
            }

            if (offered)
            {
                return seed;
            }
        }

        Assert.Fail("No seed of a thousand offers these cards.");
        return 0;
    }

    /// <summary>
    /// A show that has taken <paramref name="cards"/>, one after each of as many acts, and stands between two
    /// acts: the next has <paramref name="entries"/>. In each act played the magician picked up the piece of its
    /// one critic and walked away to the right from the fifth tick on, so a second into every later act its
    /// understudy is fourteen units from the first door and further from the second: out of range of both.
    /// </summary>
    private Simulation ShowThatTook(Tuning tuning, PlannedEntry[] entries, params Card[] cards)
    {
        // A chorus card is the leftmost of every program when the tuning gives it every chance.
        ulong seed = SeedWhosePrograms([.. cards.Where(card => card != Card.ChorusDamage).Select(card => new[] { card })]);
        var simulation = new Simulation(tuning, seed, [.. cards.Select(_ => new[] { AtOnce }), entries]);
        for (int i = 0; i < cards.Length; i++)
        {
            if (i > 0)
            {
                simulation.GoOn();
            }

            PlayTheAct(simulation, tick => tick < 4 ? default : Right);
            Take(simulation, cards[i]);
        }

        return simulation;
    }
}
