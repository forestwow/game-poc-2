using System.Text.Json;
using System.Text.Json.Serialization;

namespace Understudies.Core;

/// <summary>
/// One night of the ladder (plan T51), as nights.json holds it: its number and what it overrides of tuning.json. A
/// member is the key of the same name in camelCase, the number's is <c>night</c>, and a key that is left out
/// overrides nothing: a night with its number alone is the plain night. The file is a list of these, in the order
/// of their numbers. No rule knows of a night: a <see cref="Simulation"/> is given the tuning that
/// <see cref="Compose"/> makes.
/// </summary>
/// <param name="Number">Which night it is, 1 or more.</param>
/// <param name="ActsInPerformance">The night's <see cref="Tuning.ActsInPerformance"/>.</param>
/// <param name="BudgetScale">
/// What the night multiplies the three numbers of an act's budget by (<see cref="Tuning.FirstActBudget"/>,
/// <see cref="Tuning.BudgetGrowthPerAct"/> and <see cref="Tuning.BudgetGrowthRise"/>), each rounded to the nearest
/// whole number and a half up. A decimal, so that 0.6 is six tenths and nothing beside it.
/// </param>
/// <param name="KindsAllowed">
/// The kinds the night's acts may buy, by <see cref="EnemyKind.Name"/>. One that is not named is never bought.
/// </param>
/// <param name="WaveBurstShare">The night's <see cref="Tuning.WaveBurstShare"/>: with 0 it has no crowds.</param>
/// <param name="WaveBurstTime">The night's <see cref="Tuning.WaveBurstTime"/>.</param>
public sealed record Night(
    [property: JsonPropertyName("night"), JsonRequired] int Number,
    int? ActsInPerformance,
    decimal? BudgetScale,
    IReadOnlyList<string>? KindsAllowed,
    float? WaveBurstShare,
    float? WaveBurstTime)
{
    // As strict as the tuning's, but for one thing: every key but the number may be left out.
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        AllowDuplicateProperties = false,
    };

    /// <summary>Reads the text of a nights.json.</summary>
    /// <exception cref="JsonException">
    /// The text is not the nights. For an unknown key, a key written twice and a night with no number the message
    /// names the key; for a night that is not after the one before it (twice, out of order, less than 1) its number.
    /// </exception>
    public static IReadOnlyList<Night> Parse(string json)
    {
        IReadOnlyList<Night> nights = JsonSerializer.Deserialize<IReadOnlyList<Night>>(json, Options)
            ?? throw new JsonException("The nights are null.");

        // ponytail: as in the tuning, no number is checked for making sense (a scale below nothing, a share over
        // 1), and a key that says null is a key left out. Check them here when the file is edited by more than
        // its owner.
        int before = 0;
        foreach (Night? night in nights)
        {
            if (night is null)
            {
                throw new JsonException($"The night after night {before} is null.");
            }

            if (night.Number <= before)
            {
                throw new JsonException(
                    $"'night' is {night.Number} after night {before}: night {night.Number} is out of its place. "
                    + "The nights are numbered from 1 and stand in order, each once.");
            }

            before = night.Number;
        }

        return nights;
    }

    /// <summary>
    /// The tuning of a night: the plain tuning with what the night overrides, and the plain tuning itself for a
    /// night that overrides nothing. The list of kinds keeps its length and its order, since a planned entry and
    /// the view name a kind by its place: a kind the night does not allow is there with no weight and no number
    /// for an act, which is a kind that is never bought and takes no part in an act's draws, as a kind of a later
    /// act takes none.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The nights have no night of that number.</exception>
    /// <exception cref="JsonException">The night allows a kind the tuning does not have.</exception>
    public static Tuning Compose(Tuning plain, IReadOnlyList<Night> nights, int number)
    {
        Night night = nights.FirstOrDefault(night => night.Number == number)
            ?? throw new ArgumentOutOfRangeException(nameof(number), $"The nights have no night {number}.");

        Tuning tuning = plain with
        {
            ActsInPerformance = night.ActsInPerformance ?? plain.ActsInPerformance,
            WaveBurstShare = night.WaveBurstShare ?? plain.WaveBurstShare,
            WaveBurstTime = night.WaveBurstTime ?? plain.WaveBurstTime,
        };

        if (night.BudgetScale is { } scale)
        {
            // All three, or the rise, which is most of a late act's budget, would make a night's last acts as
            // full as the plain night's. A kind's number for an act is not scaled: it is paid out of the act's
            // budget, so the act has the scale's share of the plain act's enemies whatever that number is.
            int Scaled(int budget) => (int)Math.Round(budget * scale, MidpointRounding.AwayFromZero);

            tuning = tuning with
            {
                FirstActBudget = Scaled(plain.FirstActBudget),
                BudgetGrowthPerAct = Scaled(plain.BudgetGrowthPerAct),
                BudgetGrowthRise = Scaled(plain.BudgetGrowthRise),
            };
        }

        if (night.KindsAllowed is { } allowed)
        {
            if (allowed.FirstOrDefault(name => plain.EnemyKinds.All(kind => kind.Name != name)) is { } unknown)
            {
                throw new JsonException(
                    $"'kindsAllowed' of night {number} names '{unknown}', which 'enemyKinds' does not have.");
            }

            tuning = tuning with
            {
                EnemyKinds = [.. plain.EnemyKinds.Select(
                    kind => allowed.Contains(kind.Name) ? kind : kind with { Weight = 0, InAnAct = 0 })],
            };
        }

        return tuning;
    }
}
