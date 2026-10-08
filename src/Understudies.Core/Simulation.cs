using System.Numerics;
using Understudies.Core.Hashing;
using Understudies.Core.Randomness;

namespace Understudies.Core;

/// <summary>Owns the state of the game and advances it one tick at a time.</summary>
/// <param name="seed">What is left to chance in the show comes from this: the same seed, the same show.</param>
/// <param name="plan">
/// Who enters when, in place of the plan the seed gives: the entries of every act, an act after an act, each act's
/// in the order of their ticks. An act the plan does not reach has none.
/// </param>
public sealed class Simulation(Tuning tuning, ulong seed, IReadOnlyList<IReadOnlyList<PlannedEntry>> plan)
{
    public const int TicksPerSecond = 60;

    private readonly Rng _doorPlaces = Rng.ForStream(seed, RngStream.DoorPlaces);
    private readonly Rng _encore = Rng.ForStream(seed, RngStream.Encore);
    private readonly List<Critic> _critics = [];
    private readonly List<ThrownCard> _thrownCards = [];
    private readonly List<Cloud> _clouds = [];
    private readonly List<Understudy> _understudies = [];
    private readonly List<Applause> _applause = [];
    private readonly List<Card> _offer = [];

    // The recording of the act that is played: where the magician stood after each of its ticks so far, and each
    // Vanish of it. It stops when the magician falls: its last place is where the magician fell.
    private List<Vector2> _route = [];
    private List<(int Tick, Vector2 Place)> _vanishes = [];

    // The self cards the act that is played began with: its recording's, and what its understudy begins every
    // later act with. A card taken in an encore of the act is the magician's at once, and is recorded with the
    // tick of the act it was taken on: the number of the first tick the magician played with it.
    private SelfCards _recordingCards;
    private List<(int Tick, Card Card)> _encores = [];

    // Whom one throw is at, the nearest first. It is read within the throw that fills it: what it holds after
    // that is nobody's, and the next throw empties it before it looks for its own.
    private readonly List<Critic> _targets = [];
    private readonly List<TickEvent> _events = [];
    private int _ticksPlayed;

    // How many ticks of the act that is played have been played: the number of the next, the first being 0.
    private int _actTicksPlayed;
    private int _criticsEntered;
    private int _ticksToNextThrow;

    // One unit long: the way the magician was last asked to walk. When the curtain rises it faces the audience,
    // down the stage.
    private Vector2 _facing = Vector2.UnitY;
    private int _ticksToNextVanish;
    private int _ticksInvulnerable;
    private int _curtainTicksLeft = Ticks(tuning.CurtainTime);

    /// <summary>
    /// A show whose waves are planned here and now, from the seed and the tuning (<see cref="Waves.Plan"/>).
    /// </summary>
    public Simulation(Tuning tuning, ulong seed)
        : this(tuning, seed, Waves.Plan(tuning, seed))
    {
    }

    /// <summary>
    /// The numbers the rules run on. New ones may be set between ticks: the state stays as it is and the next tick
    /// runs on them. The plan stays too: it was made when the show was, and new numbers do not plan a performance
    /// under way again.
    /// </summary>
    public Tuning Tuning { get; set; } = tuning;

    /// <summary>The entries of every act of the performance, as they were planned when the show was made.</summary>
    public IReadOnlyList<IReadOnlyList<PlannedEntry>> Plan { get; } = plan;

    /// <summary>The planned entries of the act that is played, or of the one just over, in the order they enter.</summary>
    public IReadOnlyList<PlannedEntry> ActEntries => Act <= Plan.Count ? Plan[Act - 1] : [];

    /// <summary>
    /// How many of <see cref="ActEntries"/> have entered: those from this place on are still to come in this act.
    /// </summary>
    public int ActEntriesMade { get; private set; }

    /// <summary>
    /// Where the performance stands. A closed show is closed whatever its act's timer says; every act begins with
    /// its curtain; an act with cards on offer stands for an encore; an act whose time has run out is over, whether
    /// the magician stands or has fallen; while that one has a card on offer the program is up; and after the last
    /// act of the performance comes the ovation. Only in an act does <see cref="Step"/> change anything but the
    /// curtain's own time and an offer's.
    /// </summary>
    // ponytail: the phase is read off the other state and not kept, so a reload of the tuning with another number of
    // acts can move it without a tick (an ovation back to between two acts). Keep it as state if that ever matters.
    public Phase Phase =>
        ShowClosed ? Phase.Closed
        : _curtainTicksLeft > 0 ? Phase.Curtain
        : ActTicksLeft > 0 ? (_offer.Count > 0 ? Phase.Encore : Phase.Act)
        : _offer.Count > 0 ? Phase.Program
        : Act < Tuning.ActsInPerformance ? Phase.BetweenActs
        : Phase.Ovation;

    /// <summary>The number of the act that is played, or of the one just over: the first is 1.</summary>
    public int Act { get; private set; } = 1;

    /// <summary>
    /// What the act has left of its time, in ticks: all of its length when it begins, and nothing when it is over.
    /// </summary>
    public int ActTicksLeft { get; private set; } = Ticks(tuning.ActLength);

    /// <summary>
    /// How much of the curtain's time is left, as a share of the whole: 1 when it rises, and 0 when the act is
    /// played.
    /// </summary>
    public float CurtainLeft =>
        _curtainTicksLeft == 0 ? 0f : MathF.Min(1f, (float)_curtainTicksLeft / Ticks(Tuning.CurtainTime));

    /// <summary>The middle of the magician's circle on the floor, after the last tick.</summary>
    public Vector2 MagicianPosition { get; private set; } = tuning.MagicianMark;

    /// <summary>Where the magician was before the last tick: the view draws between the two.</summary>
    public Vector2 MagicianPreviousPosition { get; private set; } = tuning.MagicianMark;

