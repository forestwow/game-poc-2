using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Understudies.Core;

/// <summary>
/// Every tunable number of the game, in world units and seconds, as tuning.json holds them: a member here is the
/// key of the same name there in camelCase, and a point is written <c>{ "x": 1, "y": 2 }</c>.
/// </summary>
/// <param name="ActLength">Seconds an act is played for: its timer ends it, whatever is on the stage.</param>
/// <param name="ActsInPerformance">How many acts a performance is: the last ends in the standing ovation.</param>
/// <param name="CurtainTime">
/// Seconds the curtain is up for at the start of every act, with the stage standing. With no time there is no
/// curtain.
/// </param>
/// <param name="StageSize">
/// The whole stage, the back wall at its top and the floor below it: (0, 0) is its top-left corner and y grows
/// downward.
/// </param>
/// <param name="StageFloorTop">
/// The y of the foot of the back wall, where the floor that can be walked starts. At 0 there is no wall.
/// </param>
/// <param name="StageDoors">
/// The stage doors. No door may be up the back wall, and one must be open in the first act.
/// </param>
/// <param name="StageDoorWidth">
/// How wide a door is, along the stage's width: a critic enters with its whole circle anywhere within it. No door
/// may reach past a side of the stage.
/// </param>
/// <param name="BoxOfficePosition">The centre of the box office's circle on the floor.</param>
/// <param name="BoxOfficeSize">The box office is this wide and this tall; its circle's radius is half of it.</param>
/// <param name="BoxOfficeHitPoints">What the box office has when the show starts.</param>
/// <param name="MagicianMark">Where the magician stands when an act starts.</param>
/// <param name="MagicianSpeed">Units per second.</param>
/// <param name="MagicianRadius">The magician is a circle on the floor.</param>
/// <param name="MagicianHitPoints">What the magician has when an act starts.</param>
/// <param name="VanishDistance">How far the Vanish takes the magician, when no edge of the floor is in the way.</param>
/// <param name="VanishCooldown">Seconds from one Vanish to the next.</param>
/// <param name="VanishInvulnerableTime">Seconds from a Vanish in which nothing hurts the magician.</param>
/// <param name="VanishCloudRadius">The cloud a Vanish leaves is a circle on the floor.</param>
/// <param name="VanishCloudTime">Seconds the cloud is there.</param>
/// <param name="VanishStunTime">
/// Seconds a critic is stunned, counted from the last tick its circle touched a cloud.
/// </param>
/// <param name="ThrowRange">
/// The magician throws at a critic whose centre is no further than this, and a thrown card flies this far.
/// </param>
/// <param name="ThrowCooldown">Seconds from one throw to the next.</param>
/// <param name="ThrownCardSpeed">
/// Units per second. The slower the card, the further ahead of a moving target it is thrown, and the further a
/// target that stops or turns has strayed when it gets there.
/// </param>
/// <param name="ThrownCardDamage">The hit points one card takes off a critic.</param>
/// <param name="EnemyKinds">
/// The kinds of enemy an act may buy, at least one. A planned entry names its kind by its place in this list.
/// </param>
/// <param name="FirstActBudget">What the first act has to buy its enemies with.</param>
/// <param name="BudgetGrowthPerAct">How much more the second act has than the first: the step from act to act.</param>
/// <param name="BudgetGrowthRise">
/// How much bigger that step is in every act after the second: the third act has the step and this more than the
/// second, the fourth the step and twice this more than the third.
/// </param>
/// <param name="WaveBurstShare">
/// The share of an act's enemies that enter in crowds, from 0 to 1: with 0.25 every fourth. With nothing nobody
/// does.
/// </param>
/// <param name="WaveBurstTime">
/// Seconds from one crowd to the next, the first as the act begins: an enemy of a crowd enters at the last such
/// moment before its own time.
/// </param>
/// <param name="ActQuietEnd">Seconds at the end of an act in which nobody enters.</param>
/// <param name="CriticTurnRadius">
/// A critic of a kind that turns, whose centre is nearer than this to the magician's centre, turns on the magician;
/// any other walks to the box office. A critic that has not turned does not hurt the magician, so a radius of
/// nothing leaves the magician alone.
/// </param>
/// <param name="CriticTouchDamage">The hit points one touch takes off the magician.</param>
/// <param name="CriticBlowCooldown">
/// Seconds from one blow of a critic to its next, whether a blow is a strike on the box office or a touch that hurts
/// the magician.
/// </param>
/// <param name="ApplauseTime">
/// Seconds a piece of applause lies on the floor before it is gone. It can be picked up from the tick after it
/// is dropped, on one tick fewer than it is seen.
/// </param>
/// <param name="ApplausePickUpReach">
/// The magician picks up a piece whose centre is no further from its own than its radius and this.
/// </param>
/// <param name="ApplauseBoxOfficeRadius">
/// A critic that falls nearer than this to the centre of the box office leaves no applause. With nothing every
/// fall leaves its piece.
/// </param>
/// <param name="EncoreFirstCost">
/// The pieces of applause the first encore of a performance costs. The pieces are an act's own: what an act has
/// not spent is lost when it is over.
/// </param>
/// <param name="EncoreCostGrowth">How many pieces more every encore costs than the one taken before it.</param>
/// <param name="EncoreTime">
/// Seconds an encore waits for a pick before it takes its leftmost card, counted as
/// <paramref name="ProgramTime"/> is.
/// </param>
/// <param name="ProgramTime">
/// Seconds the program waits for a pick before it takes its card. With no time the program is still up when the
/// act is over, and the first tick takes the card.
/// </param>
/// <param name="CardDamage">The hit points one damage card adds to what a thrown card takes off a critic.</param>
/// <param name="CardAttackSpeed">
/// What one attack speed card adds to the rate of the throw, as a share of the rate without cards: with 0.2 one
/// card makes the time from throw to throw the cooldown over 1.2, and two over 1.4.
/// </param>
/// <param name="CardRange">The units one range card adds to the throw's range.</param>
/// <param name="CardVanishCooldown">
/// What one Vanish cooldown card adds to the rate at which the Vanish comes back, counted as
/// <paramref name="CardAttackSpeed"/> is.
/// </param>
/// <param name="CardChorusDamage">
/// The hit points one chorus card adds to what a card thrown by an understudy takes off a critic.
/// </param>
/// <param name="CardPierceLoss">
/// The hit points a strike takes off what a card of a thrower with one pierce card hurts for; with more of them
/// it is this over their number. A card with nothing left is spent.
/// </param>
/// <param name="CardRicochetReach">
/// How far from the critic a card struck, middle to middle, another may stand for the card of a thrower with a
/// ricochet card to turn to it; a card that turns flies that far at least.
/// </param>
/// <param name="CardBurstRadius">
/// How far from the critic a card struck, middle to middle, another may stand to be hurt by the burst of a
/// thrower with a burst card.
/// </param>
/// <param name="CardBurstShare">
/// The share of what a card hurts for that its burst takes off each of those critics, for one burst card: two
/// make it twice the share.
/// </param>
/// <param name="CardMaxCopies">
/// How many of one self card the magician may hold (plan T41, decision 31): an encore does not offer a card it
/// holds this many of. With nothing, or less, there is no limit.
/// </param>
public sealed record Tuning(
    float ActLength,
    int ActsInPerformance,
    float CurtainTime,
    Vector2 StageSize,
    float StageFloorTop,
    IReadOnlyList<StageDoor> StageDoors,
    float StageDoorWidth,
    Vector2 BoxOfficePosition,
    float BoxOfficeSize,
    float BoxOfficeHitPoints,
    Vector2 MagicianMark,
    float MagicianSpeed,
    float MagicianRadius,
    float MagicianHitPoints,
    float VanishDistance,
    float VanishCooldown,
    float VanishInvulnerableTime,
    float VanishCloudRadius,
    float VanishCloudTime,
    float VanishStunTime,
    float ThrowRange,
    float ThrowCooldown,
    float ThrownCardSpeed,
    float ThrownCardDamage,
    IReadOnlyList<EnemyKind> EnemyKinds,
    int FirstActBudget,
    int BudgetGrowthPerAct,
    int BudgetGrowthRise,
    float WaveBurstShare,
    float WaveBurstTime,
    float ActQuietEnd,
    float CriticTurnRadius,
    float CriticTouchDamage,
    float CriticBlowCooldown,
    float ApplauseTime,
    float ApplausePickUpReach,
    float ApplauseBoxOfficeRadius,
    int EncoreFirstCost,
    int EncoreCostGrowth,
    float EncoreTime,
    float ProgramTime,
    float CardDamage,
    float CardAttackSpeed,
    float CardRange,
    float CardVanishCooldown,
    float CardChorusDamage,
    float CardPierceLoss,
    float CardRicochetReach,
    float CardBurstRadius,
    float CardBurstShare,
    int CardMaxCopies)
{
    // ponytail: the serializer reads the types by reflection; a trimmed or AOT build needs a source-generated context.
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // A key that no member answers to is an error.
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        // A key written twice is an error: the last one would win in silence.
        AllowDuplicateProperties = false,
        // A Vector2's X and Y are fields.
        IncludeFields = true,
        // A missing key is an error at any depth. Marking every key here asks it of a point's x and y too, which
        // `required` members of this record could not.
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                static type =>
                {
                    foreach (JsonPropertyInfo key in type.Properties)
                    {
                        key.IsRequired = true;
                    }
                },
            },
        },
    };

    /// <summary>Reads the text of a tuning.json.</summary>
    /// <exception cref="JsonException">
    /// The text is not a tuning. For an unknown key, a missing one, no stage door, a door above the floor's top or past a side, no
    /// door open in the first act, no kind of enemy and a kind that costs nothing the message names the key.
    /// </exception>
    public static Tuning Parse(string json)
    {
        Tuning tuning = JsonSerializer.Deserialize<Tuning>(json, Options) ?? throw new JsonException("The tuning is null.");

        // ponytail: no number is checked for making sense. An act of no length is over when its curtain is, and a
        // performance of no acts or fewer plays one. Check ranges here when a file is edited by more than its owner.
        if (tuning.StageDoors is not { Count: > 0 })
        {
            throw new JsonException("'stageDoors' needs at least one door.");
        }

        // What the first act buys has to enter somewhere, and a door that has opened stays open.
        if (!tuning.StageDoors.Any(door => door.OpensInAct <= 1))
        {
            throw new JsonException("'stageDoors' needs a door whose 'opensInAct' is 1: the first act has no way in.");
        }

        // The rules take the first kind for granted.
        if (tuning.EnemyKinds is not { Count: > 0 })
        {
            throw new JsonException("'enemyKinds' needs at least one kind.");
        }

        // An act buys until it can afford nothing: a kind that costs nothing would be bought for ever.
        if (tuning.EnemyKinds.Any(kind => kind.Cost < 1))
        {
            throw new JsonException("'enemyKinds' has a kind whose 'cost' is less than 1.");
        }

        // A door up the back wall would let its critics in on the wall.
        if (tuning.StageDoors.Any(door => door.Position.Y < tuning.StageFloorTop))
        {
            throw new JsonException(
                "'stageDoors' has a door whose y is less than 'stageFloorTop': it would be up the back wall.");
        }

        // A door runs along the stage's width (plan T49): one that reaches past a side would let a critic in
        // outside the stage.
        float half = tuning.StageDoorWidth / 2f;
        if (tuning.StageDoors.Any(door => door.Position.X < half || door.Position.X > tuning.StageSize.X - half))
        {
            throw new JsonException(
                "'stageDoors' has a door nearer than half of 'stageDoorWidth' to a side of the stage: its critics would enter outside it.");
        }

        return tuning;
    }
}

