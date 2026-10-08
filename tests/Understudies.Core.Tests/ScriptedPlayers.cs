using System.Numerics;

namespace Understudies.Core.Tests;

/// <summary>
/// How one act of a scripted performance went, as it stood when the act was over and its program's card taken.
/// </summary>
/// <param name="Entries">The enemies let onto the stage in the act, of every kind.</param>
/// <param name="Applause">The pieces the magician picked up.</param>
/// <param name="Encores">The encores taken in the act (plan decision 26).</param>
/// <param name="Fell">Whether the magician fell in the act.</param>
/// <param name="BoxOffice">What the box office had left.</param>
/// <param name="Dropped">The pieces of applause dropped in the act, picked up or not.</param>
/// <param name="FellInReach">
/// Of the pieces picked up, those dropped inside the magician's pick-up reach of where it stood on that tick. A
/// critic that touches the magician stands inside that reach on the committed numbers (plan T21), so this counts
/// every critic that fell touching it: it is no measure of how safe a player stood.
/// </param>
/// <param name="WalkedTo">Of the pieces picked up, the others: those the magician had to go to.</param>
/// <param name="StateHash">The simulation's state hash at that moment.</param>
internal readonly record struct ActRecord(
    int Entries,
    int Applause,
    int Encores,
    bool Fell,
    float BoxOffice,
    int Dropped,
    int FellInReach,
    int WalkedTo,
    ulong StateHash);