    /// <summary>
    /// How much of the Vanish's cooldown is left, as a share of the whole: 1 on the tick of a Vanish, and one
    /// tick's share, not yet 0, when the very next tick would take a press again.
    /// </summary>
    public float VanishCooldownLeft =>
        _ticksToNextVanish == 0 ? 0f : MathF.Min(1f, (float)_ticksToNextVanish / VanishCooldownTicks);

    /// <summary>Nothing hurts the magician now: a moment that starts with a Vanish.</summary>
    public bool MagicianIsInvulnerable => _ticksInvulnerable > 0;

    /// <summary>The critics on the stage, in the order they entered.</summary>
    public IReadOnlyList<Critic> Critics => _critics;

    /// <summary>The cards in the air, in the order they were thrown.</summary>
    public IReadOnlyList<ThrownCard> ThrownCards => _thrownCards;

    /// <summary>The clouds on the floor, in the order they were left.</summary>
    public IReadOnlyList<Cloud> Clouds => _clouds;

    /// <summary>
    /// One for every act that was played before this one, in the order of their acts: the first act has none.
    /// </summary>
    public IReadOnlyList<Understudy> Understudies => _understudies;

    /// <summary>The applause on the floor, in the order it was dropped.</summary>
    public IReadOnlyList<Applause> ApplauseOnTheFloor => _applause;

    /// <summary>
    /// How many pieces of applause the magician has picked up in the act that is played, or in the one just over:
    /// every act starts with none, and nothing is kept from one act to the next.
    /// </summary>
    public int ActApplause { get; private set; }

    /// <summary>
    /// The pieces picked up in this act that no encore has been paid with yet: what the next encore is paid from
    /// (plan decision 26). Like <see cref="ActApplause"/> it is the act's own, and gone when the next begins.
    /// </summary>
    public int EncoreApplause { get; private set; }

    /// <summary>How many encores were taken in this performance, in all its acts.</summary>
    public int EncoresTaken { get; private set; }

    /// <summary>
    /// How many encores were taken in the act that is played, or in the one just over: an act with one has its
    /// program, and an act with none has no program.
    /// </summary>
    public int ActEncores { get; private set; }

    /// <summary>
    /// The pieces the next encore costs, on the tuning of now: every encore taken makes the next cost more.
    /// </summary>
    public int EncoreCost => Tuning.EncoreFirstCost + (Tuning.EncoreCostGrowth * EncoresTaken);

    /// <summary>
    /// The cards on offer, from the leftmost. In an encore, three different self cards; in the program, which
    /// comes after an act in which an encore was taken, when the act has another after it and has not closed the
    /// show, the chorus card alone. It is empty in every other phase.
    /// </summary>
    public IReadOnlyList<Card> Offer => _offer;

    /// <summary>
    /// What the offer has left of its time, in ticks: all of <see cref="Tuning.EncoreTime"/> when an encore
    /// opens and of <see cref="Tuning.ProgramTime"/> when the act is over, and the tick that would leave none
    /// takes the leftmost card. An offer of no time is still up, and waits for one tick: the first
    /// <see cref="Step"/> takes the leftmost card. Nothing while nothing is offered.
    /// </summary>
    public int OfferTicksLeft { get; private set; }

    /// <summary>
    /// The self cards the magician has taken in this performance: it throws and vanishes by the tuning's numbers
    /// as these change them.
    /// </summary>
    public SelfCards MagicianCards { get; private set; }

    /// <summary>
    /// How many chorus cards were taken in this performance: each adds to the cards of every understudy, on top
    /// of the understudy's own self cards, and to nothing of the magician's.
    /// </summary>
    public int ChorusCards { get; private set; }

    /// <summary>
    /// Takes the card at <paramref name="place"/> of <see cref="Offer"/>, the leftmost being 0. In an encore the
    /// card is the magician's at once, its cost is taken from <see cref="EncoreApplause"/> (what is over stays
    /// toward the next) and the act goes on. In the program the stage is between two acts, until
    /// <see cref="GoOn"/>. While nothing is offered, and for a place the offer does not have, this does nothing.
    /// </summary>
    public void Pick(int place)
    {
        // While nothing is offered the offer has no place.
        if (place < 0 || place >= _offer.Count)
        {
            return;
        }

        // The cost before the count of encores, which is what makes the next cost more. Never under nothing: a
        // reload of the tuning may have raised the cost while the encore was read.
        if (Phase == Phase.Encore)
        {
            EncoreApplause = Math.Max(0, EncoreApplause - EncoreCost);
            EncoresTaken++;
            ActEncores++;

            // An encore stands the act, so the act's count is still that of its next tick: the first the magician
            // plays with the card, and the tick on which its understudy gains it (plan T24).
            _encores.Add((_actTicksPlayed, _offer[place]));
        }

        if (_offer[place] == Card.ChorusDamage)
        {
            ChorusCards++;
        }
        else
        {
            MagicianCards = MagicianCards.With(_offer[place]);
        }

        _offer.Clear();
        OfferTicksLeft = 0;
    }

    /// <summary>What happened in the last tick, in the order it happened. The next tick starts the list afresh.</summary>
    public IReadOnlyList<TickEvent> Events => _events;

    /// <summary>What the magician has left: never less than nothing.</summary>
    public float MagicianHitPoints { get; private set; } = tuning.MagicianHitPoints;

    /// <summary>
    /// The magician has nothing left, from the tick of its fall until the next act begins. It lies where it fell
    /// and takes no input: it does not walk, vanish or throw, the act is no longer recorded, and no critic turns
    /// on it or touches it. The act goes on to its timer all the same.
    /// </summary>
    public bool MagicianHasFallen => MagicianHitPoints <= 0f;

    /// <summary>What the box office has left: never less than nothing.</summary>
    public float BoxOfficeHitPoints { get; private set; } = tuning.BoxOfficeHitPoints;

