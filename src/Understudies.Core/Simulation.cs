using System.Numerics;
using Understudies.Core.Hashing;
using Understudies.Core.Randomness;

namespace Understudies.Core;

/// <summary>Owns the state of the game and advances it one tick at a time.</summary>
/// <param name="seed">What is left to chance in the show comes from this: the same seed, the same show.</param>
public sealed class Simulation(Tuning tuning, ulong seed)
{
    public const int TicksPerSecond = 60;

    private readonly Rng _rng = new(seed);
    private readonly List<Critic> _critics = [];
    private readonly List<ThrownCard> _thrownCards = [];
    private readonly List<Cloud> _clouds = [];
    private readonly List<TickEvent> _events = [];
    private int _ticksPlayed;
    private int _criticsEntered;
    private int _ticksToNextCritic;
    private int _ticksToNextThrow;

    // One unit long: the way the magician was last asked to walk. When the curtain rises it faces the audience,
    // down the stage.
    private Vector2 _facing = Vector2.UnitY;
    private int _ticksToNextVanish;
    private int _ticksInvulnerable;

    /// <summary>
    /// The numbers the rules run on. New ones may be set between ticks: the state stays as it is and the next tick
    /// runs on them.
    /// </summary>
    public Tuning Tuning { get; set; } = tuning;

    /// <summary>The middle of the magician's circle on the floor, after the last tick.</summary>
    public Vector2 MagicianPosition { get; private set; } = tuning.MagicianMark;

    /// <summary>Where the magician was before the last tick: the view draws between the two.</summary>
    public Vector2 MagicianPreviousPosition { get; private set; } = tuning.MagicianMark;

    /// <summary>
    /// How much of the Vanish's cooldown is left, as a share of the whole: 1 on the tick of a Vanish, and one
    /// tick's share, not yet 0, when the very next tick would take a press again.
    /// </summary>
    public float VanishCooldownLeft =>
        _ticksToNextVanish == 0 ? 0f : MathF.Min(1f, (float)_ticksToNextVanish / Ticks(Tuning.VanishCooldown));

    /// <summary>Nothing hurts the magician now: a moment that starts with a Vanish.</summary>
    public bool MagicianIsInvulnerable => _ticksInvulnerable > 0;

    /// <summary>The critics on the stage, in the order they entered.</summary>
    public IReadOnlyList<Critic> Critics => _critics;

    /// <summary>The cards in the air, in the order they were thrown.</summary>
    public IReadOnlyList<ThrownCard> ThrownCards => _thrownCards;

    /// <summary>The clouds on the floor, in the order they were left.</summary>
    public IReadOnlyList<Cloud> Clouds => _clouds;

    /// <summary>What happened in the last tick, in the order it happened. The next tick starts the list afresh.</summary>
    public IReadOnlyList<TickEvent> Events => _events;

    /// <summary>What the magician has left: never less than nothing.</summary>
    public float MagicianHitPoints { get; private set; } = tuning.MagicianHitPoints;

    /// <summary>The magician has nothing left.</summary>
    public bool MagicianHasFallen => MagicianHitPoints <= 0f;

    /// <summary>What the box office has left: never less than nothing.</summary>
    public float BoxOfficeHitPoints { get; private set; } = tuning.BoxOfficeHitPoints;

    /// <summary>
    /// The box office has nothing left, or the magician has fallen. The show is over: <see cref="Step"/> changes
    /// nothing any more.
    /// </summary>
    public bool ShowClosed => BoxOfficeHitPoints <= 0f || MagicianHasFallen;

    public void Step(MagicianInput input)
    {
        _events.Clear();
        if (ShowClosed)
        {
            return;
        }

        // The clouds thin before the magician can leave a new one, so a cloud has its whole time on the tick it is
        // left. And the magician moves before the critics walk, so the cloud of a Vanish stuns on the tick of it, and
        // a critic turns on the magician, and touches it, where this tick has put it.
        ThinTheClouds();
        MoveTheMagician(input);

        // The cards fly before the critics walk: a card meets the critics where the last tick left them. And they fly
        // before the magician throws, so a card thrown this tick does not fly this tick: it is first seen where it
        // was thrown from, and takes its first step on the next tick.
        FlyTheCards();
        WalkTheCritics();
        ThrowACard();

        // The first critic enters on the first tick.
        if (--_ticksToNextCritic <= 0)
        {
            LetACriticIn();
            _ticksToNextCritic = Ticks(Tuning.CriticEntryInterval);
        }

        _ticksPlayed++;
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

        hasher.AddInt(_ticksPlayed);
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
        }

        hasher.AddInt(_clouds.Count);
        foreach (Cloud cloud in _clouds)
        {
            AddPoint(cloud.Position);
            hasher.AddInt(cloud.TicksLeft);
        }

