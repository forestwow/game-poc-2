using System.Numerics;

namespace Understudies.Core.Tests;

/// <summary>How one act of a scripted performance went, as it stood when the act was over and its card taken.</summary>
/// <param name="Entries">The enemies let onto the stage in the act, of every kind.</param>
/// <param name="Applause">The pieces the magician picked up.</param>
/// <param name="Share">The applause as a share of the entries (plan decision 21).</param>
/// <param name="Card">The card taken in the act's program, or none when the act earned no program.</param>
/// <param name="Fell">Whether the magician fell in the act.</param>
/// <param name="BoxOffice">What the box office had left.</param>
/// <param name="Dropped">The pieces of applause dropped in the act, picked up or not.</param>
/// <param name="FellInReach">
/// Of the pieces picked up, those dropped inside the magician's pick-up reach of where it stood on that tick:
/// applause that fell on a magician that did not move for it.
/// </param>
/// <param name="WalkedTo">Of the pieces picked up, the others: those the magician had to go to.</param>
/// <param name="StateHash">The simulation's state hash at that moment.</param>
internal readonly record struct ActRecord(
    int Entries,
    int Applause,
    float Share,
    ApplauseBand Band,
    Card? Card,
    bool Fell,
    float BoxOffice,
    int Dropped,
    int FellInReach,
    int WalkedTo,
    ulong StateHash);

/// <summary>A scripted performance played to its end: the ovation or the close.</summary>
/// <param name="Ended"><see cref="Phase.Ovation"/> or <see cref="Phase.Closed"/>.</param>
/// <param name="Acts">Every act that was played, the one the show closed in too.</param>
internal sealed record Performance(Phase Ended, IReadOnlyList<ActRecord> Acts)
{
    /// <summary>The number of the act the performance ended in.</summary>
    public int Act => Acts.Count;

    public float BoxOffice => Acts[^1].BoxOffice;
}

/// <summary>
/// The two players that measure the design (plan T19 and decision 22), and the runner that plays a performance
/// with one. A player is a function from what a person at the screen sees, the simulation's public surface, to
/// the input of one tick: it keeps nothing between two ticks and draws nothing, so the same seed and player give
/// the same performance. Like Core, the players use only + − × ÷ and the square root (plan decision 9).
/// </summary>
internal static class ScriptedPlayers
{
    /// <summary>
    /// The circle the orbit player walks round the box office and never leaves, by its radius: the throw reaches
    /// across the box office from anywhere on it.
    /// </summary>
    public const float OrbitRadius = 5f;

    /// <summary>
    /// How far from its post the doors player goes for a piece of applause: a second's walk, and as far as the
    /// committed throw reaches, so that what it fells from its post it fetches. With 6 it left most of its own
    /// applause lying, where its cards and its understudies' fell critics at the far end of the throw. From
    /// further than this, where only a Vanish takes it, it walks back before anything else.
    /// </summary>
    public const float DoorsReach = 9f;

    /// <summary>
    /// A player is crowded, and vanishes if it can, when this many critics that turn on it are within
    /// <see cref="CrowdReach"/> of its centre and not stunned.
    /// </summary>
    public const int CrowdSize = 2;

    /// <inheritdoc cref="CrowdSize"/>
    public const float CrowdReach = 2f;

    /// <summary>
    /// The one order both players pick cards by, the most wanted first: of an offer they take the card that
    /// comes first here, so the result depends on the route and not on a taste.
    /// </summary>
    public static readonly IReadOnlyList<Card> CardOrder =
    [
        Card.OneMoreCard, Card.ChorusDamage, Card.Damage, Card.AttackSpeed, Card.Range, Card.VanishCooldown,
    ];

    /// <summary>The place in <paramref name="offer"/> of the card that comes first in <see cref="CardOrder"/>.</summary>
    public static int Choose(IReadOnlyList<Card> offer)
    {
        int best = 0;
        for (int place = 1; place < offer.Count; place++)
        {
            if (Wanted(offer[place]) < Wanted(offer[best]))
            {
                best = place;
            }
        }

        return best;

        static int Wanted(Card card)
        {
            for (int i = 0; i < CardOrder.Count; i++)
            {
                if (CardOrder[i] == card)
                {
                    return i;
                }
            }

            return CardOrder.Count;
        }
    }

