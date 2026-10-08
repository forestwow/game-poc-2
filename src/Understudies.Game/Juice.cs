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
/// <param name="random">Where the scraps fly and how the stage shakes: a capture gives one that repeats.</param>
internal sealed class Juice(Random random)
{
    // Every number of the juice is here, to be turned by hand: none is a rule, so none is in tuning.json. Times are
    // in seconds and lengths in world units (a critic is one unit wide, and a unit is some 27 pixels of a window
    // 1280 wide).

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

    // A piece of applause that is picked up puffs this many.
    private const int PickUpScraps = 6;

    // A scrap starts at up to ScrapSpeed and at no less than ScrapSlowestShare of it, and loses speed at ScrapDrag
    // a second, so the fastest gets ScrapSpeed / ScrapDrag far. It is gone after ScrapTime, fading over the second
    // half of it. A burst starts ScrapLift above the floor, at a critic's chest.
    private const float ScrapSpeed = 12f;
    private const float ScrapSlowestShare = 0.4f;
    private const float ScrapDrag = 9f;
    private const float ScrapTime = 0.7f;
    private const float ScrapLift = 1f;

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

    private readonly Dictionary<int, (float HitPoints, float FlashedAt)> _critics = [];
    private readonly List<(Vector2 Position, float FellAt)> _bodies = [];
    private readonly List<Scrap> _scraps = [];

    // The juice's own time: what Advance has been given so far.
    // ponytail: a float, which stops telling one sixtieth of a second from the next when a show has been left open
    // for days. A double when a show can last that long.
    private float _now;
    private float _magicianFlashedAt = float.NegativeInfinity;
    private float _boxOfficeFlashedAt = float.NegativeInfinity;
    private float _trauma;
    private float _hitStopLeft;

    /// <summary>How far the whole stage is moved from its place in this frame.</summary>
    public Vector2 Shake { get; private set; }

    /// <summary>How far to white the magician is drawn, from 0 to 1: more than 0 when it was hurt a moment ago.</summary>
    public float MagicianWhite => White(_magicianFlashedAt);

    /// <summary>The same for the box office, struck a moment ago.</summary>
    public float BoxOfficeWhite => White(_boxOfficeFlashedAt) * BoxOfficeFlash;

    /// <summary>The same for a critic that a card hurt a moment ago.</summary>
    public float CriticWhite(int id) => _critics.TryGetValue(id, out var seen) ? White(seen.FlashedAt) : 0f;

    /// <summary>
    /// The critics that fell a moment ago: where each lies, how far to white it is drawn (the blow that felled it
    /// flashes too), and how much of it is left to see.
    /// </summary>
    public IEnumerable<(Vector2 Position, float White, float Opacity)> Bodies =>
        _bodies.Select(body => (body.Position, White(body.FellAt), 1f - ((_now - body.FellAt) / BodyTime)));

    /// <summary>The scraps in the air: the middle of each, how it is turned, and how much of it is left to see.</summary>
    public IEnumerable<(Vector2 Middle, float Turn, float Opacity)> Scraps => _scraps.Select(scrap =>
    {
        // A speed that falls by a share of itself all the time has carried the scrap this far by now.
        float age = _now - scrap.BurstAt;
        Vector2 flown = scrap.Velocity * ((1f - MathF.Exp(-ScrapDrag * age)) / ScrapDrag);
        return (scrap.From + flown, scrap.Turn, MathF.Min(1f, 2f * (1f - (age / ScrapTime))));
    });

    /// <summary>Takes in what the last tick did. Called after every tick: a frame may run several.</summary>
    public void Feed(Simulation simulation)
    {
        // An event has no id. A card hurt the critic that has less left than when it was last looked at.
        // ponytail: a fallen critic's entry stays until the next show, a few bytes for every critic there ever was.
        // A show that lets in tens of thousands needs them taken out, and a kill that says whose it was.
        foreach (Critic critic in simulation.Critics)
        {
            if (!_critics.TryGetValue(critic.Id, out var seen))
            {
                seen = (critic.HitPoints, float.NegativeInfinity);
            }

            bool hurt = critic.HitPoints < seen.HitPoints;
            _critics[critic.Id] = (critic.HitPoints, hurt ? FlashNow(seen.FlashedAt) : seen.FlashedAt);
        }

        foreach (TickEvent happened in simulation.Events)
        {
            switch (happened.Kind)
            {
                case TickEventKind.Kill:
                    _bodies.Add((happened.Position, _now));
                    Burst(happened.Position, KillScraps);
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
    }

    /// <summary>Lets <paramref name="seconds"/> go by: scraps fly, bodies fade, the shake dies away.</summary>
    public void Advance(float seconds)
    {
        _now += seconds;
        _bodies.RemoveAll(body => _now - body.FellAt >= BodyTime);
        _scraps.RemoveAll(scrap => _now - scrap.BurstAt >= ScrapTime);
        _trauma = MathF.Max(0f, _trauma - (TraumaFade * seconds));
        Shake = new Vector2(Sway(), Sway()) * (ShakeReach * _trauma * _trauma);
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

    /// <summary>From -1 to 1.</summary>
    private float Sway() => (random.NextSingle() * 2f) - 1f;

    private readonly record struct Scrap(Vector2 From, Vector2 Velocity, float BurstAt, float Turn);
}