/// <summary>A stage door: critics enter anywhere along it, in the acts in which it is open.</summary>
/// <param name="Position">
/// The door's middle, a point of the floor: the door runs half its width to either side of it, along the stage's
/// width (plan T49), and its critics enter on that line.
/// </param>
/// <param name="OpensInAct">The number of the first act in which the door is open: it is open ever after.</param>
public readonly record struct StageDoor(Vector2 Position, int OpensInAct);

/// <summary>A kind of enemy: what the critics of that kind are, and what an act pays for one.</summary>
/// <param name="Name">What the file calls the kind. No rule reads it.</param>
/// <param name="Speed">Units per second.</param>
/// <param name="Radius">A critic is a circle on the floor.</param>
/// <param name="HitPoints">What a critic has when it enters, in the kind's first act.</param>
/// <param name="HitPointsPerAct">
/// How much more one has when it enters for every act after the kind's first (<paramref name="FromAct"/>): with
/// nothing the kind is as tough in the last act as in its first.
/// </param>
/// <param name="StrikeDamage">The hit points one strike of a critic of the kind takes off the box office.</param>
/// <param name="Cost">What one takes from an act's budget, 1 or more.</param>
/// <param name="Weight">
/// How likely an act is to buy this kind and not another it can afford: the weight's share of all their weights. A
/// kind with no weight is never drawn: an act has it only by <paramref name="InAnAct"/>, and with neither it is
/// never bought.
/// </param>
/// <param name="FromAct">The number of the first act that may buy the kind.</param>
/// <param name="TurnsOnTheMagician">
/// Whether the kind turns on a magician that comes within <see cref="Tuning.CriticTurnRadius"/>.
/// </param>
/// <param name="UnderstudyDamageShare">
/// The share of its damage that an understudy's card, or its burst, takes off a critic of the kind: 1 for a kind
/// that an understudy hurts as the magician does, and less for one that is the magician's to fell (plan T46).
/// </param>
/// <param name="InAnAct">
/// How many of the kind every act from <paramref name="FromAct"/> has whatever is drawn, paid for before anything
/// is drawn and spread evenly over the act (plan T46): with nothing the kind is only drawn for, by its weight.
/// </param>
public readonly record struct EnemyKind(
    string Name,
    float Speed,
    float Radius,
    float HitPoints,
    float HitPointsPerAct,
    float StrikeDamage,
    int Cost,
    int Weight,
    int FromAct,
    bool TurnsOnTheMagician,
    float UnderstudyDamageShare,
    int InAnAct);