    /// <summary>
    /// The box office has nothing left. The show is over: <see cref="Step"/> changes nothing any more. The
    /// magician's fall does not close it.
    /// </summary>
    public bool ShowClosed => BoxOfficeHitPoints <= 0f;

    /// <summary>
    /// Whether a door, by its place in <see cref="Tuning.StageDoors"/>, is open in the act that is played or just
    /// over, as the tuning has it now: after a reload that need not be what the plan was made with.
    /// </summary>
    public bool DoorIsOpen(int door) => Tuning.StageDoors[door].OpensInAct <= Act;

    /// <summary>
    /// Plays one tick of an act. While the curtain is up the tick only counts the curtain's time: nothing moves,
    /// strikes or is released, the act's own time stands and the input is not taken, nor kept for later. In an
    /// encore and in the program the tick only counts the offer's time, and the one that ends it takes the leftmost
    /// card: the act's own time stands in an encore as under the curtain. Between two acts and when the performance
    /// is over the world stands: the tick reports nothing and changes nothing.
    /// </summary>
    public void Step(MagicianInput input)
    {
        _events.Clear();
        if (Phase == Phase.Curtain)
        {
            _curtainTicksLeft--;
            return;
        }

        if (Phase is Phase.Encore or Phase.Program)
        {
            if (--OfferTicksLeft <= 0)
            {
                Pick(0);
            }

            return;
        }

        if (Phase != Phase.Act)
        {
            return;
        }

        // The clouds thin before the magician can leave a new one, so a cloud has its whole time on the tick it is
        // left. And the magician moves before the critics walk, so the cloud of a Vanish stuns on the tick of it, and
        // a critic turns on the magician, and touches it, where this tick has put it.
        // The applause fades as the clouds thin, before any can be dropped: a piece has its whole time on the tick
        // it is dropped.
        ThinTheClouds();
        _applause.RemoveAll(piece => --piece.TicksLeft <= 0);
        if (MagicianHasFallen)
        {
            // It lies where it fell, whatever is asked of it: there is nothing behind it for the view to draw.
            MagicianPreviousPosition = MagicianPosition;
        }
        else
        {
            MoveTheMagician(input);
            PickUpApplause();
        }

        // The understudies take their places right after the magician has moved, for the magician's own reasons:
        // the cloud of an understudy's Vanish stuns on the tick of it, and an understudy throws from where this
        // tick has put it.
        PlaceTheUnderstudies();

        // The cards fly before the critics walk: a card meets the critics where the last tick left them. And they fly
        // before the magician throws, so a card thrown this tick does not fly this tick: it is first seen where it
        // was thrown from, and takes its first step on the next tick.
        FlyTheCards();
        WalkTheCritics();

        // The magician throws first, then each understudy in the order of their acts: the cards fly in the order
        // they were thrown, so of the cards thrown on one tick that reach one critic together the magician's own
        // lands first, and that fall is its own. A card an understudy threw a tick earlier flies before it. Once a blow of this tick has closed the show nobody throws, and a
        // magician that a blow of this tick has felled does not.
        if (!ShowClosed && !MagicianHasFallen)
        {
            _ticksToNextThrow = ThrowACard(MagicianPosition, _ticksToNextThrow, MagicianCards, TickEvent.TheMagician);
        }

        for (int i = 0; i < _understudies.Count; i++)
        {
            Understudy understudy = _understudies[i];
            if (understudy.IsOnStage && !ShowClosed)
            {
                understudy.TicksToNextThrow = ThrowACard(
                    understudy.Position, understudy.TicksToNextThrow, understudy.Cards, thrower: i);
            }
        }

        // Whoever the plan has for this tick of the act enters last, and is first seen where it entered.
        LetTheCriticsIn();
        _ticksPlayed++;
        _actTicksPlayed++;

        // The act's time is counted last: its last tick is played in full, and the act is over when that tick is,
        // whatever is on the stage. A blow of that very tick may still have closed the show.
        ActTicksLeft--;

        // An act that is over and has another after it has its program when an encore was taken in it: the chorus
        // card, alone. Not one that closed the show on its last tick, nor the last act of the performance. An act
        // with no encore has none: the chorus grows by the magician's applause and never without it.
        if (Phase == Phase.BetweenActs)
        {
            if (ActEncores > 0)
            {
                _offer.Add(Card.ChorusDamage);
                OfferTicksLeft = Ticks(Tuning.ProgramTime);
            }
        }

        // An act that goes on stands for an encore when its applause has reached the cost of one: asked when the
        // tick is over, so the tick of the piece is played in full, and after every tick, so what one encore left
        // over opens the next a tick of the act later. A magician that a blow of this tick felled has none.
        else if (Phase == Phase.Act && !MagicianHasFallen && EncoreApplause >= EncoreCost)
        {
            OfferAnEncore();
        }
    }

