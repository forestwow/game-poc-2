using System.Numerics;
using Understudies.Core.Randomness;

namespace Understudies.Core;

/// <summary>Owns the state of the game and advances it one tick at a time.</summary>
/// <param name="seed">What is left to chance in the show comes from this: the same seed, the same show.</param>
public sealed class Simulation(Tuning tuning, ulong seed)
{
    public const int TicksPerSecond = 60;

    private readonly Rng _rng = new(seed);
    private readonly List<Critic> _critics = [];
    private int _criticsEntered;
    private int _ticksToNextCritic;

    /// <summary>
    /// The numbers the rules run on. New ones may be set between ticks: the state stays as it is and the next tick
    /// runs on them.
    /// </summary>
    public Tuning Tuning { get; set; } = tuning;

    /// <summary>The middle of the magician's circle on the floor, after the last tick.</summary>
    public Vector2 MagicianPosition { get; private set; } = tuning.MagicianMark;

    /// <summary>Where the magician was before the last tick: the view draws between the two.</summary>
    public Vector2 MagicianPreviousPosition { get; private set; } = tuning.MagicianMark;

    /// <summary>The critics on the stage, in the order they entered.</summary>
    public IReadOnlyList<Critic> Critics => _critics;

    /// <summary>What the box office has left: never less than nothing.</summary>
    public float BoxOfficeHitPoints { get; private set; } = tuning.BoxOfficeHitPoints;

    /// <summary>The box office has nothing left. The show is over: <see cref="Step"/> changes nothing any more.</summary>
    public bool ShowClosed => BoxOfficeHitPoints <= 0f;

    public void Step(MagicianInput input)
    {
        if (ShowClosed)
        {
            return;
        }

        // A keyboard diagonal is no faster than a straight line; a stick pushed halfway stays at half speed.
        Vector2 move = input.Move;
        float lengthSquared = (move.X * move.X) + (move.Y * move.Y);
        if (lengthSquared > 1f)
        {
            move /= MathF.Sqrt(lengthSquared);
        }

        // The stage's edge stops the magician: the whole circle stays on the floor.
        var radius = new Vector2(Tuning.MagicianRadius);
        MagicianPreviousPosition = MagicianPosition;
        MagicianPosition = Vector2.Clamp(
            MagicianPosition + (move * (Tuning.MagicianSpeed / TicksPerSecond)),
            radius,
            Tuning.StageSize - radius);

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
            // Straight at the box office as far as where the two circles touch, and back out to there when the
            // crowd has pushed the critic in.
            Vector2 toBoxOffice = Direction(Tuning.BoxOfficePosition - critic.Position, out float distance);
            float gap = distance - (Tuning.BoxOfficeSize / 2f) - Tuning.CriticRadius;
            float step = Tuning.CriticSpeed / TicksPerSecond;
            critic.Position += toBoxOffice * MathF.Min(gap, step);

            // A critic that touches the box office strikes it, and again a cooldown later.
            if (critic.TicksToNextStrike > 0)
            {
                critic.TicksToNextStrike--;
            }

            if (gap <= step && critic.TicksToNextStrike == 0)
            {
                BoxOfficeHitPoints = MathF.Max(0f, BoxOfficeHitPoints - Tuning.CriticStrikeDamage);
                critic.TicksToNextStrike = Ticks(Tuning.CriticStrikeCooldown);
            }
        }

        // The first critic enters on the first tick.
        if (--_ticksToNextCritic <= 0)
        {
            LetACriticIn();
            _ticksToNextCritic = Ticks(Tuning.CriticEntryInterval);
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
        // A door in a side edge runs up and down it; a door in the top or the bottom edge runs along it.
        Vector2 door = Tuning.StageDoors[0];
        bool inASide = door.X <= 0f || door.X >= Tuning.StageSize.X;
        float along = (_rng.NextFloat() - 0.5f) * Tuning.StageDoorWidth;
        _critics.Add(new Critic(_criticsEntered++, door + (inASide ? new Vector2(0f, along) : new Vector2(along, 0f))));
    }
}
