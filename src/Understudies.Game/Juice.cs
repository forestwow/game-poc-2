using System.Numerics;
using Understudies.Core;

namespace Understudies.Game;

/// <summary>
/// What makes a blow felt: flashes, scraps, fading bodies, the shake and the hit-stop. All of it is the view's own
/// state and decides no rule, so it may use what the rules may not: <see cref="Random"/>, a dictionary,
/// trigonometry. It is fed the simulation after every tick (<see cref="Feed"/>: a tick's events are gone on the
/// next), advanced by the time that goes by (<see cref="Advance"/>), and asked by the drawing what to add. The
/// trail behind a thrown card needs no state, the drawing makes it of the card's last step: only its numbers are
/// here, with the rest.
/// </summary>
/// <param name="random">Where the scraps fly: a capture gives one that repeats.</param>
internal sealed class Juice(Random random)
{
    // Every number of the juice is here, to be turned by hand: none is a rule, so none is in tuning.json. Times are
    // in seconds and lengths in world units (a critic's footprint is one unit wide, and a unit is some 27 pixels of a
    // window 1280 wide).

    // A figure that was hurt is drawn white for FlashTime, and is not made to flash again for FlashRest after
    // that. A crowd deals many blows a second: what it beats blinks, under three times a second, and does not
    // flicker.
    private const float FlashTime = 0.08f;
    private const float FlashRest = 0.3f;

    // The box office is the biggest thing on the stage, and a crowd beats it for as long as it stands there: its
    // flash goes this far to white and not all the way.
    private const float BoxOfficeFlash = 0.5f;

    // A critic that falls bursts into this many scraps, and each end of a Vanish puffs this many.
    private const int KillScraps = 12;
    private const int VanishScraps = 8;

    // A piece of applause that is picked up puffs this many, and one that a scalper eats (plan T57) this many:
    // torn tickets, where the piece lay.
    private const int PickUpScraps = 6;
    private const int EatenScraps = 10;

    // A scrap starts at up to ScrapSpeed and at no less than ScrapSlowestShare of it, and loses speed at ScrapDrag
    // a second, so the fastest gets ScrapSpeed / ScrapDrag far. It is gone after ScrapTime, fading over the second
    // half of it. A burst starts ScrapLift above the floor, at a critic's chest.
    private const float ScrapSpeed = 12f;
    private const float ScrapSlowestShare = 0.4f;
    private const float ScrapDrag = 9f;
    private const float ScrapTime = 0.7f;
    private const float ScrapLift = 1f * UnderstudiesGame.FiguresMeasure;

    // What a card does is seen where it does it: a flick of light at the hand that throws it, a burst of its suits
    // where it strikes and a splash of ink and newsprint where its critic falls, each this long. They are at a
    // critic's chest, as the scraps are.
    private const float FlickTime = 0.1f;
    private const float HitBurstTime = 0.25f;
    private const float KillBurstTime = 0.5f;

    // The ring of a card's burst (plan T25) opens from this share of the burst's radius to all of it and goes
    // out, in this long: a line this thick, in this many straight stretches.
    private const float RingTime = 0.3f;
    public const float RingFirstShare = 0.4f;
    public const float RingLine = 0.15f;
    public const float RingOpacity = 0.8f;
    public const int RingStretches = 32;

    /// <summary>
    /// The splash is black ink and grey newsprint, and the boards are dark: under it, for the first
    /// <see cref="KillFlashShare"/> of its time, a pale disc this wide and this thick at first, going out.
    /// </summary>
    public const float KillFlashRadius = 1.1f * UnderstudiesGame.FiguresMeasure;
    public const float KillFlashOpacity = 0.75f;
    public const float KillFlashShare = 0.5f;

    /// <summary>
    /// An understudy's flick is the past's and not the present's: this much of the magician's in size and in
    /// brightness, and in the dull face of an understudy's card.
    /// </summary>
    public const float UnderstudyFlickSize = 0.7f;
    public const float UnderstudyFlickOpacity = 0.5f;