    /// <summary>
    /// Between two acts, begins the next one, with its curtain: the magician is whole and on its mark, facing the
    /// audience, with the Vanish ready. Everything else on the stage is as the last act left it, the critics too
    /// (plan decision 17). In any other phase this does nothing: a program waits for its <see cref="Pick"/>, and
    /// an act with no encore has no program to wait for.
    /// The encores of the act just over stay taken, and make those of the next cost more.
    /// </summary>
    public void GoOn()
    {
        if (Phase != Phase.BetweenActs)
        {
            return;
        }

        // The act just over is an understudy from now on, and the next act's recording starts empty. Every
        // understudy stands at the start of its route, with its throw ready: for the view there is nothing between
        // that place and where the last act left it.
        // It begins every act with the cards its own began with (plan decision 20), and gains those of its act's
        // encores on their ticks (plan T24): placed on the first tick, it has only the first.
        _understudies.Add(new Understudy(Act, _route, _vanishes, _recordingCards, _encores));
        _recordingCards = MagicianCards;
        _route = [];
        _vanishes = [];
        _encores = [];
        _actTicksPlayed = 0;
        foreach (Understudy understudy in _understudies)
        {
            Place(understudy, tick: 0);
            understudy.PreviousPosition = understudy.Position;
            understudy.TicksToNextThrow = 0;
        }

        // Nor is there anything behind a critic or a card: the view draws nothing of the next act from where the
        // last tick of the old one found them.
        foreach (Critic critic in _critics)
        {
            critic.PreviousPosition = critic.Position;
        }

        foreach (ThrownCard card in _thrownCards)
        {
            card.PreviousPosition = card.Position;
        }

        // Applause is not banked (vision 7.1): what the last act picked up is forgotten, and what it left lying
        // is gone.
        _applause.Clear();
        ActApplause = 0;
        EncoreApplause = 0;
        ActEncores = 0;

        Act++;
        ActTicksLeft = Ticks(Tuning.ActLength);
        ActEntriesMade = 0;
        _curtainTicksLeft = Ticks(Tuning.CurtainTime);

        // Both positions: there is nothing between where the magician stood and the mark for the view to draw.
        MagicianPosition = Tuning.MagicianMark;
        MagicianPreviousPosition = Tuning.MagicianMark;
        MagicianHitPoints = Tuning.MagicianHitPoints;
        _facing = Vector2.UnitY;
        _ticksToNextVanish = 0;
        _ticksInvulnerable = 0;
    }

    /// <summary>
    /// A 64-bit FNV-1a hash of everything that decides what happens next: two shows with one hash go on alike under
    /// the same inputs and the same tuning. What only the view reads (the positions before the last tick, the
    /// events) is not in it.
    /// </summary>
    public ulong ComputeStateHash()
    {
        var hasher = new Fnv1a64();

        void AddPoint(Vector2 point)
        {
            hasher.AddFloat(point.X);
            hasher.AddFloat(point.Y);
        }

        void AddRecording(
            IReadOnlyList<Vector2> route,
            IReadOnlyList<(int Tick, Vector2 Place)> vanishes,
            SelfCards cards,
            IReadOnlyList<(int Tick, Card Card)> encores)
        {
            hasher.AddInt(route.Count);
            foreach (Vector2 place in route)
            {
                AddPoint(place);
            }

            hasher.AddInt(vanishes.Count);
            foreach ((int tick, Vector2 place) in vanishes)
            {
                hasher.AddInt(tick);
                AddPoint(place);
            }

            AddCards(cards);
            hasher.AddInt(encores.Count);
            foreach ((int tick, Card card) in encores)
            {
                hasher.AddInt(tick);
                hasher.AddInt((int)card);
            }
        }

        void AddCards(SelfCards cards)
        {
            hasher.AddInt(cards.Damage);
            hasher.AddInt(cards.AttackSpeed);
            hasher.AddInt(cards.Range);
            hasher.AddInt(cards.VanishCooldown);
            hasher.AddInt(cards.OneMoreCard);
        }

        hasher.AddInt(_ticksPlayed);
        hasher.AddInt(_actTicksPlayed);

        // The phase beside the act's number and time: whether an act that is over was the last is not in those.
        hasher.AddInt((int)Phase);
        hasher.AddInt(Act);
        hasher.AddInt(ActTicksLeft);
        hasher.AddInt(_curtainTicksLeft);
        AddPoint(MagicianPosition);
        AddPoint(_facing);
        hasher.AddInt(_ticksToNextVanish);
        hasher.AddInt(_ticksInvulnerable);
        hasher.AddFloat(MagicianHitPoints);
        hasher.AddFloat(BoxOfficeHitPoints);

        // Each list after its length, so that where one ends and the next begins is never in doubt.
        hasher.AddInt(_critics.Count);
        foreach (Critic critic in _critics)
        {
            hasher.AddInt(critic.Id);
            hasher.AddInt(critic.Kind);
            AddPoint(critic.Position);
            hasher.AddFloat(critic.HitPoints);
            hasher.AddInt(critic.TicksToNextBlow);
            hasher.AddInt(critic.TicksStunned);
        }

        hasher.AddInt(_thrownCards.Count);
        foreach (ThrownCard card in _thrownCards)
        {
            AddPoint(card.Position);
            AddPoint(card.Direction);
            hasher.AddFloat(card.RangeLeft);
            hasher.AddFloat(card.Damage);

            // Whether the magician threw it decides the applause; which understudy did decides nothing and is
            // only told in the events, so it stays out until a rule reads it.
            hasher.AddInt(card.ThrownByMagician ? 1 : 0);
        }

        hasher.AddInt(_clouds.Count);
        foreach (Cloud cloud in _clouds)
        {
            AddPoint(cloud.Position);
            hasher.AddInt(cloud.TicksLeft);
        }

        hasher.AddInt(_applause.Count);
        foreach (Applause piece in _applause)
        {
            AddPoint(piece.Position);
            hasher.AddInt(piece.TicksLeft);
        }

        // No test tells EncoresTaken, ActEncores, the encore stream's state or the offer's cards apart from the rest
        // of the hash: as the rule stands EncoresTaken is the number of the magician's self cards, the stream and
        // the offer follow from the seed and that number, and ActEncores is the number of the recording's encores.
        // Plan T24 broke none of those. Of what it added, the tick of a recorded encore and its card each have a
        // test (the same encore on two ticks; two encores of one act taken in two orders). The number of a
        // recording's encores and the cards an understudy begins with are told apart only together with those,
        // and the cards an understudy has now follow from the rest. Whatever changes that adds the test.
        hasher.AddInt(ActApplause);
        hasher.AddInt(EncoreApplause);
        hasher.AddInt(EncoresTaken);
        hasher.AddInt(ActEncores);

        // Every recording whole, an understudy's and that of the act that is played, which is the next
        // understudy: where everybody stands now does not say where each will stand a tick from now.
        // ponytail: the hash walks every place of every act on every call, 45,000 of them by the tenth act. It is
        // for the tests and the scripted players, not for the game's frame; a hash kept per finished recording
        // replaces the walk when something asks for the hash on every tick.
        hasher.AddInt(_understudies.Count);
        foreach (Understudy understudy in _understudies)
        {
            hasher.AddInt(understudy.Act);
            AddCards(understudy.Cards);
            AddPoint(understudy.Position);
            hasher.AddInt(understudy.TicksToNextThrow);
            hasher.AddInt(understudy.IsOnStage ? 1 : 0);
            AddRecording(understudy.Route, understudy.Vanishes, understudy.FirstCards, understudy.Encores);
        }

        AddRecording(_route, _vanishes, _recordingCards, _encores);
        hasher.AddInt(_ticksToNextThrow);

        // The cards taken, and an offer that is up: what it has and how long it still waits.
        AddCards(MagicianCards);
        hasher.AddInt(ChorusCards);
        hasher.AddInt(_offer.Count);
        foreach (Card card in _offer)
        {
            hasher.AddInt((int)card);
        }

        hasher.AddInt(OfferTicksLeft);

        // How far through the act's entries the show is. The plan itself is not in the hash: it follows from the
        // seed and the tuning, which two shows that are compared share.
        hasher.AddInt(ActEntriesMade);
        hasher.AddInt(_criticsEntered);
        hasher.AddULong(_doorPlaces.State);
        hasher.AddULong(_encore.State);
        return hasher.Value;
    }