        hasher.AddInt(_ticksToNextThrow);
        hasher.AddInt(_ticksToNextCritic);
        hasher.AddInt(_criticsEntered);
        hasher.AddULong(_rng.State);
        return hasher.Value;
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
            _ticksToNextVanish = Ticks(Tuning.VanishCooldown);
            _ticksInvulnerable = Ticks(Tuning.VanishInvulnerableTime);
            _events.Add(new TickEvent(TickEventKind.Vanish, from));
            LeaveACloud(from);
        }

        // The floor's edge stops the magician, a walk and a blink alike: the whole circle stays on the floor, which
        // starts at the foot of the back wall and ends at the stage's other three edges.
        var radius = new Vector2(Tuning.MagicianRadius);
        var floorTopLeft = new Vector2(0f, Tuning.StageFloorTop);
        MagicianPosition = Vector2.Clamp(from + step, floorTopLeft + radius, Tuning.StageSize - radius);

        // The view draws the magician between the two: after a blink there is nothing between them to draw.
        MagicianPreviousPosition = vanishes ? MagicianPosition : from;
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
                touched.HitPoints -= Tuning.ThrownCardDamage;
                bool fell = touched.HitPoints <= 0f;
                if (fell)
                {
                    _critics.Remove(touched);
                }

                _events.Add(new TickEvent(fell ? TickEventKind.Kill : TickEventKind.Hit, touched.Position));
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
            float halfChordSquared = (Tuning.CriticRadius * Tuning.CriticRadius) - (aside * aside);
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
        float diameter = 2f * Tuning.CriticRadius;
        for (int i = 0; i < _critics.Count; i++)
        {
            for (int j = i + 1; j < _critics.Count; j++)
            {
                Vector2 apart = Direction(_critics[j].Position - _critics[i].Position, out float distance);
                if (distance < diameter)
                {
                    Vector2 push = apart * ((diameter - distance) / 2f);
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

            if (TouchesACloud(critic.Position))
            {
                critic.TicksStunned = Ticks(Tuning.VanishStunTime);
            }

            // A critic nearer to the magician than the turn radius has turned on it: it walks at the magician and
            // not at the box office. That is asked anew on every tick, so it goes back to the box office the moment
            // the magician is out of the radius. A stunned critic does not turn.
            Vector2 apart = MagicianPosition - critic.Position;
            bool turned = !critic.IsStunned
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
            float gap = distance - (turned ? Tuning.MagicianRadius : Tuning.BoxOfficeSize / 2f) - Tuning.CriticRadius;
            float step = critic.IsStunned ? 0f : Tuning.CriticSpeed / TicksPerSecond;
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
            }
            else
            {
                BoxOfficeHitPoints = MathF.Max(0f, BoxOfficeHitPoints - Tuning.CriticStrikeDamage);
                _events.Add(new TickEvent(TickEventKind.BoxOfficeStruck, critic.Position));
            }

            critic.TicksToNextBlow = Ticks(Tuning.CriticBlowCooldown);
        }
    }

    /// <summary>A critic's circle with its middle at <paramref name="critic"/> touches some cloud.</summary>
    private bool TouchesACloud(Vector2 critic)
    {
        float reach = Tuning.VanishCloudRadius + Tuning.CriticRadius;
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

    private void ThrowACard()
    {
        // The throw is ready a cooldown after the last one, and stays ready while there is nobody to throw at.
        if (_ticksToNextThrow > 0)
        {
            _ticksToNextThrow--;
        }

        if (_ticksToNextThrow > 0)
        {
            return;
        }

        // At the nearest critic whose centre is in range; of two as near, at the one that entered first.
        // ponytail: the card flies at where its target stands now, so it only hits what cannot leave that spot in
        // time: thrownCardSpeed must stay at or above criticSpeed x throwRange / criticRadius (72 with the
        // committed numbers, which is exactly where it is). A faster enemy or a longer range breaks that and
        // cards start to miss across the line of fire; the throw then has to aim ahead of its target.
        Vector2? aim = null;
        float nearest = float.PositiveInfinity;
        foreach (Critic critic in _critics)
        {
            Vector2 toCritic = Direction(critic.Position - MagicianPosition, out float distance);
            if (distance <= Tuning.ThrowRange && distance < nearest)
            {
                aim = toCritic;
                nearest = distance;
            }
        }

        if (aim is { } direction)
        {
            _thrownCards.Add(new ThrownCard(MagicianPosition, direction, Tuning.ThrowRange));
            _ticksToNextThrow = Ticks(Tuning.ThrowCooldown);
            _events.Add(new TickEvent(TickEventKind.Throw, MagicianPosition));
        }
    }

    /// <summary>The file gives seconds and the rules count whole ticks: the nearest number of them.</summary>
    private static int Ticks(float seconds) => (int)((seconds * TicksPerSecond) + 0.5f);

    /// <summary>
    /// Which way <paramref name="v"/> points, one unit long. A vector of no length points nowhere and is given a
    /// fixed direction, so that two critics on exactly one point still part, and the same way on every machine.
    /// </summary>
    private static Vector2 Direction(Vector2 v, out float length)
    {
        length = MathF.Sqrt((v.X * v.X) + (v.Y * v.Y));
        return length > 0f ? v / length : Vector2.UnitX;
    }

    private void LetACriticIn()
    {
        // A door in a side edge runs up and down it; any other, at the foot of the back wall or in the bottom edge,
        // runs along the stage's width.
        Vector2 door = Tuning.StageDoors[0];
        bool inASide = door.X <= 0f || door.X >= Tuning.StageSize.X;
        float along = (_rng.NextFloat() - 0.5f) * Tuning.StageDoorWidth;
        Vector2 position = door + (inASide ? new Vector2(0f, along) : new Vector2(along, 0f));
        _critics.Add(new Critic(_criticsEntered++, position, Tuning.CriticHitPoints));
    }
}
