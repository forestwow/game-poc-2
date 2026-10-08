namespace Understudies.Core;

/// <summary>
/// The cards (plan decisions 20, 26 and 29). Eight are self cards, which an encore offers: they change the magician
/// and, with it, the understudy of the act they were taken in, from that tick of every later act on, and of
/// every act that begins after. One is a chorus card, which the program
/// offers, and an encore when the magician has every self card it may: it changes every understudy and not the
/// magician. A card may be taken again and adds again, a self card up to <see cref="Tuning.CardMaxCopies"/> of
/// it (plan T41) and the chorus card without end; how much each gives is the tuning's.
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

    /// <summary>
    /// A self card, The Pierce (plan T25): a thrown card goes on through the critic it strikes, and each strike
    /// takes <see cref="Tuning.CardPierceLoss"/> over the number of these cards off what it hurts for; it is spent
    /// when nothing is left. It strikes no critic twice.
    /// </summary>
    Pierce,

    /// <summary>
    /// A self card, The Ricochet (plan T25): a thrown card that strikes turns to the nearest critic it has not
    /// struck within <see cref="Tuning.CardRicochetReach"/> of the one it struck, once for each of these cards.
    /// </summary>
    Ricochet,

    /// <summary>
    /// A self card, The Burst (plan T25): where a thrown card strikes, every other critic within
    /// <see cref="Tuning.CardBurstRadius"/> of the one struck takes <see cref="Tuning.CardBurstShare"/> of what
    /// the card hurts for, for each of these cards.
    /// </summary>
    Burst,
}

/// <summary>
/// The self cards somebody has: how many of each. The magician's are those it has taken in this performance, and
/// an understudy's those the magician had on the same tick of the act it was recorded in (plan decision 20, plan
/// T24): what that act began with, and each card of its encores from its tick on. The numbers a throw runs on are
/// the tuning's as these change them.
/// </summary>
public readonly record struct SelfCards(
    int Damage = 0,
    int AttackSpeed = 0,
    int Range = 0,
    int VanishCooldown = 0,
    int OneMoreCard = 0,
    int Pierce = 0,
    int Ricochet = 0,
    int Burst = 0)
{
    /// <summary>How many of <paramref name="card"/> these are: none of a chorus card, which is nobody's own.</summary>
    public int Of(Card card) => card switch
    {
        Card.Damage => Damage,
        Card.AttackSpeed => AttackSpeed,
        Card.Range => Range,
        Card.VanishCooldown => VanishCooldown,
        Card.OneMoreCard => OneMoreCard,
        Card.Pierce => Pierce,
        Card.Ricochet => Ricochet,
        Card.Burst => Burst,
        _ => 0,
    };

    /// <summary>These and one more self card. A chorus card is nobody's own.</summary>
    internal SelfCards With(Card card) => card switch
    {
        Card.Damage => this with { Damage = Damage + 1 },
        Card.AttackSpeed => this with { AttackSpeed = AttackSpeed + 1 },
        Card.Range => this with { Range = Range + 1 },
        Card.VanishCooldown => this with { VanishCooldown = VanishCooldown + 1 },
        Card.OneMoreCard => this with { OneMoreCard = OneMoreCard + 1 },
        Card.Pierce => this with { Pierce = Pierce + 1 },
        Card.Ricochet => this with { Ricochet = Ricochet + 1 },
        Card.Burst => this with { Burst = Burst + 1 },
        _ => this,
    };
}