    /// <summary>
    /// The time from one Vanish of the magician to its next, in ticks: the tuning's cooldown, shortened by the
    /// magician's cards.
    /// </summary>
    private int VanishCooldownTicks =>
        Ticks(Tuning.VanishCooldown / (1f + (MagicianCards.VanishCooldown * Tuning.CardVanishCooldown)));

    /// <summary>
    /// Opens an encore: three self cards, each any of those still in the pile, as likely as another.
    /// </summary>
    private void OfferAnEncore()
    {
        List<Card> pile = [Card.Damage, Card.AttackSpeed, Card.Range, Card.VanishCooldown, Card.OneMoreCard];
        for (int place = 0; place < 3; place++)
        {
            int drawn = _encore.NextInt(pile.Count);
            _offer.Add(pile[drawn]);
            pile.RemoveAt(drawn);
        }

        OfferTicksLeft = Ticks(Tuning.EncoreTime);
    }

    private void ThinTheClouds() => _clouds.RemoveAll(cloud => --cloud.TicksLeft <= 0);

    private void MoveTheMagician(MagicianInput input)
    {
        // A keyboard diagonal is no faster than a straight line; a stick pushed halfway stays at half speed.
        Vector2 move = input.Move;
        float lengthSquared = (move.X * move.X) + (move.Y * move.Y);
        if (lengthSquared > 0f)
        {
            // The magician faces the way it was last asked to go, however hard the stick was pushed.
            // ponytail: an input so short that its square is less than a float holds (about 1e-19 long) gives a
            // facing shorter than one unit, and a short blink after it. No key and no stick gives such an input.
            _facing = move / MathF.Sqrt(lengthSquared);
        }

        if (lengthSquared > 1f)
        {
            move = _facing;
        }

        if (_ticksToNextVanish > 0)
        {
            _ticksToNextVanish--;
        }

        if (_ticksInvulnerable > 0)
        {
            _ticksInvulnerable--;
        }

        // The Vanish is ready a cooldown after the last one. A press before that is refused, and not kept for later.
        Vector2 from = MagicianPosition;
        Vector2 step = move * (Tuning.MagicianSpeed / TicksPerSecond);
        bool vanishes = input.Vanish && _ticksToNextVanish == 0;
        if (vanishes)
        {
            // A blink and not a run: it is the tick's whole move, with no step of the walk added.
            step = _facing * Tuning.VanishDistance;
            _ticksToNextVanish = VanishCooldownTicks;
            _ticksInvulnerable = Ticks(Tuning.VanishInvulnerableTime);
            _events.Add(new TickEvent(TickEventKind.Vanish, from));
            LeaveACloud(from);
            _vanishes.Add((_actTicksPlayed, from));
        }

        // The floor's edge stops the magician, a walk and a blink alike: the whole circle stays on the floor, which
        // starts at the foot of the back wall and ends at the stage's other three edges.
        var radius = new Vector2(Tuning.MagicianRadius);
        var floorTopLeft = new Vector2(0f, Tuning.StageFloorTop);
        MagicianPosition = Vector2.Clamp(from + step, floorTopLeft + radius, Tuning.StageSize - radius);

        // The view draws the magician between the two: after a blink there is nothing between them to draw.
        MagicianPreviousPosition = vanishes ? MagicianPosition : from;

        // The act is recorded as places and not as inputs (plan decision 6): nothing that happens to be on the
        // stage in a later act can take an understudy off its route.
        _route.Add(MagicianPosition);
    }

    /// <summary>
    /// The magician picks up every piece its circle reaches, from where this tick has put it. Nobody else does: an
    /// understudy walks over applause and leaves it.
    /// </summary>
    private void PickUpApplause()
    {
        float reach = Tuning.MagicianRadius + Tuning.ApplausePickUpReach;
        for (int i = 0; i < _applause.Count; i++)
        {
            Vector2 apart = _applause[i].Position - MagicianPosition;
            if ((apart.X * apart.X) + (apart.Y * apart.Y) <= reach * reach)
            {
                _events.Add(new TickEvent(TickEventKind.ApplausePickedUp, _applause[i].Position));
                _applause.RemoveAt(i--);
                ActApplause++;
                EncoreApplause++;
            }
        }
    }