    /// <summary>
    /// The degenerate strategy the design has to beat (vision 4): it walks round the box office on a circle of
    /// <see cref="OrbitRadius"/> and never leaves the circle. It steps to a piece of applause only when the piece
    /// lies inside the circle, and when it is crowded it vanishes across the circle, to a place on it.
    /// </summary>
    public static MagicianInput Orbit(Simulation simulation)
    {
        Vector2 position = simulation.MagicianPosition;
        Vector2 centre = simulation.Tuning.BoxOfficePosition;
        Vector2 outward = Direction(position - centre, out float fromCentre);
        var onward = new Vector2(-outward.Y, outward.X);

        // The Vanish goes its whole distance the way the magician faces: the place on the circle that far from
        // here is `inward` along the line to the centre and `aside` along the circle's tangent. Nearer the
        // centre than the Vanish is long less the radius there is no such place, and it does not vanish.
        float blink = simulation.Tuning.VanishDistance;
        float inward = ((fromCentre * fromCentre) + (blink * blink) - (OrbitRadius * OrbitRadius)) / (2f * fromCentre);
        if (inward <= blink && IsCrowded(simulation, out _) && simulation.VanishCooldownLeft == 0f)
        {
            float aside = MathF.Sqrt((blink * blink) - (inward * inward));
            return new MagicianInput((onward * aside) - (outward * inward), Vanish: true);
        }

        if (NearestApplause(simulation, centre, OrbitRadius) is Vector2 piece)
        {
            return Walk(simulation, piece);
        }

        // Round the circle: at the place on it one step on, or, from inside it, straight out to it. The straight
        // line to a place on the circle is inside the circle.
        float step = simulation.Tuning.MagicianSpeed / Simulation.TicksPerSecond;
        return Walk(simulation, centre + (Direction((position - centre) + (onward * step), out _) * OrbitRadius));
    }

    /// <summary>
    /// The strategy the design wants to win (vision 4): it holds the door that opened last, standing a throw's
    /// range inside it, so that what comes in is thrown at as it enters, and leaves the older doors to its
    /// understudies. It steps to a piece of applause within <see cref="DoorsReach"/> of that post and goes back,
    /// and when it is crowded it vanishes away from the crowd.
    /// </summary>
    public static MagicianInput Doors(Simulation simulation)
    {
        Vector2 position = simulation.MagicianPosition;
        Vector2 post = DoorsPost(simulation);
        if (IsCrowded(simulation, out Vector2 crowd) && simulation.VanishCooldownLeft == 0f)
        {
            // Away from the middle of the crowd; from the very middle of it, back towards the box office.
            Vector2 away = position - crowd;
            return new MagicianInput(
                away == Vector2.Zero ? simulation.Tuning.BoxOfficePosition - position : away, Vanish: true);
        }

        Direction(position - post, out float fromPost);
        return fromPost <= DoorsReach && NearestApplause(simulation, post, DoorsReach) is Vector2 piece
            ? Walk(simulation, piece)
            : Walk(simulation, post);
    }

    /// <summary>
    /// Where the doors player stands in the act that is played: a throw's range, as the tuning has it without
    /// cards, in from the door that opened last, on the straight line to the box office, which is the way its
    /// critics walk. Of two doors that opened together it is the later on the list.
    /// </summary>
    public static Vector2 DoorsPost(Simulation simulation)
    {
        Tuning tuning = simulation.Tuning;
        // A tuning has a door that is open in the first act: one is always found.
        var newest = new StageDoor(default, int.MinValue);
        for (int door = 0; door < tuning.StageDoors.Count; door++)
        {
            if (simulation.DoorIsOpen(door) && tuning.StageDoors[door].OpensInAct >= newest.OpensInAct)
            {
                newest = tuning.StageDoors[door];
            }
        }

        Vector2 inside = Direction(tuning.BoxOfficePosition - newest.Position, out _);
        return newest.Position + (inside * tuning.ThrowRange);
    }