/// <summary>A scripted performance played to its end: the ovation or the close.</summary>
/// <param name="Ended"><see cref="Phase.Ovation"/> or <see cref="Phase.Closed"/>.</param>
/// <param name="Acts">Every act that was played, the one the show closed in too.</param>
/// <param name="Encores">The cards taken in the encores of each of those acts, in the order they were taken.</param>
internal sealed record Performance(
    Phase Ended, IReadOnlyList<ActRecord> Acts, IReadOnlyList<IReadOnlyList<Card>> Encores)
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
    /// The circles the orbit player is played on, by their radii: standing on the box office, close round it, and
    /// out where the throw still reaches across it. The guard is read against all of them (see
    /// <see cref="Orbit"/>): a tuning that only beats one way of hiding has not beaten hiding.
    /// </summary>
    public static readonly IReadOnlyList<float> OrbitRadii = [0.5f, 3f, 5f];

    /// <summary>
    /// How far from where it stands the doors player goes for a piece of applause: a second's walk, and as far
    /// as the committed throw reaches, so that what it fells from its post it fetches. With 6 it left most of its
    /// own applause lying, where its cards and its understudies' fell critics at the far end of the throw. From
    /// further than this it walks back before anything else.
    /// </summary>
    public const float DoorsReach = 9f;

    /// <summary>
    /// The doors player steps back from a critic that has turned on it and is this near to its centre: a touch
    /// is at 1.1 on the committed numbers.
    /// </summary>
    public const float DoorsStepBackReach = 2.5f;

    /// <summary>
    /// While a critic is at the box office the doors player stands this far from the box office's centre, on the
    /// side of its door: the throw reaches across the box office from there.
    /// </summary>
    public const float DoorsDefenceDistance = 6f;

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
    /// The degenerate strategy the design has to beat (vision 4), on a circle of <paramref name="radius"/>: it
    /// walks round the box office on the circle and never leaves it. It steps to a piece of applause only when
    /// the piece lies inside the circle, and when it is crowded it vanishes across the circle, to a place on it:
    /// on a circle less than a Vanish across it does not vanish. Hiding is whichever of
    /// <see cref="OrbitRadii"/> does best, so the design has beaten it on a seed only when every one of them
    /// has lost.
    /// </summary>
    public static Func<Simulation, MagicianInput> Orbit(float radius) => simulation =>
    {
        Vector2 position = simulation.MagicianPosition;
        Vector2 centre = simulation.Tuning.BoxOfficePosition;
        Vector2 outward = Direction(position - centre, out float fromCentre);
        var onward = new Vector2(-outward.Y, outward.X);

        // The Vanish goes its whole distance the way the magician faces: the place on the circle that far from
        // here is `inward` along the line to the centre and `aside` along the circle's tangent. Where there is
        // no such place (too near the centre, or the circle too small) it does not vanish.
        float blink = simulation.Tuning.VanishDistance;
        float inward = ((fromCentre * fromCentre) + (blink * blink) - (radius * radius)) / (2f * fromCentre);
        if (inward <= blink
            && IsCrowded(simulation, CrowdSize, CrowdReach, out _)
            && simulation.VanishCooldownLeft == 0f)
        {
            float aside = MathF.Sqrt((blink * blink) - (inward * inward));
            return new MagicianInput((onward * aside) - (outward * inward), Vanish: true);
        }

        if (NearestApplause(simulation, centre, radius) is Vector2 piece)
        {
            return Walk(simulation, piece);
        }

        // Round the circle: at the place on it one step on, or, from inside it, straight out to it. The straight
        // line to a place on the circle is inside the circle.
        float step = simulation.Tuning.MagicianSpeed / Simulation.TicksPerSecond;
        return Walk(simulation, centre + (Direction((position - centre) + (onward * step), out _) * radius));
    };

    /// <summary>
    /// The strategy the design wants to win (vision 4). It holds the door that opened last, standing a throw's
    /// range inside it, so that what comes in is thrown at as it enters, and leaves the older doors to its
    /// understudies; while a critic is at the box office it goes back and stands by the box office instead, and
    /// returns to its door when none is (<see cref="DoorsStand"/>). It steps to a piece of applause within
    /// <see cref="DoorsReach"/> of where it stands and goes back. It steps back from a critic that has turned
    /// on it and is within <see cref="DoorsStepBackReach"/>, and when it is crowded all the same it vanishes
    /// away from the crowd.
    /// </summary>
    public static MagicianInput Doors(Simulation simulation)
    {
        Vector2 position = simulation.MagicianPosition;

        // Away from the middle of those upon it; from the very middle of them, back towards the box office.
        if (IsCrowded(simulation, CrowdSize, CrowdReach, out Vector2 crowd) && simulation.VanishCooldownLeft == 0f)
        {
            return new MagicianInput(Away(crowd), Vanish: true);
        }

        if (IsCrowded(simulation, size: 1, DoorsStepBackReach, out Vector2 near))
        {
            return new MagicianInput(Direction(Away(near), out _));
        }

        Vector2 stand = DoorsStand(simulation);
        Direction(position - stand, out float fromStand);
        return fromStand <= DoorsReach && NearestApplause(simulation, stand, DoorsReach) is Vector2 piece
            ? Walk(simulation, piece)
            : Walk(simulation, stand);

        Vector2 Away(Vector2 from) =>
            position == from ? simulation.Tuning.BoxOfficePosition - position : position - from;
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
    /// Where the doors player stands now: on its post (<see cref="DoorsPost"/>), or, while some critic is at the
    /// box office, where it strikes, <see cref="DoorsDefenceDistance"/> from the box office's centre on the line
    /// to the post. A post nearer the box office than that is where it stands either way.
    /// </summary>
    public static Vector2 DoorsStand(Simulation simulation)
    {
        Tuning tuning = simulation.Tuning;
        Vector2 post = DoorsPost(simulation);
        Vector2 toPost = Direction(post - tuning.BoxOfficePosition, out float postFromBoxOffice);
        if (postFromBoxOffice <= DoorsDefenceDistance)
        {
            return post;
        }

        foreach (Critic critic in simulation.Critics)
        {
            // A critic strikes when it is within a step of touching the box office: a fifth of a unit is more
            // than a step of either committed kind.
            Direction(critic.Position - tuning.BoxOfficePosition, out float fromBoxOffice);
            if (fromBoxOffice <= (tuning.BoxOfficeSize / 2f) + tuning.EnemyKinds[critic.Kind].Radius + 0.2f)
            {
                return tuning.BoxOfficePosition + (toPost * DoorsDefenceDistance);
            }
        }

        return post;
    }

    /// <summary>
    /// Plays one performance with <paramref name="player"/> to its end. In an encore it does what a person does:
    /// takes the card <see cref="Choose"/> names, at once. Between two acts it takes the program's card, at once,
    /// and goes on.
    /// </summary>
    public static Performance Play(Tuning tuning, ulong seed, Func<Simulation, MagicianInput> player)
    {
        var simulation = new Simulation(tuning, seed);
        var acts = new List<ActRecord>();
        var encores = new List<IReadOnlyList<Card>>();
        var taken = new List<Card>();
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

            if (simulation.Phase == Phase.Encore)
            {
                int place = Choose(simulation.Offer);
                taken.Add(simulation.Offer[place]);
                simulation.Pick(place);
            }

            if (simulation.Phase is Phase.Curtain or Phase.Act)
            {
                continue;
            }

            // The program's one card, where the act has a program.
            simulation.Pick(0);
            encores.Add(taken);
            acts.Add(new ActRecord(
                simulation.ActEntriesMade,
                simulation.ActApplause,
                taken.Count,
                simulation.MagicianHasFallen,
                simulation.BoxOfficeHitPoints,
                dropped,
                fellInReach,
                walkedTo,
                simulation.ComputeStateHash()));
            if (simulation.Phase != Phase.BetweenActs)
            {
                return new Performance(simulation.Phase, acts, encores);
            }

            simulation.GoOn();
            taken = [];
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
    /// Whether <paramref name="size"/> critics that have turned on the magician are within
    /// <paramref name="reach"/> of it, and the middle of those that are.
    /// </summary>
    private static bool IsCrowded(Simulation simulation, int size, float reach, out Vector2 middle)
    {
        int near = 0;
        middle = Vector2.Zero;
        foreach (Critic critic in simulation.Critics)
        {
            Direction(critic.Position - simulation.MagicianPosition, out float apart);
            if (apart <= reach && !critic.IsStunned && simulation.Tuning.EnemyKinds[critic.Kind].TurnsOnTheMagician)
            {
                near++;
                middle += critic.Position;
            }
        }

        middle /= Math.Max(1, near);
        return near >= size;
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