    private void PlaceTheUnderstudies()
    {
        // The act's own count and not the length of its recording, which stops when the magician falls: the
        // understudies play on.
        int tick = _actTicksPlayed;
        foreach (Understudy understudy in _understudies)
        {
            Place(understudy, tick);

            // Its Vanish is the cloud, left where the magician's was. The blink is in the route already: the view
            // has nothing to draw between its two ends. No Vanish is reported: that event is the magician's own.
            // ponytail: every Vanish of every understudy is looked at on every tick. A Vanish every three seconds
            // of a 75-second act is 25 an understudy; an index of the next one in each replaces the scan when
            // that is too many.
            foreach ((int vanishTick, Vector2 place) in understudy.Vanishes)
            {
                if (vanishTick == tick)
                {
                    LeaveACloud(place);
                    understudy.PreviousPosition = understudy.Position;
                }
            }
        }
    }

    /// <summary>
    /// Puts an understudy where its route says for a tick of the act, the first being 0, with the cards the
    /// magician played that tick with: those its act began with and those of the encores taken by then. A route
    /// with no place for that tick leaves it where it is, and off the stage.
    /// </summary>
    private static void Place(Understudy understudy, int tick)
    {
        // Worked out afresh from the recording on every tick, so there is nothing to set back when an act begins.
        // The encores are in the order they were taken: the first that is still to come ends the walk.
        // ponytail: every understudy walks its encores up to now on every tick, some twenty at most each; an index
        // of the next one in each replaces the walk when an act holds hundreds.
        SelfCards cards = understudy.FirstCards;
        for (int i = 0; i < understudy.Encores.Count && understudy.Encores[i].Tick <= tick; i++)
        {
            cards = cards.With(understudy.Encores[i].Card);
        }

        understudy.Cards = cards;
        understudy.IsOnStage = tick < understudy.Route.Count;
        if (understudy.IsOnStage)
        {
            understudy.PreviousPosition = understudy.Position;
            understudy.Position = understudy.Route[tick];
        }
    }

    /// <summary>
    /// The cloud of a Vanish, kept apart from the blink: a Vanish leaves one where it began. A cloud with no time is
    /// no cloud.
    /// </summary>
    private void LeaveACloud(Vector2 position)
    {
        int ticks = Ticks(Tuning.VanishCloudTime);
        if (ticks > 0)
        {
            _clouds.Add(new Cloud(position, ticks));
        }
    }

    private void FlyTheCards()
    {
        for (int i = 0; i < _thrownCards.Count; i++)
        {
            // Straight on, a tick's worth or what is left of the throw's range.
            ThrownCard card = _thrownCards[i];
            float step = MathF.Min(Tuning.ThrownCardSpeed / TicksPerSecond, card.RangeLeft);
            Critic? touched = FirstCriticOnThePath(card.Position, card.Direction, step);
            card.PreviousPosition = card.Position;
            card.Position += card.Direction * step;
            card.RangeLeft -= step;

            // The card is spent on the first critic it touches, which need not be the one it was thrown at.
            if (touched is not null)
            {
                touched.HitPoints -= card.Damage;
                bool fell = touched.HitPoints <= 0f;
                if (fell)
                {
                    _critics.Remove(touched);
                }

                _events.Add(new TickEvent(
                    fell ? TickEventKind.Kill : TickEventKind.Hit, touched.Position, card.Thrower, touched.Id));

                // The audience cheers the star and never the cardboard: applause is left where a critic falls to
                // a card the magician itself threw, whoever hurt the critic before. A piece with no time is no
                // piece.
                int ticks = Ticks(Tuning.ApplauseTime);
                // And none by the box office: a critic that falls within the radius of its middle leaves nothing,
                // so that what is thrown from beside the box office at what has come to it earns no encore.
                Vector2 fromBoxOffice = touched.Position - Tuning.BoxOfficePosition;
                bool byTheBoxOffice = (fromBoxOffice.X * fromBoxOffice.X) + (fromBoxOffice.Y * fromBoxOffice.Y)
                    < Tuning.ApplauseBoxOfficeRadius * Tuning.ApplauseBoxOfficeRadius;
                if (fell && card.ThrownByMagician && ticks > 0 && !byTheBoxOffice)
                {
                    _applause.Add(new Applause(touched.Position, ticks));
                    _events.Add(new TickEvent(TickEventKind.ApplauseDropped, touched.Position, CriticId: touched.Id));
                }
            }

            if (touched is not null || card.RangeLeft <= 0f)
            {
                _thrownCards.RemoveAt(i--);
            }
        }
    }

    /// <summary>
    /// The critic whose circle a card comes to first on its way from <paramref name="start"/>,
    /// <paramref name="length"/> units along <paramref name="direction"/>. The whole path is looked at and not only
    /// its end, so a card fast enough to jump over a critic in one tick touches it all the same.
    /// </summary>
    private Critic? FirstCriticOnThePath(Vector2 start, Vector2 direction, float length)
    {
        Critic? first = null;
        float firstEntry = float.PositiveInfinity;
        foreach (Critic critic in _critics)
        {
            // How far along the path the critic's centre is, and how far to the side of it.
            Vector2 toCritic = critic.Position - start;
            float along = (toCritic.X * direction.X) + (toCritic.Y * direction.Y);
            float aside = (toCritic.X * direction.Y) - (toCritic.Y * direction.X);
            float radius = KindOf(critic).Radius;
            float halfChordSquared = (radius * radius) - (aside * aside);
            if (halfChordSquared < 0f)
            {
                continue;
            }

            // The path's line is inside the circle from `entry` to `exit`: the path touches the circle when some of
            // that stretch is on it. A card that starts inside a circle is in it from its first step, however far
            // the circle reaches back: of several such circles (the magician in a squeezed crowd) the critic that
            // entered first is the one hurt, and never one for lying further behind the card.
            float halfChord = MathF.Sqrt(halfChordSquared);
            float entry = MathF.Max(0f, along - halfChord);
            float exit = along + halfChord;
            if (exit >= 0f && entry <= length && entry < firstEntry)
            {
                first = critic;
                firstEntry = entry;
            }
        }

        return first;
    }

