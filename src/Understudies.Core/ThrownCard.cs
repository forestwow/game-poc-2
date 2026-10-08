using System.Numerics;

namespace Understudies.Core;

/// <summary>A card in the air: a point over the floor that flies the way it was thrown.</summary>
public sealed class ThrownCard
{
    internal ThrownCard(Vector2 position, Vector2 direction, float rangeLeft, float damage, int thrower)
    {
        Damage = damage;
        ThrownFrom = position;
        Position = position;
        PreviousPosition = position;
        Direction = direction;
        RangeLeft = rangeLeft;
        Thrower = thrower;
    }

    /// <summary>Where whoever threw the card stood: the view's trail reaches back no further.</summary>
    public Vector2 ThrownFrom { get; }

    /// <summary>Where the card is, after the last tick.</summary>
    public Vector2 Position { get; internal set; }

    /// <summary>Where the card was before the last tick: the view draws between the two.</summary>
    public Vector2 PreviousPosition { get; internal set; }

    /// <summary>The magician itself threw the card, and not an understudy.</summary>
    public bool ThrownByMagician => Thrower == TickEvent.TheMagician;

    /// <summary>
    /// Who threw the card: <see cref="TickEvent.TheMagician"/>, or an understudy by its place in
    /// <see cref="Simulation.Understudies"/>. No rule reads which understudy, only whether it was the magician:
    /// it is carried for the events of the card's strike, and so is not in the state hash.
    /// </summary>
    public int Thrower { get; }

    /// <summary>One unit long.</summary>
    internal Vector2 Direction { get; }

    /// <summary>The hit points it takes off the critic it touches: its thrower's, when it was thrown.</summary>
    internal float Damage { get; }

    /// <summary>How far the card may still fly.</summary>
    internal float RangeLeft { get; set; }
}
