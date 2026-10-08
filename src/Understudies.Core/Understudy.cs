using System.Numerics;

namespace Understudies.Core;

/// <summary>
/// What an act that was played leaves behind: a figure that plays that act again in every act after it, tick for
/// tick where the magician stood. It is on its rail: nothing hurts it, pushes it or stands in its way.
/// </summary>
public sealed class Understudy
{
    internal Understudy(
        int act,
        IReadOnlyList<Vector2> route,
        IReadOnlyList<(int Tick, Vector2 Place)> vanishes,
        SelfCards cards,
        IReadOnlyList<(int Tick, Card Card)> encores)
    {
        Act = act;
        FirstCards = cards;
        Cards = cards;
        Route = route;
        Vanishes = vanishes;
        Encores = encores;
    }

    /// <summary>The number of the act it came from: the first is 1.</summary>
    public int Act { get; }

    /// <summary>
    /// The self cards it throws by now: those the magician had on this tick of that act, whatever the magician
    /// has taken since. It begins every act with those its own act began with, and gains each card of that
    /// act's encores on the tick the magician first played with it (plan T24). The Vanish cooldown card is among
    /// them and changes nothing for it: its Vanishes are recorded ticks.
    /// </summary>
    public SelfCards Cards { get; internal set; }

    /// <summary>The self cards that act began with.</summary>
    internal SelfCards FirstCards { get; }

    /// <summary>
    /// Every encore taken in that act, in the order they were taken: the tick of the act the magician first
    /// played with the card, the first being 0, and the card. None is after the magician's fall: a fallen
    /// magician takes no encore.
    /// </summary>
    internal IReadOnlyList<(int Tick, Card Card)> Encores { get; }

    /// <summary>
    /// Where the magician stood after each tick of that act, from its first: as many places as the act was played
    /// for, or, when the magician fell in it, as many as it stood for: the last is where it fell.
    /// </summary>
    public IReadOnlyList<Vector2> Route { get; }

    /// <summary>
    /// Every Vanish of that act: the tick of the act it was on, the first being 0, and where its cloud was left.
    /// </summary>
    internal IReadOnlyList<(int Tick, Vector2 Place)> Vanishes { get; }

    /// <summary>
    /// The middle of its circle on the floor, after the last tick: the place of its route for that tick of the act.
    /// Before an act's first tick it stands on the first.
    /// </summary>
    public Vector2 Position { get; internal set; }

    /// <summary>Where it was before the last tick: the view draws between the two.</summary>
    public Vector2 PreviousPosition { get; internal set; }

    /// <summary>
    /// Its route has a place for this tick of the act. When the act goes on for longer than its own was played, it
    /// is gone from the stage for the rest of the act, and stays where it last stood for nobody to see.
    /// </summary>
    public bool IsOnStage { get; internal set; }

    internal int TicksToNextThrow { get; set; }
}