    // A fallen critic lies there this long, fading all the while.
    private const float BodyTime = 1.5f;

    // A blow adds its trauma, which is never more than 1 and falls by TraumaFade a second. The stage is moved by up
    // to ShakeReach, sideways and up or down, times the square of the trauma: one strike is a nudge of two thirds
    // of ShakeReach that is over in a quarter of a second, a touch on the magician one of under a third, and a
    // crowd's blows, many a second, keep the stage shaking by near the whole of it and by no more.
    private const float StrikeTrauma = 0.8f;
    private const float HurtTrauma = 0.55f;
    private const float TraumaFade = 3f;
    private const float ShakeReach = 0.2f;

    // The shaken stage is moved to a new place this many times a second of the juice's own time, however many
    // frames are drawn in it: the shake of a fast screen is the shake of a slow one, and a capture's the game's.
    private const float ShakesPerSecond = 60f;

    // The moment of a Vanish holds the world still for this long: three frames of sixty a second, whatever their
    // jitter. Three frames' worth to the hair (0.05) would hold three frames or four as the frames fell.
    private const float VanishHitStop = 0.045f;

    /// <summary>A scrap of paper, as it is drawn.</summary>
    public static readonly Vector2 ScrapSize = new(0.3f, 0.18f);

    /// <summary>
    /// The streak behind a thrown card: how long at most, how wide and how thick.
    /// </summary>
    public const float TrailLength = 3f;
    public const float TrailWidth = 0.15f;
    public const float TrailOpacity = 0.5f;

    // When each critic that a card has hurt last began to flash, by its id: a critic that falls is taken out.
    private readonly Dictionary<int, float> _criticFlashedAt = [];

    // How many self cards each understudy had when it was last looked at, in the order of their acts.
    private readonly List<int> _understudyCards = [];

    // The kind of every critic on the stage, by its id, as it was last fed: a kill names its critic, which is
    // gone from the stage by then, and its body is drawn as what it was. A critic that falls is taken out.
    private readonly Dictionary<int, int> _criticKinds = [];
    private readonly List<(Vector2 Position, int Kind, float FellAt)> _bodies = [];
    private readonly List<Scrap> _scraps = [];
    private readonly List<(Effect Kind, Vector2 Middle, float At)> _effects = [];

    // The juice's own time: what Advance has been given so far.
    // ponytail: a float, which stops telling one sixtieth of a second from the next when a show has been left open
    // for days. A double when a show can last that long.
    private float _now;
    private float _magicianFlashedAt = float.NegativeInfinity;
    private float _boxOfficeFlashedAt = float.NegativeInfinity;
    private float _trauma;
    private float _hitStopLeft;

    /// <summary>
    /// How far the whole stage is moved from its place now: told by the juice's time alone, the same place for
    /// every frame drawn within one of the <see cref="ShakesPerSecond"/>.
    /// </summary>
    public Vector2 Shake
    {
        get
        {
            uint beat = (uint)(_now * ShakesPerSecond);
            return new Vector2(Sway(2 * beat), Sway((2 * beat) + 1)) * (ShakeReach * _trauma * _trauma);
        }
    }

    /// <summary>How far to white the magician is drawn, from 0 to 1: more than 0 when it was hurt a moment ago.</summary>
    public float MagicianWhite => White(_magicianFlashedAt);

    /// <summary>The same for the box office, struck a moment ago.</summary>
    public float BoxOfficeWhite => White(_boxOfficeFlashedAt) * BoxOfficeFlash;

    /// <summary>The same for a critic that a card hurt a moment ago.</summary>
    public float CriticWhite(int id) => _criticFlashedAt.TryGetValue(id, out float flashedAt) ? White(flashedAt) : 0f;

    /// <summary>
    /// The critics that fell a moment ago: where each lies, its kind by the kind's place in the tuning, how far to
    /// white it is drawn (the blow that felled it flashes too), and how much of it is left to see.
    /// </summary>
    public IEnumerable<(Vector2 Position, int Kind, float White, float Opacity)> Bodies =>
        _bodies.Select(body => (body.Position, body.Kind, White(body.FellAt), 1f - ((_now - body.FellAt) / BodyTime)));

