namespace Understudies.Core;

/// <summary>
/// The cards of the program (plan decision 20). Five are self cards, which change the magician and, with it, the
/// understudy of every act played from then on; one is a chorus card, which changes every understudy and not the
/// magician. Any of them may be taken again and adds again; how much each gives is the tuning's.
/// </summary>
public enum Card
{
    /// <summary>A thrown card takes <see cref="Tuning.CardDamage"/> more hit points.</summary>
    Damage,

    /// <summary>Throws come <see cref="Tuning.CardAttackSpeed"/> of the first rate more often.</summary>
    AttackSpeed,

    /// <summary>The throw reaches <see cref="Tuning.CardRange"/> further.</summary>
    Range,

    /// <summary>The Vanish comes back <see cref="Tuning.CardVanishCooldown"/> of the first rate sooner.</summary>
    VanishCooldown,

    /// <summary>
    /// Each throw sends one more card, at the next nearest critic in range: with fewer critics in range than cards,
    /// a card for each critic and no more.
    /// </summary>
    OneMoreCard,

    /// <summary>
    /// The chorus card: a card thrown by an understudy, any there is and any there will be, takes
    /// <see cref="Tuning.CardChorusDamage"/> more hit points. The magician's own cards do not.
    /// </summary>
    ChorusDamage,
}

/// <summary>
/// The self cards somebody has: how many of each. The magician's are those it has taken in this performance, and
/// an understudy's those the magician had in the act it was recorded, for ever (plan decision 20). The numbers a
/// throw runs on are the tuning's as these change them.
/// </summary>
public readonly record struct SelfCards(
    int Damage = 0,
    int AttackSpeed = 0,
    int Range = 0,
    int VanishCooldown = 0,
    int OneMoreCard = 0)
{
    /// <summary>These and one more self card. A chorus card is nobody's own.</summary>
    internal SelfCards With(Card card) => card switch
    {
        Card.Damage => this with { Damage = Damage + 1 },
        Card.AttackSpeed => this with { AttackSpeed = AttackSpeed + 1 },
        Card.Range => this with { Range = Range + 1 },
        Card.VanishCooldown => this with { VanishCooldown = VanishCooldown + 1 },
        Card.OneMoreCard => this with { OneMoreCard = OneMoreCard + 1 },
        _ => this,
    };
}
