using System.Numerics;

namespace Understudies.Core;

/// <summary>What a <see cref="TickEvent"/> tells of.</summary>
public enum TickEventKind
{
    /// <summary>The magician threw a card.</summary>
    Throw,

    /// <summary>A card hurt a critic and it still stands.</summary>
    Hit,

    /// <summary>A critic fell.</summary>
    Kill,

    /// <summary>The magician vanished.</summary>
    Vanish,

    /// <summary>A critic's touch hurt the magician.</summary>
    MagicianHurt,

    /// <summary>A critic struck the box office.</summary>
    BoxOfficeStruck,
}

/// <summary>Something that happened in one tick, for the view and the sound to show.</summary>
/// <param name="Position">
/// Where on the floor: for a throw, where the card was thrown from; for a hit and for a kill, the middle of the
/// critic's circle; for a Vanish, where the magician stood before it; for a hurt magician, where the magician
/// stands; for a struck box office, the middle of the circle of the critic that struck it.
/// </param>
public readonly record struct TickEvent(TickEventKind Kind, Vector2 Position);