    private void WalkTheCritics()
    {
        foreach (Critic critic in _critics)
        {
            critic.PreviousPosition = critic.Position;
        }

        // Critics whose circles overlap push each other apart, half the overlap each, in the order they entered.
        // One pass a tick is soft: those who walk in from behind squeeze a crowd, and it spreads round the box
        // office into a clump.
        // ponytail: every pair is looked at, 4950 of them for the hundred critics one screen is meant to hold; a
        // grid of cells one critic wide replaces the two loops when there are more. And one pass squeezes harder the
        // bigger the crowd (the closest two of forty overlap by a third, of a hundred and twenty by half); more
        // passes a tick are what loosens it.
        for (int i = 0; i < _critics.Count; i++)
        {
            for (int j = i + 1; j < _critics.Count; j++)
            {
                float touching = KindOf(_critics[i]).Radius + KindOf(_critics[j]).Radius;
                Vector2 apart = Direction(_critics[j].Position - _critics[i].Position, out float distance);
                if (distance < touching)
                {
                    Vector2 push = apart * ((touching - distance) / 2f);
                    _critics[i].Position -= push;
                    _critics[j].Position += push;
                }
            }
        }

        foreach (Critic critic in _critics)
        {
            // A critic whose circle touches a cloud is stunned, for the stun's time counted from this tick: one
            // that stays in a cloud is stunned anew on every tick of it.
            if (critic.TicksStunned > 0)
            {
                critic.TicksStunned--;
            }

            EnemyKind kind = KindOf(critic);
            if (TouchesACloud(critic.Position, kind.Radius))
            {
                critic.TicksStunned = Ticks(Tuning.VanishStunTime);
            }

            // A critic nearer to the magician than the turn radius has turned on it: it walks at the magician and
            // not at the box office. That is asked anew on every tick, so it goes back to the box office the moment
            // the magician is out of the radius. A stunned critic does not turn, nor one of a kind that never does,
            // and nobody turns on a magician that has fallen: from the blow that felled it on, in this very tick,
            // it is not there for a critic.
            Vector2 apart = MagicianPosition - critic.Position;
            bool turned = kind.TurnsOnTheMagician
                && !critic.IsStunned
                && !MagicianHasFallen
                && (apart.X * apart.X) + (apart.Y * apart.Y) < Tuning.CriticTurnRadius * Tuning.CriticTurnRadius;

            // Straight at whichever it is, as far as where the two circles touch. From the box office a critic is
            // put back out to there when the crowd has pushed it in; a stunned critic walks no step, and is put back
            // out all the same: the others still push it. From the magician nobody is put back out: nothing blocks
            // the magician, which walks through critics and leaves them where they stand.
            // ponytail: a critic that has turned walks in a straight line, through the box office when that is
            // between the two, and is not put back out of it while it is turned, so one that turns back is put out
            // in a single tick, however deep it stood. Were the box office a wall to it, nobody could reach a
            // magician that stands inside the box office; it becomes a wall to every critic when something keeps
            // the magician out of it too.
            Vector2 toTarget = Direction(
                (turned ? MagicianPosition : Tuning.BoxOfficePosition) - critic.Position, out float distance);
            float gap = distance - (turned ? Tuning.MagicianRadius : Tuning.BoxOfficeSize / 2f) - kind.Radius;
            float step = critic.IsStunned ? 0f : kind.Speed / TicksPerSecond;
            float walk = MathF.Min(gap, step);
            critic.Position += toTarget * (turned ? MathF.Max(0f, walk) : walk);

            // Nor does it deal a blow, and its time to the next blow stands still.
            if (critic.IsStunned)
            {
                continue;
            }

            // A critic that touches what it walks at deals it a blow, and its next blow a cooldown later, whichever
            // of the two that one is for: turning from one to the other never brings a blow sooner.
            if (critic.TicksToNextBlow > 0)
            {
                critic.TicksToNextBlow--;
            }

            // While nothing hurts the magician a touch takes nothing and is no blow: the critic's stays ready.
            // And once a blow of this tick has closed the show, the critics after it in the list deal none.
            if (ShowClosed || gap > step || critic.TicksToNextBlow > 0 || (turned && MagicianIsInvulnerable))
            {
                continue;
            }

            if (turned)
            {
                MagicianHitPoints = MathF.Max(0f, MagicianHitPoints - Tuning.CriticTouchDamage);
                _events.Add(new TickEvent(TickEventKind.MagicianHurt, MagicianPosition));
                if (MagicianHasFallen)
                {
                    _events.Add(new TickEvent(TickEventKind.MagicianFell, MagicianPosition));
                }
            }
            else
            {
                BoxOfficeHitPoints = MathF.Max(0f, BoxOfficeHitPoints - Tuning.CriticStrikeDamage);
                _events.Add(new TickEvent(TickEventKind.BoxOfficeStruck, critic.Position));
            }

            critic.TicksToNextBlow = Ticks(Tuning.CriticBlowCooldown);
        }
    }

