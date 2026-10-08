using System.Numerics;

namespace Understudies.Core.Tests;

public class CardTests
{
    /// <summary>Two seconds: the act of <see cref="Scene"/>.</summary>
    private const int ActTicks = 2 * Simulation.TicksPerSecond;

    /// <summary>Twelve seconds: the program of <see cref="Scene"/>.</summary>
    private const int ProgramTicks = 12 * Simulation.TicksPerSecond;

    /// <summary>Six seconds: an encore of <see cref="Scene"/>.</summary>
    private const int EncoreTicks = 6 * Simulation.TicksPerSecond;

    /// <summary>The tick of an act of <see cref="Scene"/>, the first being 0, on which the piece of a critic
    /// that entered by the first door on the first tick is picked up by a magician that stands there.</summary>
    private const int PieceTick = 3;

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
    /// stands there. An encore costs one piece, the first and every other. A Vanish is ready again half a second
    /// later and leaves no cloud. A card gives a round number. The stage has no back wall and the curtain no
    /// length: these tests count their ticks from the first tick of an act.
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
        EncoreFirstCost = 1,
        EncoreCostGrowth = 0,
        EncoreTime = 6f,
        ProgramTime = 12f,
        CardDamage = 1f,
        CardAttackSpeed = 0.2f,
        CardRange = 1.5f,
        CardVanishCooldown = 0.25f,
        CardChorusDamage = 1f,
    };

    /// <summary><see cref="Scene"/> with an encore nobody can pay for.</summary>
    private Tuning NoEncore => Scene with { EncoreFirstCost = 1000 };

    // The encore (plan decision 26).

    [Test]
    public void Step_TheApplauseReachesTheCost_TheActStandsForAnEncoreOfThreeSelfCards()
    {
        // Two pieces an encore: critics by the magician's door on the first tick and ten ticks later.
        var simulation = new Simulation(Scene with { EncoreFirstCost = 2 }, seed: 1, [[AtOnce, AtOnce with { Tick = 10 }]]);
        Assert.That(simulation.EncoreCost, Is.EqualTo(2));

        Run(simulation, PieceTick + 1);
        Assert.That(simulation.EncoreApplause, Is.EqualTo(1));
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.Offer, Is.Empty);

        Run(simulation, ticks: 9);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        simulation.Step(default);

        // On the tick of the piece, which is played in full and reports what happened in it.
        Assert.That(simulation.Events.Select(happened => happened.Kind), Does.Contain(TickEventKind.ApplausePickedUp));
        Assert.That(simulation.EncoreApplause, Is.EqualTo(2));
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Encore));
        Assert.That(simulation.OfferTicksLeft, Is.EqualTo(EncoreTicks));
        Assert.That(simulation.Offer, Has.Count.EqualTo(3));
        Assert.That(simulation.Offer, Is.Unique);
        Assert.That(simulation.Offer, Has.None.EqualTo(Card.ChorusDamage));
        Assert.That(simulation.ActTicksLeft, Is.EqualTo(ActTicks - (PieceTick + 11)));
    }

    [Test]
    public void Offer_OfAnEncore_IsOfThreeDifferentSelfCards_AndEverySelfCardIsOfferedBySomeSeed()
    {
        var offered = new HashSet<Card>();
        for (ulong seed = 1; seed <= 100; seed++)
        {
            Simulation simulation = InAnEncore(Scene, seed);

            Assert.That(simulation.Offer, Has.Count.EqualTo(3));
            Assert.That(simulation.Offer, Is.Unique);
            Assert.That(simulation.Offer, Has.None.EqualTo(Card.ChorusDamage));
            offered.UnionWith(simulation.Offer);
        }

        Assert.That(offered, Has.Count.EqualTo(5));
    }

    [Test]
    public void Offer_TheSameSeedAndTheSamePlay_GiveTheSameEncores()
    {
        // Two encores each, in two acts: what the second offers follows from the seed as what the first does.
        List<Card> Offers(ulong seed)
        {
            var simulation = new Simulation(Scene, seed, [[AtOnce], [AtOnce]]);
            PlayTheAct(simulation);
            List<Card> offers = [.. simulation.Offer];
            simulation.Pick(0);
            PlayTheAct(simulation);
            simulation.Pick(0);
            simulation.GoOn();
            PlayTheAct(simulation);
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
    public void Step_InAnEncore_OnlyItsTimeRuns_AndTheActsOwnStands()
    {
        // A throw every two ticks, and a second critic by the other door: the card thrown at it is in the air,
        // not yet flown, when the first one's piece is picked up and the encore opens.
        Tuning tuning = Scene with { ThrowCooldown = 2f / Simulation.TicksPerSecond };
        var simulation = new Simulation(tuning, seed: 1, [[AtOnce, AtOnce with { Door = 1 }]]);
        PlayTheAct(simulation);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Encore));
        List<Vector2> critics = [.. simulation.Critics.Select(critic => critic.Position)];
        List<Vector2> cards = [.. simulation.ThrownCards.Select(card => card.Position)];
        Assert.That(critics, Is.Not.Empty);
        Assert.That(cards, Is.Not.Empty);
        int actTicksLeft = simulation.ActTicksLeft;

        simulation.Step(Right with { Vanish = true });

        Assert.That(simulation.Phase, Is.EqualTo(Phase.Encore));
        Assert.That(simulation.OfferTicksLeft, Is.EqualTo(EncoreTicks - 1));
        Assert.That(simulation.ActTicksLeft, Is.EqualTo(actTicksLeft));
        Assert.That(simulation.Events, Is.Empty);
        Assert.That(simulation.MagicianPosition, Is.EqualTo(AtTheDoor));
        Assert.That(simulation.Critics.Select(critic => critic.Position), Is.EqualTo(critics));
        Assert.That(simulation.ThrownCards.Select(card => card.Position), Is.EqualTo(cards));
        Assert.That(simulation.Offer, Has.Count.EqualTo(3));

        // And a press of the encore is not kept for the act.
        simulation.Pick(0);
        simulation.Step(default);
        Assert.That(simulation.Events.Select(happened => happened.Kind), Has.None.EqualTo(TickEventKind.Vanish));
    }

    [Test]
    public void Pick_InAnEncore_GivesTheMagicianTheCardAtOnce_TakesTheCost_AndTheActGoesOn()
    {
        Simulation simulation = InAnEncore(Scene, SeedWhoseEncores([Card.Range]));
        int place = simulation.Offer.ToList().IndexOf(Card.Range);
        int actTicksLeft = simulation.ActTicksLeft;

        simulation.Pick(place);

        Assert.That(simulation.MagicianCards, Is.EqualTo(new SelfCards(Range: 1)));
        Assert.That(simulation.ChorusCards, Is.Zero);
        Assert.That(simulation.EncoreApplause, Is.Zero);
        Assert.That(simulation.EncoresTaken, Is.EqualTo(1));
        Assert.That(simulation.Offer, Is.Empty);
        Assert.That(simulation.OfferTicksLeft, Is.Zero);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.ActTicksLeft, Is.EqualTo(actTicksLeft));

        // What the act has picked up is still its own count.
        Assert.That(simulation.ActApplause, Is.EqualTo(1));

        simulation.Step(default);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.ActTicksLeft, Is.EqualTo(actTicksLeft - 1));
    }

    [Test]
    public void Pick_TheVanishCooldownCardInAnEncore_TheVanishComesBackSoonerInThatVeryAct()
    {
        // Thirty ticks from Vanish to Vanish, and twenty-four with the card: taken on the fourth tick of an act
        // of a hundred and twenty.
        ulong seed = SeedWhoseEncores([Card.VanishCooldown]);
        var simulation = new Simulation(Scene, seed, [[AtOnce]]);
        var vanish = new MagicianInput(Vector2.Zero, Vanish: true);
        PlayTheAct(simulation);
        Take(simulation, Card.VanishCooldown);

        List<int> vanishes = [.. Of(PlayTheAct(simulation, _ => vanish), TickEventKind.Vanish).Select(v => v.Tick)];

        Assert.That(vanishes.Take(3), Is.EqualTo(new[] { PieceTick + 1, PieceTick + 25, PieceTick + 49 }));
    }

    [TestCase(-1)]
    [TestCase(3)]
    public void Pick_APlaceTheEncoreDoesNotHave_IsRefused(int place)
    {
        Simulation simulation = InAnEncore(Scene);

        simulation.Pick(place);

        Assert.That(simulation.Phase, Is.EqualTo(Phase.Encore));
        Assert.That(simulation.Offer, Has.Count.EqualTo(3));
        Assert.That(simulation.MagicianCards, Is.EqualTo(default(SelfCards)));
        Assert.That(simulation.EncoresTaken, Is.Zero);
    }

    [Test]
    public void GoOn_InAnEncore_IsRefused()
    {
        Simulation simulation = InAnEncore(Scene);

        simulation.GoOn();

        Assert.That(simulation.Phase, Is.EqualTo(Phase.Encore));
        Assert.That(simulation.Act, Is.EqualTo(1));
    }

    [Test]
    public void Step_TheEncoresTimeRunsOut_TheLeftmostCardIsTaken()
    {
        Simulation waited = InAnEncore(Scene);
        Simulation picked = InAnEncore(Scene);
        Simulation pickedAnother = InAnEncore(Scene);
        picked.Pick(0);
        pickedAnother.Pick(1);

        // On the last tick of the six seconds the choice is still open.
        Run(waited, EncoreTicks - 1);
        Assert.That(waited.Phase, Is.EqualTo(Phase.Encore));
        Assert.That(waited.OfferTicksLeft, Is.EqualTo(1));
        Assert.That(waited.MagicianCards, Is.EqualTo(default(SelfCards)));

        waited.Step(default);

        Assert.That(waited.Phase, Is.EqualTo(Phase.Act));
        Assert.That(waited.MagicianCards, Is.EqualTo(picked.MagicianCards));
        Assert.That(waited.MagicianCards, Is.Not.EqualTo(pickedAnother.MagicianCards));

        // And nothing else came of the wait: the act goes on as one whose leftmost card was picked at once.
        Assert.That(waited.ComputeStateHash(), Is.EqualTo(picked.ComputeStateHash()));
    }

    [Test]
    public void Pick_EveryEncoreTaken_MakesTheNextCostMore()
    {
        // One piece the first and one more each: six critics by the magician's door, ten ticks apart.
        var simulation = new Simulation(
            Scene with { EncoreCostGrowth = 1 },
            seed: 1,
            [[.. Enumerable.Range(0, 6).Select(i => AtOnce with { Tick = 10 * i })]]);
        var opened = new List<(int Pieces, int Cost)>();

        while (simulation.Phase is Phase.Act or Phase.Encore)
        {
            if (simulation.Phase == Phase.Encore)
            {
                opened.Add((simulation.ActApplause, simulation.EncoreCost));
                simulation.Pick(0);
                Assert.That(simulation.EncoreApplause, Is.Zero);
            }

            simulation.Step(default);
        }

        // After the first piece, the third and the sixth.
        Assert.That(opened, Is.EqualTo(new[] { (1, 1), (3, 2), (6, 3) }));
        Assert.That(simulation.EncoresTaken, Is.EqualTo(3));
        Assert.That(simulation.EncoreCost, Is.EqualTo(4));
    }

    [Test]
    public void Pick_WhatIsOverTheCost_StaysTowardTheNext_WhichOpensATickOfTheActLater()
    {
        // Three pieces while an encore costs more than anybody has, and then a tuning in which it costs one.
        var simulation = new Simulation(
            NoEncore, seed: 1, [[AtOnce, AtOnce with { Tick = 10 }, AtOnce with { Tick = 20 }]]);
        Run(simulation, PieceTick + 21);
        Assert.That(simulation.EncoreApplause, Is.EqualTo(3));
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        simulation.Tuning = Scene;
        simulation.Step(default);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Encore));
        int actTicksLeft = simulation.ActTicksLeft;

        simulation.Pick(0);

        // The act goes on for one tick, and stands again.
        Assert.That(simulation.EncoreApplause, Is.EqualTo(2));
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        simulation.Step(default);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Encore));
        Assert.That(simulation.ActTicksLeft, Is.EqualTo(actTicksLeft - 1));
        Assert.That(simulation.OfferTicksLeft, Is.EqualTo(EncoreTicks));

        simulation.Pick(0);
        simulation.Step(default);
        simulation.Pick(0);
        simulation.Step(default);

        // Three pieces, three encores, and nothing left to open a fourth.
        Assert.That(simulation.EncoresTaken, Is.EqualTo(3));
        Assert.That(simulation.EncoreApplause, Is.Zero);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.ActTicksLeft, Is.EqualTo(actTicksLeft - 3));
    }

    // A critic that enters four ticks before the act is over leaves its piece to the act's last tick, and one
    // that enters a tick sooner to the tick before it.
    [TestCase(ActTicks - 1 - PieceTick, Phase.BetweenActs)]
    [TestCase(ActTicks - 2 - PieceTick, Phase.Encore)]
    public void Step_ThePieceIsPickedUp_OnlyAnActThatHasTimeLeftStandsForAnEncore(int entersOn, Phase phase)
    {
        var simulation = new Simulation(Scene, seed: 1, [[AtOnce with { Tick = entersOn }], []]);

        PlayTheAct(simulation);

        Assert.That(simulation.EncoreApplause, Is.EqualTo(1));
        Assert.That(simulation.Phase, Is.EqualTo(phase));
        if (phase == Phase.BetweenActs)
        {
            // The act is over, with no encore and so no program, and what it had toward an encore is lost.
            Assert.That(simulation.Offer, Is.Empty);
            Assert.That(simulation.EncoresTaken, Is.Zero);
            simulation.GoOn();
            Assert.That(simulation.EncoreApplause, Is.Zero);
            Run(simulation, ticks: 2);
            Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        }
        else
        {
            Assert.That(simulation.ActTicksLeft, Is.EqualTo(1));
        }
    }

    [Test]
    public void GoOn_WhatTheActHadTowardAnEncoreIsLost_AndTheEncoresTakenStay()
    {
        // One piece the first encore and two the second: each act has one critic.
        var simulation = new Simulation(Scene with { EncoreCostGrowth = 1 }, seed: 1, [[AtOnce], [AtOnce], [AtOnce]]);
        PlayTheAct(simulation);
        simulation.Pick(0);
        PlayTheAct(simulation);
        simulation.Pick(0);
        simulation.GoOn();
        Assert.That((simulation.EncoresTaken, simulation.EncoreCost), Is.EqualTo((1, 2)));

        // The second act's one piece is half an encore, and the third's is the first half again.
        PlayTheAct(simulation);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.BetweenActs));
        Assert.That(simulation.EncoreApplause, Is.EqualTo(1));
        simulation.GoOn();
        Assert.That(simulation.EncoreApplause, Is.Zero);
        PlayTheAct(simulation);

        Assert.That(simulation.Phase, Is.EqualTo(Phase.BetweenActs));
        Assert.That(simulation.EncoreApplause, Is.EqualTo(1));
        Assert.That(simulation.EncoresTaken, Is.EqualTo(1));
    }

    [Test]
    public void Step_TheMagicianHasFallen_NoEncoreOpens_WhateverItHadPickedUp()
    {
        // One piece, while an encore costs more. Then a critic that no card reaches enters by the magician's
        // door, touching it, and its one blow fells the magician. Now an encore costs the one piece.
        var simulation = new Simulation(NoEncore, seed: 1, [[AtOnce, AtOnce with { Tick = 10 }]]);
        Run(simulation, ticks: 10);
        simulation.Tuning = NoEncore with { ThrowRange = 0f, CriticTurnRadius = 4f, CriticTouchDamage = 100f };
        Run(simulation, ticks: 5);
        Assert.That(simulation.MagicianHasFallen);
        Assert.That(simulation.EncoreApplause, Is.EqualTo(1));

        simulation.Tuning = simulation.Tuning with { EncoreFirstCost = 1 };
        simulation.Step(default);

        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.Offer, Is.Empty);
    }

    // The program (plan decision 26): the chorus card after an act in which an encore was taken.

    [Test]
    public void Step_AnActWithAnEncoreIsOver_TheProgramOffersTheChorusCardAlone()
    {
        Simulation simulation = AfterAFirstActWithAnEncore();

        Assert.That(simulation.Phase, Is.EqualTo(Phase.Program));
        Assert.That(simulation.Offer, Is.EqualTo(new[] { Card.ChorusDamage }));
        Assert.That(simulation.OfferTicksLeft, Is.EqualTo(ProgramTicks));
    }

    // An act that picked up its piece and could not pay for an encore with it, and one that picked up nothing.
    [TestCase(9f, 1)]
    [TestCase(0f, 0)]
    public void Step_AnActWithNoEncoreIsOver_ThereIsNoProgram_WhateverItsApplause(float range, int pieces)
    {
        var simulation = new Simulation(NoEncore with { ThrowRange = range }, seed: 1, [[AtOnce], [AtOnce]]);

        Run(simulation, ActTicks);

        Assert.That(simulation.ActApplause, Is.EqualTo(pieces));
        Assert.That(simulation.Phase, Is.EqualTo(Phase.BetweenActs));
        Assert.That(simulation.Offer, Is.Empty);
        Assert.That(simulation.OfferTicksLeft, Is.Zero);
    }

    [Test]
    public void Step_AnEncoreOfAnEarlierAct_PaysForNoProgramAfterALaterOne()
    {
        // The first act has its encore and its program. The second has no critic, and so no applause.
        var simulation = new Simulation(Scene, seed: 1, [[AtOnce], [], [AtOnce]]);
        PlayTheAct(simulation);
        simulation.Pick(0);
        PlayTheAct(simulation);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Program));
        simulation.Pick(0);
        simulation.GoOn();

        PlayTheAct(simulation);

        Assert.That(simulation.EncoresTaken, Is.EqualTo(1));
        Assert.That(simulation.Phase, Is.EqualTo(Phase.BetweenActs));
        Assert.That(simulation.ChorusCards, Is.EqualTo(1));

        // And the third, with an encore of its own, has its program again.
        simulation.GoOn();
        PlayTheAct(simulation);
        simulation.Pick(0);
        PlayTheAct(simulation);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Program));
    }

    [Test]
    public void Offer_WhileAnActIsPlayed_IsEmpty()
    {
        var simulation = new Simulation(NoEncore, seed: 1, [[AtOnce]]);

        Run(simulation, ActTicks - 1);

        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.Offer, Is.Empty);
    }

    [Test]
    public void Step_InTheProgram_OnlyTheCountdownRuns()
    {
        // A second critic enters by the other door three ticks before the act is over: it is on the stage, and
        // the card thrown at it in the air, when the program comes.
        var simulation = new Simulation(Scene, seed: 1, [[AtOnce, new PlannedEntry(ActTicks - 3, Door: 1, Kind: 0)], []]);
        PlayTheAct(simulation);
        simulation.Pick(0);
        PlayTheAct(simulation);
        List<Vector2> critics = [.. simulation.Critics.Select(critic => critic.Position)];
        List<Vector2> cards = [.. simulation.ThrownCards.Select(card => card.Position)];
        Assert.That(critics, Is.Not.Empty);
        Assert.That(cards, Is.Not.Empty);

        simulation.Step(Right with { Vanish = true });

        Assert.That(simulation.Phase, Is.EqualTo(Phase.Program));
        Assert.That(simulation.OfferTicksLeft, Is.EqualTo(ProgramTicks - 1));
        Assert.That(simulation.Events, Is.Empty);
        Assert.That(simulation.MagicianPosition, Is.EqualTo(AtTheDoor));
        Assert.That(simulation.Critics.Select(critic => critic.Position), Is.EqualTo(critics));
        Assert.That(simulation.ThrownCards.Select(card => card.Position), Is.EqualTo(cards));
        Assert.That(simulation.Offer, Has.Count.EqualTo(1));
    }

    [Test]
    public void GoOn_InTheProgram_IsRefused()
    {
        Simulation simulation = AfterAFirstActWithAnEncore();

        simulation.GoOn();

        Assert.That(simulation.Phase, Is.EqualTo(Phase.Program));
        Assert.That(simulation.Act, Is.EqualTo(1));
    }

    [Test]
    public void Pick_InTheProgram_TakesTheChorusCard_AndTheStageIsBetweenTwoActs()
    {
        Simulation simulation = AfterAFirstActWithAnEncore();

        SelfCards cards = simulation.MagicianCards;

        simulation.Pick(0);

        Assert.That(simulation.ChorusCards, Is.EqualTo(1));
        Assert.That(simulation.MagicianCards, Is.EqualTo(cards));
        Assert.That(simulation.EncoresTaken, Is.EqualTo(1));
        Assert.That(simulation.Phase, Is.EqualTo(Phase.BetweenActs));
        Assert.That(simulation.Offer, Is.Empty);
        Assert.That(simulation.OfferTicksLeft, Is.Zero);

        simulation.GoOn();
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.Act, Is.EqualTo(2));
    }

    [TestCase(-1)]
    [TestCase(1)]
    public void Pick_APlaceTheProgramDoesNotHave_IsRefused(int place)
    {
        Simulation simulation = AfterAFirstActWithAnEncore();

        simulation.Pick(place);

        Assert.That(simulation.Phase, Is.EqualTo(Phase.Program));
        Assert.That(simulation.Offer, Has.Count.EqualTo(1));
        Assert.That(simulation.ChorusCards, Is.Zero);
    }

    [Test]
    public void Pick_WhileNothingIsOffered_IsRefused()
    {
        var simulation = new Simulation(Scene, seed: 1, [[AtOnce]]);

        // In an act.
        simulation.Pick(0);
        Assert.That(simulation.MagicianCards, Is.EqualTo(default(SelfCards)));
        Assert.That(simulation.EncoresTaken, Is.Zero);

        // Between two acts, when the card has been taken: a program gives one card.
        PlayTheAct(simulation);
        simulation.Pick(0);
        PlayTheAct(simulation);
        simulation.Pick(0);
        SelfCards cards = simulation.MagicianCards;
        simulation.Pick(0);
        Assert.That(simulation.ChorusCards, Is.EqualTo(1));
        Assert.That(simulation.MagicianCards, Is.EqualTo(cards));
        Assert.That(simulation.EncoresTaken, Is.EqualTo(1));
    }

    [Test]
    public void Step_TheProgramsTimeRunsOut_TheCardIsTaken()
    {
        Simulation waited = AfterAFirstActWithAnEncore();
        Simulation picked = AfterAFirstActWithAnEncore();
        picked.Pick(0);

        // On the last tick of the twelve seconds the card is still on offer.
        Run(waited, ProgramTicks - 1);
        Assert.That(waited.Phase, Is.EqualTo(Phase.Program));
        Assert.That(waited.OfferTicksLeft, Is.EqualTo(1));
        Assert.That(waited.ChorusCards, Is.Zero);

        waited.Step(default);

        Assert.That(waited.Phase, Is.EqualTo(Phase.BetweenActs));
        Assert.That(waited.Offer, Is.Empty);
        Assert.That(waited.ChorusCards, Is.EqualTo(1));

        // And nothing else came of the wait: the show goes on as one whose card was picked at once.
        Assert.That(waited.ComputeStateHash(), Is.EqualTo(picked.ComputeStateHash()));
    }

    [Test]
    public void Step_TheLastActIsOver_NothingIsOffered()
    {
        Simulation simulation = AfterAFirstActWithAnEncore(Scene with { ActsInPerformance = 1 });

        Assert.That(simulation.Phase, Is.EqualTo(Phase.Ovation));
        Assert.That(simulation.Offer, Is.Empty);
    }

    [Test]
    public void Step_TheBoxOfficeFallsOnTheLastTickOfAnAct_NothingIsOffered()
    {
        // The box office stands against the second door with one hit point. A critic enters there on the tick
        // before the act's last, touching it, and strikes on the last: the show closes with an encore taken in
        // the act and another act to come.
        Tuning tuning = Scene with
        {
            BoxOfficePosition = OtherDoor + new Vector2(0f, 2f),
            BoxOfficeSize = 4f,
            BoxOfficeHitPoints = 1f,
            CriticStrikeDamage = 1f,

            // The box office has come to within the committed radius of where the scene's critic falls: the
            // scene is about the act's last tick, and its encore is earned as everywhere else in these tests.
            ApplauseBoxOfficeRadius = 0f,
        };
        var simulation = new Simulation(tuning, seed: 1, [[AtOnce, new PlannedEntry(ActTicks - 2, Door: 1, Kind: 0)], []]);
        PlayTheAct(simulation);
        simulation.Pick(0);
        Run(simulation, ActTicks - PieceTick - 2);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Act));
        Assert.That(simulation.ActTicksLeft, Is.EqualTo(1));

        simulation.Step(default);

        Assert.That(simulation.ActTicksLeft, Is.Zero);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.Closed));
        Assert.That(simulation.Offer, Is.Empty);
        Assert.That(simulation.OfferTicksLeft, Is.Zero);

        // And there is nothing to take.
        simulation.Pick(0);
        Assert.That(simulation.ChorusCards, Is.Zero);
    }

    // What each card changes (plan decision 20).

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

    // An understudy takes its act's encores (plan T24).

    [Test]
    public void Pick_ASelfCardInAnEncore_ItsActsUnderstudyGainsItOnTheTickItWasTaken_InEveryLaterAct()
    {
        // The first act's encore opens when the tick of the piece is over, and its range card is the magician's
        // from the next tick of the act on. The later acts have a curtain of six ticks.
        ulong seed = SeedWhoseEncores([Card.Range]);
        var simulation = new Simulation(Scene, seed, [[AtOnce]]);
        PlayTheAct(simulation);
        Take(simulation, Card.Range);
        simulation.Tuning = NoEncore with { CurtainTime = 0.1f };
        PlayTheAct(simulation);
        simulation.Pick(0);

        for (int act = 2; act <= 3; act++)
        {
            // What the act began with, under the curtain and up to the tick of the encore.
            simulation.GoOn();
            Assert.That(simulation.Understudies[0].Cards, Is.EqualTo(default(SelfCards)), $"act {act}, as it begins");
            while (simulation.Phase == Phase.Curtain)
            {
                simulation.Step(default);
            }

            Assert.That(simulation.Understudies[0].Cards, Is.EqualTo(default(SelfCards)), $"act {act}, the curtain");
            Run(simulation, PieceTick + 1);
            Assert.That(simulation.Understudies[0].Cards, Is.EqualTo(default(SelfCards)), $"act {act}, the piece's tick");

            // And the card from the tick after it, to the end of the act.
            simulation.Step(default);
            Assert.That(simulation.Understudies[0].Cards, Is.EqualTo(new SelfCards(Range: 1)), $"act {act}, the next tick");
            PlayTheAct(simulation);
            Assert.That(simulation.Understudies[0].Cards, Is.EqualTo(new SelfCards(Range: 1)), $"act {act}, over");
        }

        // The second act began with the card: its understudy has it from the first.
        Assert.That(simulation.Understudies[1].Cards, Is.EqualTo(new SelfCards(Range: 1)));
        Assert.That(simulation.MagicianCards, Is.EqualTo(new SelfCards(Range: 1)));
    }

    [Test]
    public void Pick_OneMoreCardInAnEncore_ItsActsUnderstudyThrowsOnThatTickAsTheMagicianDid()
    {
        // A throw every three ticks: on the second tick of the act, at the critic that entered on the first, and
        // on the fifth, which is the first tick after the encore, at the two that entered on the third, one by
        // each door. The second act has two critics from its first tick as well, and its magician walks away
        // from its first tick: a card thrown from the door is the understudy's.
        Tuning quick = Scene with { ThrowCooldown = 0.05f };
        PlannedEntry byTheOther = new(Tick: 0, Door: 1, Kind: 0);
        PlannedEntry[] onTheThird = [AtOnce with { Tick = 2 }, byTheOther with { Tick = 2 }];
        ulong seed = SeedWhoseEncores([Card.OneMoreCard]);
        var simulation = new Simulation(quick, seed, [[AtOnce, .. onTheThird], [AtOnce, byTheOther, .. onTheThird]]);

        List<(int Tick, TickEvent Event)> first = PlayTheAct(simulation);
        Take(simulation, Card.OneMoreCard);
        simulation.Tuning = quick with { EncoreFirstCost = 1000 };
        first.AddRange(PlayTheAct(simulation));
        simulation.Pick(0);
        simulation.GoOn();
        List<(int Tick, TickEvent Event)> second = PlayTheAct(simulation, _ => Right);

        int Thrown(List<(int Tick, TickEvent Event)> events, int tick) =>
            Of(events, TickEventKind.Throw).Count(thrown => thrown.Tick == tick && thrown.Event.Position == AtTheDoor);

        Assert.That(simulation.Understudies[0].Position, Is.EqualTo(AtTheDoor));
        Assert.That((Thrown(first, 1), Thrown(first, PieceTick + 1)), Is.EqualTo((1, 2)), "the magician");
        Assert.That((Thrown(second, 1), Thrown(second, PieceTick + 1)), Is.EqualTo((1, 2)), "its understudy");
    }

    [Test]
    public void Step_TheMagicianFellAfterAnEncore_ItsUnderstudyHasThatEncore_AndLeavesTheStageAsBefore()
    {
        // The range card on the tick after the piece's. Then the critic that enters on the eleventh tick turns
        // on the magician, which stands in its reach, and fells it with one touch on the twelfth.
        ulong seed = SeedWhoseEncores([Card.Range]);
        var simulation = new Simulation(Scene, seed, [[AtOnce, AtOnce with { Tick = 10 }]]);
        PlayTheAct(simulation);
        Take(simulation, Card.Range);
        simulation.Tuning = NoEncore with { CriticTurnRadius = 2f, CriticTouchDamage = 1000f };
        PlayTheAct(simulation);
        Assert.That(simulation.MagicianHasFallen, Is.True);
        simulation.Pick(0);
        simulation.Tuning = NoEncore;
        simulation.GoOn();

        Run(simulation, PieceTick + 2);
        Understudy understudy = simulation.Understudies[0];
        Assert.That((understudy.IsOnStage, understudy.Cards), Is.EqualTo((true, new SelfCards(Range: 1))));
        Assert.That(understudy.Route, Has.Count.EqualTo(12));
        PlayTheAct(simulation);
        Assert.That((understudy.IsOnStage, understudy.Cards), Is.EqualTo((false, new SelfCards(Range: 1))));
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

    // Two acts played at the door to their ends, each with an encore that gives a quicker Vanish, which nobody
    // asks for, and a chorus card after it. In the third the magician walks away, and a critic of six hit points
    // enters a second in, in range of the two understudies alone, which throw on one tick. A card of an
    // understudy takes one, and three with two chorus cards that add one each: the first of their two cards
    // hurts and the second fells. With chorus cards that add nothing, six cards.
    [TestCase(1f, 1)]
    [TestCase(0f, 5)]
    public void Pick_TheChorusCard_EveryUnderstudysCardsHurtMore(float cardChorusDamage, int hitsBeforeTheFall)
    {
        ulong seed = SeedWhoseEncores([Card.VanishCooldown], [Card.VanishCooldown]);
        var simulation = new Simulation(
            Scene with { CardChorusDamage = cardChorusDamage }, seed, [[AtOnce], [AtOnce], [Later]]);
        for (int act = 1; act <= 2; act++)
        {
            PlayTheAct(simulation);
            Take(simulation, Card.VanishCooldown);
            PlayTheAct(simulation);
            Take(simulation, Card.ChorusDamage);
            if (act == 1)
            {
                simulation.GoOn();
            }
        }

        Assert.That(simulation.ChorusCards, Is.EqualTo(2));
        Assert.That(simulation.MagicianCards, Is.EqualTo(new SelfCards(VanishCooldown: 2)));
        simulation.Tuning = simulation.Tuning.WithCritic(critic => critic with { HitPoints = 6f }) with
        {
            EncoreFirstCost = 1000,
        };
        simulation.GoOn();

        List<(int Tick, TickEvent Event)> events = PlayTheAct(simulation, _ => Right);

        // The understudy of the act before the second card was taken and the one of the act after the first.
        Assert.That(Of(events, TickEventKind.Throw).Take(2).Select(thrown => thrown.Event.Position), Is.All.EqualTo(AtTheDoor));
        Assert.That(Of(events, TickEventKind.Hit), Has.Count.EqualTo(hitsBeforeTheFall));
        Assert.That(Of(events, TickEventKind.Kill), Has.Count.EqualTo(1));
    }

    [Test]
    public void Pick_TheChorusCard_TheMagiciansOwnCardsHurtAsBefore()
    {
        // The first act's magician takes a quicker Vanish in its encore and walks away to the right, so its
        // understudy is out of range of the door a second into the next act. There a critic of two hit points
        // has the magician alone in range of it: two cards of one, as without the chorus card.
        ulong seed = SeedWhoseEncores([Card.VanishCooldown]);
        var simulation = new Simulation(Scene, seed, [[AtOnce], [Later]]);
        PlayTheAct(simulation);
        Take(simulation, Card.VanishCooldown);
        PlayTheAct(simulation, _ => Right);
        Take(simulation, Card.ChorusDamage);
        simulation.Tuning = NoEncore.WithCritic(critic => critic with { HitPoints = 2f });
        simulation.GoOn();

        List<(int Tick, TickEvent Event)> events = PlayTheAct(simulation);

        Assert.That(Of(events, TickEventKind.Throw).Select(thrown => thrown.Event.Position), Is.All.EqualTo(AtTheDoor));
        Assert.That(Of(events, TickEventKind.Hit), Has.Count.EqualTo(1));
        Assert.That(Of(events, TickEventKind.Kill), Has.Count.EqualTo(1));
    }

    [Test]
    public void Pick_TheChorusCard_AddsToAnUnderstudysOwnDamageCard()
    {
        // The first act's encore gives a damage card and its program the chorus card. The second act, played
        // at the door to its end, begins with the damage card, and has no encore and no program. In the third
        // the magician walks away and a critic of five hit points enters: the first act's understudy takes two
        // with a card and the second's three, the five between them.
        ulong seed = SeedWhoseEncores([Card.Damage]);
        var simulation = new Simulation(Scene, seed, [[AtOnce], [], [Later]]);
        PlayTheAct(simulation);
        Take(simulation, Card.Damage);
        simulation.Tuning = NoEncore;
        PlayTheAct(simulation);
        Take(simulation, Card.ChorusDamage);
        simulation.GoOn();
        PlayTheAct(simulation);
        Assert.That(simulation.Phase, Is.EqualTo(Phase.BetweenActs));
        simulation.Tuning = simulation.Tuning.WithCritic(critic => critic with { HitPoints = 5f });
        simulation.GoOn();

        List<(int Tick, TickEvent Event)> events = PlayTheAct(simulation, _ => Right);

        Assert.That(Of(events, TickEventKind.Hit), Has.Count.EqualTo(1));
        Assert.That(Of(events, TickEventKind.Kill), Has.Count.EqualTo(1));
    }

    // The state hash.

    [Test]
    public void ComputeStateHash_TwoProgramsWithTwoTimesLeft_AreTwoHashes()
    {
        Simulation one = AfterAFirstActWithAnEncore();
        Simulation other = AfterAFirstActWithAnEncore();

        other.Step(default);

        Assert.That(other.ComputeStateHash(), Is.Not.EqualTo(one.ComputeStateHash()));
    }

    [Test]
    public void ComputeStateHash_TwoEncoresWithTwoTimesLeft_AreTwoHashes()
    {
        Simulation one = InAnEncore(Scene);
        Simulation other = InAnEncore(Scene);

        other.Step(default);

        Assert.That(other.ComputeStateHash(), Is.Not.EqualTo(one.ComputeStateHash()));
    }

    [Test]
    public void ComputeStateHash_AnActThatStandsForAnEncoreAndOneThatDoesNot_AreTwoHashes()
    {
        // The same act to the tick of its piece, which pays for an encore in one show and not in the other:
        // one tuning from there on.
        Simulation one = InAnEncore(Scene);
        Simulation other = InAnEncore(NoEncore);
        other.Tuning = Scene;

        Assert.That((one.Phase, other.Phase), Is.EqualTo((Phase.Encore, Phase.Act)));
        Assert.That(other.ComputeStateHash(), Is.Not.EqualTo(one.ComputeStateHash()));
    }

    [Test]
    public void ComputeStateHash_TwoMagiciansWithTwoCards_AreTwoHashes()
    {
        Simulation one = InAnEncore(Scene);
        Simulation other = InAnEncore(Scene);

        one.Pick(0);
        other.Pick(1);

        Assert.That(other.ComputeStateHash(), Is.Not.EqualTo(one.ComputeStateHash()));
    }

    [Test]
    public void ComputeStateHash_TwoActsWithTwoCountsTowardTheNextEncore_AreTwoHashes()
    {
        // Two pieces each while an encore costs more than anybody has. Then it costs one piece in one show and
        // two in the other: both stand for an encore of the same cards and take the leftmost, and one has a
        // piece left. One tuning again: the two shows differ only in what they have toward the next encore.
        Simulation Show(int cost)
        {
            var simulation = new Simulation(NoEncore, seed: 1, [[AtOnce, AtOnce with { Tick = 10 }]]);
            Run(simulation, PieceTick + 11);
            simulation.Tuning = Scene with { EncoreFirstCost = cost };
            simulation.Step(default);
            simulation.Pick(0);
            simulation.Tuning = NoEncore;
            return simulation;
        }

        Simulation one = Show(cost: 1);
        Simulation other = Show(cost: 2);

        Assert.That((one.EncoreApplause, other.EncoreApplause), Is.EqualTo((1, 0)));
        Assert.That((one.EncoresTaken, other.EncoresTaken), Is.EqualTo((1, 1)));
        Assert.That(other.MagicianCards, Is.EqualTo(one.MagicianCards));
        Assert.That(other.ComputeStateHash(), Is.Not.EqualTo(one.ComputeStateHash()));
    }

    [Test]
    public void ComputeStateHash_TheSameCardsTakenInTwoOrders_AreTwoHashes_ForTheActsBeganWithOthers()
    {
        // Two cards that leave nothing behind in an act played at the door: a longer throw, and a Vanish nobody
        // asks for. Both shows are offered both, twice, and take them in two orders.
        Card[] both = [Card.Range, Card.VanishCooldown];
        ulong seed = SeedWhoseEncores(both, both);
        Simulation Show(Card first, Card second)
        {
            var simulation = new Simulation(Scene, seed, [[AtOnce], [AtOnce]]);
            PlayTheAct(simulation);
            Take(simulation, first);
            PlayTheAct(simulation);
            simulation.Pick(0);
            simulation.GoOn();
            PlayTheAct(simulation);
            Take(simulation, second);
            PlayTheAct(simulation);
            simulation.Pick(0);
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
    public void ComputeStateHash_TheSameEncoreOnTwoTicks_AreTwoHashes_InTheRecordingAndInItsUnderstudy()
    {
        // The one critic of the act enters on its first tick in one show and on its eleventh in the other: both
        // magicians stand at the door, pick its piece up and take the leftmost card of the same encore, ten
        // ticks apart. When the act is over nothing is left of it on the stage, and the two shows differ only in
        // the tick of the encore that the act's recording keeps.
        Simulation Show(int entersOn)
        {
            var simulation = new Simulation(Scene, seed: 1, [[AtOnce with { Tick = entersOn }]]);
            PlayTheAct(simulation);
            simulation.Pick(0);
            PlayTheAct(simulation);
            Assert.That((simulation.Phase, simulation.Critics, simulation.ThrownCards, simulation.ApplauseOnTheFloor),
                Is.EqualTo((Phase.Program, Array.Empty<Critic>(), Array.Empty<ThrownCard>(), Array.Empty<Applause>())));
            return simulation;
        }

        Simulation one = Show(entersOn: 0);
        Simulation other = Show(entersOn: 10);

        Assert.That(other.MagicianCards, Is.EqualTo(one.MagicianCards));
        Assert.That(other.ComputeStateHash(), Is.Not.EqualTo(one.ComputeStateHash()));

        // In the second act, past both ticks, the two understudies have the same cards, and each gains its own
        // on another tick of every act to come.
        foreach (Simulation simulation in new[] { one, other })
        {
            simulation.Pick(0);
            simulation.GoOn();
            Run(simulation, ticks: 20);
        }

        Assert.That(one.Understudies[0].Cards, Is.Not.EqualTo(default(SelfCards)));
        Assert.That(other.Understudies[0].Cards, Is.EqualTo(one.Understudies[0].Cards));
        Assert.That(other.ComputeStateHash(), Is.Not.EqualTo(one.ComputeStateHash()));
    }

    [Test]
    public void ComputeStateHash_TwoEncoresOfOneActTakenInTwoOrders_AreTwoHashes_ForTheCardsOfTheRecording()
    {
        // Two critics in one act, ten ticks apart, and two encores: both shows are offered the same two cards at
        // both and take them in two orders. The magicians end alike and the ticks of the encores are the same:
        // the shows differ only in which card the recording keeps for which tick.
        Card[] both = [Card.Range, Card.VanishCooldown];
        ulong seed = SeedWhoseEncores(both, both);
        Simulation Show(Card first, Card second)
        {
            var simulation = new Simulation(Scene, seed, [[AtOnce, AtOnce with { Tick = 10 }]]);
            PlayTheAct(simulation);
            Take(simulation, first);
            PlayTheAct(simulation);
            Take(simulation, second);
            PlayTheAct(simulation);
            Assert.That(simulation.Phase, Is.EqualTo(Phase.Program));
            return simulation;
        }

        Simulation one = Show(Card.Range, Card.VanishCooldown);
        Simulation other = Show(Card.VanishCooldown, Card.Range);

        Assert.That(other.MagicianCards, Is.EqualTo(one.MagicianCards));
        Assert.That(other.ComputeStateHash(), Is.Not.EqualTo(one.ComputeStateHash()));

        // Between the two ticks of the next act the two understudies have each its own first encore's card.
        foreach (Simulation simulation in new[] { one, other })
        {
            simulation.Pick(0);
            simulation.GoOn();
            Run(simulation, ticks: 8);
        }

        Assert.That(other.Understudies[0].Cards, Is.Not.EqualTo(one.Understudies[0].Cards));
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
    /// Plays the act by <paramref name="script"/> until it stands for an encore or is over: the script is asked for
    /// each tick of the act by its number, the first being 0, and with no script the magician stands. All that
    /// the ticks reported, in order, each with the number of its tick.
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

    /// <summary>Takes a card that must be on offer.</summary>
    private static void Take(Simulation simulation, Card card)
    {
        Assert.That(simulation.Offer, Does.Contain(card));
        simulation.Pick(simulation.Offer.ToList().IndexOf(card));
    }

    /// <summary>
    /// A show whose first act is over: one critic entered in it by the first door on its first tick, and the
    /// magician stood there, felled it, picked its piece up and took the leftmost card of the encore it paid for.
    /// </summary>
    private Simulation AfterAFirstActWithAnEncore(Tuning? tuning = null)
    {
        var simulation = new Simulation(tuning ?? Scene, seed: 1, [[AtOnce], [AtOnce]]);
        PlayTheAct(simulation);
        simulation.Pick(0);
        PlayTheAct(simulation);
        Assert.That(simulation.EncoresTaken, Is.EqualTo(1));
        return simulation;
    }

    /// <summary>
    /// A show on the tick its first act's one piece is picked up: where an encore that costs one piece opens.
    /// </summary>
    private static Simulation InAnEncore(Tuning tuning, ulong seed = 1)
    {
        var simulation = new Simulation(tuning, seed, [[AtOnce], [AtOnce]]);
        Run(simulation, PieceTick + 1);
        Assert.That(simulation.EncoreApplause, Is.EqualTo(1));
        return simulation;
    }

    /// <summary>
    /// The first seed whose encores, one in each act of <see cref="Scene"/> played at the door, offer these
    /// cards: the first encore all of <paramref name="encores"/>' first, the second all of its second. What an
    /// encore offers follows from the seed and from how many encores were opened before it, and from nothing
    /// else in the play: a show of that seed is offered the same, wherever its magician walks and whichever
    /// card it takes.
    /// </summary>
    private ulong SeedWhoseEncores(params Card[][] encores)
    {
        for (ulong seed = 1; seed <= 1000; seed++)
        {
            var simulation = new Simulation(
                Scene with { ActsInPerformance = encores.Length + 1 },
                seed,
                [.. encores.Select(_ => new[] { AtOnce })]);
            bool offered = true;
            foreach (Card[] cards in encores)
            {
                PlayTheAct(simulation);
                offered &= cards.All(simulation.Offer.Contains);
                simulation.Pick(0);
                PlayTheAct(simulation);
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
    /// A show that has taken <paramref name="cards"/>, one in an encore of each of as many acts, and their
    /// programs' chorus cards, and stands between two acts: the next has <paramref name="entries"/>, and no
    /// encore that anybody can pay for. In each act played the magician picked up the piece of its one critic
    /// and walked away to the right from the fifth tick on, so a second into every later act its understudy is
    /// fourteen units from the first door and further from the second: out of range of both.
    /// </summary>
    private Simulation ShowThatTook(Tuning tuning, PlannedEntry[] entries, params Card[] cards)
    {
        ulong seed = SeedWhoseEncores([.. cards.Select(card => new[] { card })]);
        var simulation = new Simulation(tuning, seed, [.. cards.Select(_ => new[] { AtOnce }), entries]);
        for (int i = 0; i < cards.Length; i++)
        {
            if (i > 0)
            {
                simulation.GoOn();
            }

            PlayTheAct(simulation);
            Take(simulation, cards[i]);
            PlayTheAct(simulation, _ => Right);
            simulation.Pick(0);
        }

        simulation.Tuning = simulation.Tuning with { EncoreFirstCost = 1000 };
        return simulation;
    }
}
