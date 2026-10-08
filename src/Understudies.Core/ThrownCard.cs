using System.Numerics;

namespace Understudies.Core;

/// <summary>A card in the air: a point over the floor that flies the way it was thrown.</summary>
public sealed class ThrownCard
{
    internal ThrownCard(Vector2 position, Vector2 direction, float rangeLeft, bool thrownByMagician)
    {
        Position = position;
        PreviousPosition = position;
        Direction = direction;
        RangeLeft = rangeLeft;
        ThrownByMagician = thrownByMagician;
    }

    /// <summary>Where the card is, after the last tick.</summary>
    public Vector2 Position { get; internal set; }

    /// <summary>Where the card was before the last tick: the view draws between the two.</summary>
    public Vector2 PreviousPosition { get; internal set; }

    /// <summary>The magician itself threw the card, and not an understudy.</summary>
    public bool ThrownByMagician { get; }

    /// <summary>One unit long.</summary>
    internal Vector2 Direction { get; }

    /// <summary>How far the card may still fly.</summary>
    internal float RangeLeft { get; set; }
}
