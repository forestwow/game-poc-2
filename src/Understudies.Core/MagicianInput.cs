using System.Numerics;

namespace Understudies.Core;

/// <summary>
/// What the player asks of the magician in one tick. <paramref name="Move"/> points where to walk, with y growing
/// downward; its length is the share of the full speed, and anything longer than 1 counts as 1.
/// <paramref name="Vanish"/> asks for the Vanish in this tick: once for each press, not for as long as it is held.
/// </summary>
public readonly record struct MagicianInput(Vector2 Move, bool Vanish = false);