    /// <summary>
    /// Plays one performance with <paramref name="player"/> to its end. Between two acts it does what a person
    /// does: takes the card <see cref="Choose"/> names, at once, and goes on.
    /// </summary>
    public static Performance Play(Tuning tuning, ulong seed, Func<Simulation, MagicianInput> player)
    {
        var simulation = new Simulation(tuning, seed);
        var acts = new List<ActRecord>();
        float reach = tuning.MagicianRadius + tuning.ApplausePickUpReach;

        // Where the pieces lie that were dropped in reach and are not picked up yet. The act's applause is gone
        // when the next begins, and these with it.
        var inReach = new List<Vector2>();
        int dropped = 0;
        int fellInReach = 0;
        int walkedTo = 0;
        while (true)
        {
            simulation.Step(player(simulation));
            foreach (TickEvent happened in simulation.Events)
            {
                if (happened.Kind == TickEventKind.ApplauseDropped)
                {
                    dropped++;
                    Direction(happened.Position - simulation.MagicianPosition, out float apart);
                    if (apart <= reach)
                    {
                        inReach.Add(happened.Position);
                    }
                }
                else if (happened.Kind == TickEventKind.ApplausePickedUp)
                {
                    if (inReach.Remove(happened.Position))
                    {
                        fellInReach++;
                    }
                    else
                    {
                        walkedTo++;
                    }
                }
            }

            Card? card = null;
            if (simulation.Phase == Phase.Program)
            {
                int place = Choose(simulation.Offer);
                card = simulation.Offer[place];
                simulation.Pick(place);
            }

            if (simulation.Phase is Phase.Curtain or Phase.Act)
            {
                continue;
            }

            acts.Add(new ActRecord(
                simulation.ActEntriesMade,
                simulation.ActApplause,
                simulation.ActApplauseShare,
                simulation.ActApplauseBand,
                card,
                simulation.MagicianHasFallen,
                simulation.BoxOfficeHitPoints,
                dropped,
                fellInReach,
                walkedTo,
                simulation.ComputeStateHash()));
            if (simulation.Phase != Phase.BetweenActs)
            {
                return new Performance(simulation.Phase, acts);
            }

            simulation.GoOn();
            inReach.Clear();
            dropped = fellInReach = walkedTo = 0;
        }
    }

    /// <summary>
    /// The input that walks the magician straight at <paramref name="target"/> at full speed, and on the last
    /// tick only as far as the target.
    /// </summary>
    private static MagicianInput Walk(Simulation simulation, Vector2 target) =>
        new((target - simulation.MagicianPosition)
            / (simulation.Tuning.MagicianSpeed / Simulation.TicksPerSecond));

    /// <summary>
    /// Whether <see cref="CrowdSize"/> critics that have turned on the magician are within
    /// <see cref="CrowdReach"/> of it, and the middle of those that are.
    /// </summary>
    private static bool IsCrowded(Simulation simulation, out Vector2 middle)
    {
        int near = 0;
        middle = Vector2.Zero;
        foreach (Critic critic in simulation.Critics)
        {
            Direction(critic.Position - simulation.MagicianPosition, out float apart);
            if (apart <= CrowdReach && !critic.IsStunned && simulation.Tuning.EnemyKinds[critic.Kind].TurnsOnTheMagician)
            {
                near++;
                middle += critic.Position;
            }
        }

        middle /= Math.Max(1, near);
        return near >= CrowdSize;
    }

    /// <summary>
    /// Of the pieces of applause no further than <paramref name="radius"/> from <paramref name="centre"/>, the
    /// one nearest to the magician, and of two as near the one dropped first.
    /// </summary>
    private static Vector2? NearestApplause(Simulation simulation, Vector2 centre, float radius)
    {
        Vector2? nearest = null;
        float nearestApart = float.PositiveInfinity;
        foreach (Applause piece in simulation.ApplauseOnTheFloor)
        {
            Direction(piece.Position - centre, out float fromCentre);
            Direction(piece.Position - simulation.MagicianPosition, out float apart);
            if (fromCentre <= radius && apart < nearestApart)
            {
                nearest = piece.Position;
                nearestApart = apart;
            }
        }

        return nearest;
    }

    /// <summary>Which way <paramref name="v"/> points, one unit long, as the simulation's own rule has it.</summary>
    private static Vector2 Direction(Vector2 v, out float length)
    {
        length = MathF.Sqrt((v.X * v.X) + (v.Y * v.Y));
        return length > 0f ? v / length : Vector2.UnitX;
    }
}