    /// <summary>The scraps in the air: the middle of each, how it is turned, and how much of it is left to see.</summary>
    public IEnumerable<(Vector2 Middle, float Turn, float Opacity)> Scraps => _scraps.Select(scrap =>
    {
        // A speed that falls by a share of itself all the time has carried the scrap this far by now.
        float age = _now - scrap.BurstAt;
        Vector2 flown = scrap.Velocity * ((1f - MathF.Exp(-ScrapDrag * age)) / ScrapDrag);
        return (scrap.From + flown, scrap.Turn, MathF.Min(1f, 2f * (1f - (age / ScrapTime))));
    });

    /// <summary>The cards' effects that are on the stage now, each with how far through its time it is, from 0 to 1.</summary>
    public IEnumerable<(Effect Kind, Vector2 Middle, float Through)> Effects =>
        _effects.Select(effect => (effect.Kind, effect.Middle, (_now - effect.At) / Lasts(effect.Kind)));

    /// <summary>Takes in what the last tick did. Called after every tick: a frame may run several.</summary>
    public void Feed(Simulation simulation)
    {
        // An understudy that has more cards than when it was last looked at has just gained one of its act's
        // encores (plan T24): a puff at it, as for a piece picked up. No event says so. An act that begins sets an
        // understudy back to the cards it began with, which is fewer and no puff.
        for (int i = 0; i < simulation.Understudies.Count; i++)
        {
            Understudy understudy = simulation.Understudies[i];
            SelfCards cards = understudy.Cards;
            int has = cards.Damage + cards.AttackSpeed + cards.Range + cards.VanishCooldown + cards.OneMoreCard
                + cards.Pierce + cards.Ricochet + cards.Burst;
            if (i == _understudyCards.Count)
            {
                _understudyCards.Add(has);
            }

            if (has > _understudyCards[i] && understudy.IsOnStage)
            {
                Burst(understudy.Position, PickUpScraps);
            }

            _understudyCards[i] = has;
        }

        foreach (TickEvent happened in simulation.Events)
        {
            switch (happened.Kind)
            {
                case TickEventKind.Throw:
                    Show(
                        happened.Thrower == TickEvent.TheMagician ? Effect.Flick : Effect.UnderstudyFlick,
                        happened.Position);
                    break;

                // The hit says which critic it hurt, and that one flashes.
                case TickEventKind.Hit:
                    _criticFlashedAt[happened.CriticId] =
                        FlashNow(_criticFlashedAt.GetValueOrDefault(happened.CriticId, float.NegativeInfinity));
                    Show(Effect.HitBurst, happened.Position);
                    break;

                // A fallen critic is a body, which flashes by the time it fell: its own flash is kept no longer.
                case TickEventKind.Kill:
                    _criticFlashedAt.Remove(happened.CriticId);

                    // Every critic was on the stage when some earlier tick was fed, since it enters last in its
                    // tick: one the juice never saw (fed a show under way) lies as the first kind.
                    _criticKinds.Remove(happened.CriticId, out int kind);
                    _bodies.Add((happened.Position, kind, _now));
                    Burst(happened.Position, KillScraps);
                    Show(Effect.KillBurst, happened.Position);
                    break;

                // A puff where the magician left and one where it arrives, and a held breath: a vanish, not a glitch.
                case TickEventKind.Vanish:
                    Burst(happened.Position, VanishScraps);
                    Burst(simulation.MagicianPosition, VanishScraps);
                    _hitStopLeft = VanishHitStop;
                    break;

                case TickEventKind.ApplausePickedUp:
                    Burst(happened.Position, PickUpScraps);
                    break;

                case TickEventKind.ApplauseEaten:
                    Burst(happened.Position, EatenScraps);
                    break;

                // The burst of a card (plan T25) is a ring on the floor, round the critic the card struck: no
                // lift, it lies where the rule measures. An understudy's is the dimmer, as its flick is.
                case TickEventKind.Burst:
                    _effects.Add((
                        happened.Thrower == TickEvent.TheMagician ? Effect.Ring : Effect.UnderstudyRing,
                        happened.Position,
                        _now));
                    break;

                case TickEventKind.MagicianHurt:
                    _magicianFlashedAt = FlashNow(_magicianFlashedAt);
                    _trauma = MathF.Min(1f, _trauma + HurtTrauma);
                    break;

                case TickEventKind.BoxOfficeStruck:
                    _boxOfficeFlashedAt = FlashNow(_boxOfficeFlashedAt);
                    _trauma = MathF.Min(1f, _trauma + StrikeTrauma);
                    break;
            }
        }

        // After the events: who stands now is whose fall a later tick may report.
        foreach (Critic critic in simulation.Critics)
        {
            _criticKinds[critic.Id] = critic.Kind;
        }
    }

