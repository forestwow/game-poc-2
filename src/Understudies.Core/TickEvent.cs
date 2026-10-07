using System.Numerics;

namespace Understudies.Core;

/// <summary>What a <see cref="TickEvent"/> tells of.</summary>
public enum TickEventKind
{
    /// <summary>A card hurt a critic and it still stands.</summary>
    Hit,

    /// <summary>A critic fell.</summary>
    Kill,

    /// <summary>The magician vanished.</summary>
    Vanish,
}

/// <summary>Something that happened in one tick, for the view and the sound to show.</summary>
/// <param name="Position">
/// Where on the floor: for a hit and for a kill, the middle of the critic's circle; for a Vanish, where the magician
/// stood before it.
/// </param>
public readonly record struct TickEvent(TickEventKind Kind, Vector2 Position);
