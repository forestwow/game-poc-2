using System.Numerics;

namespace Understudies.Core;

/// <summary>A card in the air: a point over the floor that flies the way it was thrown.</summary>
public sealed class ThrownCard
{
    internal ThrownCard(
        Vector2 position,
        Vector2 direction,
        float rangeLeft,
        float damage,
        int thrower,
        float pierceLoss,
        int turnsLeft,
        float burstShare)
    {
        Damage = damage;
        PierceLoss = pierceLoss;
        TurnsLeft = turnsLeft;
        BurstShare = burstShare;
        ThrownFrom = position;
        Position = position;
        PreviousPosition = position;
        Direction = direction;
        RangeLeft = rangeLeft;
        Thrower = thrower;
    }

    /// <summary>
    /// Where whoever threw the card stood, or where the card last turned (plan T25): the view's trail reaches
    /// back no further. No rule reads it.
    /// </summary>
    public Vector2 ThrownFrom { get; internal set; }

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
    internal Vector2 Direction { get; set; }

    /// <summary>
    /// The hit points it takes off the critic it touches: its thrower's, when it was thrown, less what its
    /// strikes have taken off since. A card is spent when a strike leaves it nothing, or next to nothing.
    /// </summary>
    internal float Damage { get; set; }

    /// <summary>
    /// What a strike takes off <see cref="Damage"/> when the card goes on through the critic: its thrower's, when
    /// it was thrown. For a thrower with no pierce card it is all the card has, so its first strike spends it.
    /// </summary>
    internal float PierceLoss { get; }

    /// <summary>
    /// How many times the card may still turn to another critic: its thrower's ricochet cards at first.
    /// </summary>
    internal int TurnsLeft { get; set; }

    /// <summary>
    /// The share of <see cref="Damage"/> a strike of the card takes off every other critic within the burst's
    /// radius: its thrower's, when it was thrown, and nothing for a thrower with no burst card.
    /// </summary>
    internal float BurstShare { get; }

    /// <summary>
    /// The <see cref="Critic.Id"/> of every critic the card has struck, in the order it struck them: it goes
    /// through those and turns to none of them again.
    /// </summary>
    internal List<int> Struck { get; } = [];

    /// <summary>How far the card may still fly.</summary>
    internal float RangeLeft { get; set; }
}
