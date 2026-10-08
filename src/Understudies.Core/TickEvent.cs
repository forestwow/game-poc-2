using System.Numerics;

namespace Understudies.Core;

/// <summary>What a <see cref="TickEvent"/> tells of.</summary>
public enum TickEventKind
{
    /// <summary>The magician or an understudy threw a card: <see cref="TickEvent.Thrower"/> says who.</summary>
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

    /// <summary>The magician fell: the blow that took the last of its hit points is reported before it.</summary>
    MagicianFell,

    /// <summary>
    /// A critic fell to a card the magician itself threw, and a piece of applause was left where it fell: the
    /// kill is reported before it.
    /// </summary>
    ApplauseDropped,

    /// <summary>The magician picked up a piece of applause.</summary>
    ApplausePickedUp,

    /// <summary>
    /// A card of a thrower with a burst card struck a critic (plan T25): the strike is reported before it, and
    /// after it a hit or a kill for each critic the burst hurt, which name the card's thrower as the strike does.
    /// </summary>
    Burst,
}

/// <summary>Something that happened in one tick, for the view and the sound to show.</summary>
/// <param name="Position">
/// Where on the floor: for a throw, where the card was thrown from; for a hit and for a kill, the middle of the
/// critic's circle; for a burst, the middle of the circle of the critic the card struck; for a Vanish, where the magician stood before it; for a hurt magician and for a fallen one,
/// where the magician stands; for a struck box office, the middle of the circle of the critic that struck it; for
/// applause, dropped or picked up, where the piece lies.
/// </param>
/// <param name="Thrower">
/// Who threw the card, for a throw, a hit, a kill and a burst: <see cref="TheMagician"/>, or an understudy by its place in
/// <see cref="Simulation.Understudies"/>. For any other kind it is left as it is made, the magician: true of applause dropped, which only the magician's card leaves, and saying nothing of the rest.
/// </param>
/// <param name="CriticId">
/// The <see cref="Critic.Id"/> of the critic a card struck, for a hit and a kill, and of the critic that left
/// the piece, for applause dropped: a critic that has fallen is on the stage no more, and its id is all that is
/// left to know it by. <see cref="NoCritic"/> for every other kind.
/// </param>
public readonly record struct TickEvent(
    TickEventKind Kind, Vector2 Position, int Thrower = TickEvent.TheMagician, int CriticId = TickEvent.NoCritic)
{
    /// <summary>The <see cref="Thrower"/> that is no understudy.</summary>
    public const int TheMagician = -1;

    /// <summary>The <see cref="CriticId"/> of an event that is about no critic: no critic has it.</summary>
    public const int NoCritic = -1;
}