    /// <summary>
    /// A critic's circle of <paramref name="radius"/> with its middle at <paramref name="critic"/> touches some
    /// cloud.
    /// </summary>
    private bool TouchesACloud(Vector2 critic, float radius)
    {
        float reach = Tuning.VanishCloudRadius + radius;
        foreach (Cloud cloud in _clouds)
        {
            Vector2 apart = critic - cloud.Position;
            if ((apart.X * apart.X) + (apart.Y * apart.Y) <= reach * reach)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The throw of whoever stands at <paramref name="from"/>, the magician or an understudy: one rule for both,
    /// on the tuning's numbers as the thrower's own <paramref name="cards"/> change them, and for an understudy
    /// the chorus cards too. It is given the thrower's countdown to its next throw and gives back what that is
    /// after this tick. The <paramref name="thrower"/> is <see cref="TickEvent.TheMagician"/> or the understudy's
    /// place among the understudies, and is told by the throw's event and by the card.
    /// </summary>
    private int ThrowACard(Vector2 from, int ticksToNextThrow, SelfCards cards, int thrower)
    {
        // The throw is ready a cooldown after the last one, and stays ready while there is nobody to throw at.
        if (ticksToNextThrow > 0)
        {
            ticksToNextThrow--;
        }

        if (ticksToNextThrow > 0)
        {
            return ticksToNextThrow;
        }

        float range = Tuning.ThrowRange + (cards.Range * Tuning.CardRange);
        float damage = Tuning.ThrownCardDamage
            + (cards.Damage * Tuning.CardDamage)
            + (thrower == TickEvent.TheMagician ? 0f : ChorusCards * Tuning.CardChorusDamage);

        // One card, and one more for each card of that name, each at a critic of its own: the first at the nearest
        // whose centre is in range, the next at the nearest of the rest, and of two as near at the one that entered
        // first. With fewer critics in range than cards the rest of the cards are not thrown.
        // ponytail: every critic is looked at once for each card of the throw. A throw of many cards at a full
        // stage sorts the critics in range once instead.
        _targets.Clear();
        for (int thrown = 0; thrown <= cards.OneMoreCard; thrown++)
        {
            Critic? target = null;
            float nearest = float.PositiveInfinity;
            foreach (Critic critic in _critics)
            {
                Direction(critic.Position - from, out float distance);
                if (distance <= range && distance < nearest && !_targets.Contains(critic))
                {
                    target = critic;
                    nearest = distance;
                }
            }

            if (target is null)
            {
                break;
            }

            _targets.Add(target);

            // The card is thrown ahead of its target, at where the two meet if the target goes on as in its last step:
            // `to` away now and `step` further every tick, it is met after t ticks by a card that flies `speed` a tick
            // when |to + step x t| = speed x t. A target that stands has no step and is aimed at where it stands. The
            // step is this tick's own, made before anybody throws: nothing of an earlier tick is read here.
            // ponytail: the lead is of the first order, and the card flies straight. A target that stops, turns or is
            // pushed while the card is in the air (a stagehand that reaches the box office, a critic that turns on the
            // magician, a cloud) has left the meeting place by as much as it strays in that time, and is missed when
            // that is more than its radius: at most its speed x throwRange / thrownCardSpeed, 2 units for the
            // committed stagehand. A target near the end of the range that walks away is met past the range, where the
            // card has fallen. And a card first flies on the tick after its throw, so its target is up to one of its
            // own steps behind the aim: 0.13 of a unit for the committed stagehand, whose radius is 0.4. A slower card or a longer range makes both worse; a card that turns in the air is what
            // mends them.
            Vector2 to = target.Position - from;
            Vector2 step = target.Position - target.PreviousPosition;
            float speed = Tuning.ThrownCardSpeed / TicksPerSecond;
            float closing = (to.X * step.X) + (to.Y * step.Y);
            float faster = (speed * speed) - ((step.X * step.X) + (step.Y * step.Y));

            // A card no faster than its target need never meet it: that one too is aimed at where it stands.
            float ticks = faster > 0f
                ? (closing + MathF.Sqrt((closing * closing) + (faster * nearest * nearest))) / faster
                : 0f;
            Vector2 direction = Direction(to + (step * ticks), out _);
            _thrownCards.Add(new ThrownCard(from, direction, range, damage, thrower));
            _events.Add(new TickEvent(TickEventKind.Throw, from, thrower));
        }

        // An attack speed card adds its share to the rate of the throw: the time to the next is the cooldown over
        // one and those shares.
        return _targets.Count == 0
            ? 0
            : Ticks(Tuning.ThrowCooldown / (1f + (cards.AttackSpeed * Tuning.CardAttackSpeed)));
    }

    /// <summary>The file gives seconds and the rules count whole ticks: the nearest number of them.</summary>
    internal static int Ticks(float seconds) => (int)((seconds * TicksPerSecond) + 0.5f);

    /// <summary>
    /// Which way <paramref name="v"/> points, one unit long. A vector of no length points nowhere and is given a
    /// fixed direction, so that two critics on exactly one point still part, and the same way on every machine.
    /// </summary>
    private static Vector2 Direction(Vector2 v, out float length)
    {
        length = MathF.Sqrt((v.X * v.X) + (v.Y * v.Y));
        return length > 0f ? v / length : Vector2.UnitX;
    }

    /// <summary>
    /// What a critic is: its kind as the tuning has it now.
    /// </summary>
    // ponytail: a critic knows its kind, and an entry its door, by a place in the tuning's lists as they were when
    // the plan was made. A reload that reorders a list changes who is what, and one that shortens it leaves those
    // past its end as its last; the lists need names to be looked up by when a reload has to do better.
    private EnemyKind KindOf(Critic critic) => Tuning.EnemyKinds[Math.Min(critic.Kind, Tuning.EnemyKinds.Count - 1)];

    private void LetTheCriticsIn()
    {
        IReadOnlyList<PlannedEntry> entries = ActEntries;
        while (ActEntriesMade < entries.Count && entries[ActEntriesMade].Tick <= _actTicksPlayed)
        {
            LetACriticIn(entries[ActEntriesMade++]);
        }
    }

    private void LetACriticIn(PlannedEntry entry)
    {
        // A door in a side edge runs up and down it; any other, at the foot of the back wall or in the bottom edge,
        // runs along the stage's width.
        Vector2 door = Tuning.StageDoors[Math.Min(entry.Door, Tuning.StageDoors.Count - 1)].Position;
        bool inASide = door.X <= 0f || door.X >= Tuning.StageSize.X;
        float along = (_doorPlaces.NextFloat() - 0.5f) * Tuning.StageDoorWidth;
        Vector2 position = door + (inASide ? new Vector2(0f, along) : new Vector2(along, 0f));
        var critic = new Critic(_criticsEntered++, entry.Kind, position);
        critic.HitPoints = KindOf(critic).HitPoints;
        _critics.Add(critic);
    }
}