    /// <summary>Lets <paramref name="seconds"/> go by: scraps fly, bodies fade, the shake dies away.</summary>
    public void Advance(float seconds)
    {
        _now += seconds;
        _bodies.RemoveAll(body => _now - body.FellAt >= BodyTime);
        _scraps.RemoveAll(scrap => _now - scrap.BurstAt >= ScrapTime);
        _effects.RemoveAll(effect => _now - effect.At >= Lasts(effect.Kind));
        _trauma = MathF.Max(0f, _trauma - (TraumaFade * seconds));
    }

    /// <summary>
    /// The moment of a Vanish holds the world still. While it does, a frame's time is taken here and is to be given
    /// to nothing else: not to the clock, and not to <see cref="Advance"/>.
    /// </summary>
    public bool Holds(float frameSeconds)
    {
        if (_hitStopLeft <= 0f)
        {
            return false;
        }

        _hitStopLeft -= frameSeconds;
        return true;
    }

    private static float Lasts(Effect kind) => kind switch
    {
        Effect.Flick or Effect.UnderstudyFlick => FlickTime,
        Effect.HitBurst => HitBurstTime,
        Effect.Ring or Effect.UnderstudyRing => RingTime,
        _ => KillBurstTime,
    };

    private void Show(Effect kind, Vector2 floor) => _effects.Add((kind, floor - new Vector2(0f, ScrapLift), _now));

    private float White(float flashedAt) => _now - flashedAt < FlashTime ? 1f : 0f;

    /// <summary>
    /// A figure that last flashed at <paramref name="flashedAt"/> is hurt now. When its flash began: now, unless
    /// that is too soon after the last one, which then stands.
    /// </summary>
    private float FlashNow(float flashedAt) => _now - flashedAt < FlashTime + FlashRest ? flashedAt : _now;

    private void Burst(Vector2 floor, int scraps)
    {
        for (int i = 0; i < scraps; i++)
        {
            float way = random.NextSingle() * MathF.Tau;
            float speed = ScrapSpeed * (ScrapSlowestShare + ((1f - ScrapSlowestShare) * random.NextSingle()));
            _scraps.Add(new Scrap(
                floor - new Vector2(0f, ScrapLift),
                new Vector2(MathF.Cos(way), MathF.Sin(way)) * speed,
                _now,
                random.NextSingle() * MathF.Tau));
        }
    }

    /// <summary>
    /// From -1 to 1, evenly, and always the same for the same <paramref name="beat"/>: its bits stirred (the
    /// finisher of MurmurHash3) and the top twenty-four of them taken, which a float holds whole.
    /// </summary>
    private static float Sway(uint beat)
    {
        beat ^= beat >> 16;
        beat *= 0x85EBCA6B;
        beat ^= beat >> 13;
        beat *= 0xC2B2AE35;
        beat ^= beat >> 16;
        return ((beat >> 8) / 8388608f) - 1f;
    }

    /// <summary>What a card is seen to do.</summary>
    public enum Effect
    {
        Flick,
        UnderstudyFlick,
        HitBurst,
        KillBurst,
        Ring,
        UnderstudyRing,
    }

    private readonly record struct Scrap(Vector2 From, Vector2 Velocity, float BurstAt, float Turn);
}
