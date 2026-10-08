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
/// The kinds the night's acts may buy, by <see cref="EnemyKind.Name"/>, at least one. One that is not named is
/// never bought.
/// </param>
/// <param name="WaveBurstShare">The night's <see cref="Tuning.WaveBurstShare"/>: with 0 it has no crowds.</param>
/// <param name="Name">
/// The night's name on its poster (plan T55; the key is <c>name</c>). It overrides nothing and no rule reads it:
/// <see cref="Compose"/> does not look at it. It may be left out as every other key may, and the poster then has
/// the night's number alone; a name that is there has words in it.
/// </param>
/// <param name="Rules">
/// The night's house rules, by their names (plan T56; the key is <c>rules</c>), each one of
/// <see cref="RuleNames"/> and each once. Left out or empty, the night has none. A rule is a name and nothing
/// more here: <see cref="Compose"/> does not look at it, so the tuning a <see cref="Simulation"/> is given says
/// nothing of a rule, and whoever a rule is for asks the night (<see cref="Has"/>). The one rule there is, the
/// spotlight night, is the view's alone.
/// </param>
/// <param name="Note">
/// One sentence the night's poster says beside what is new (plan T58; the key is <c>note</c>): night 3's names
/// the quiet floor. The night's own words, as its name is: it overrides nothing, no rule reads it and
/// <see cref="Compose"/> does not look at it. Left out, the poster has no such row; one that is there has
/// words in it.
/// ponytail: the sentence is the file's and nothing holds it to the rule it speaks of. It has no number in it,
/// so it is wrong only when the rule itself goes (an applauseBoxOfficeRadius of nothing): a note by a name,
/// with its sentence made from the tuning as a card's is, when a second night has one that must carry a number.
/// </param>
public sealed record Night(
    [property: JsonPropertyName("night"), JsonRequired] int Number,
    int? ActsInPerformance,
    decimal? BudgetScale,
    IReadOnlyList<string>? KindsAllowed,
    float? WaveBurstShare,
    string? Name,
    IReadOnlyList<string>? Rules,
    string? Note)
{
    /// <summary>
    /// The spotlight night (the ladder's document, §1.4): what is dark is not drawn, and is still there. Only a
    /// circle of <see cref="Tuning.SpotlightRadius"/> round the magician and a pool of light at every open door
    /// are lit. No rule of the simulation knows of it.
    /// </summary>
    public const string Spotlight = "spotlight";

    /// <summary>
    /// Every house rule there is, by its name.
    /// ponytail: a night's rules are written in the file. The document draws them from pools by the show's seed
    /// (a stream RngStream.House, the compatibility check): build the draw with the second rule, when there is
    /// something to draw from.
    /// </summary>
    public static readonly IReadOnlyList<string> RuleNames = [Spotlight];

    /// <summary>Whether the night has the house rule of that name.</summary>
    public bool Has(string rule) => Rules?.Contains(rule) ?? false;

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
    /// names the key; for a night that is not after the one before it (twice, out of order, less than 1) its number;
    /// for a list of kinds that is empty or has a null in it, and for a name or a note of no words, the key and
    /// the night;
    /// for a rule that is not one of <see cref="RuleNames"/> or is there twice, the key, the night and the rule.
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

            // A list of no kinds would shut every kind, and a night of nobody is not something left out by a slip.
            if (night.KindsAllowed is { } allowed && (allowed.Count == 0 || allowed.Contains(null!)))
            {
                throw new JsonException(
                    $"'kindsAllowed' of night {night.Number} is empty or has a null in it: it names the kinds, at least one.");
            }

            if (night.Name is { } name && string.IsNullOrWhiteSpace(name))
            {
                throw new JsonException($"'name' of night {night.Number} has no words in it: leave the key out for a night with no name.");
            }

            if (night.Note is { } note && string.IsNullOrWhiteSpace(note))
            {
                throw new JsonException($"'note' of night {night.Number} has no words in it: leave the key out for a night with no note.");
            }

            // An unknown rule is refused here and not at the composition, as an unknown key is: a rule is the
            // game's own word, where a kind's name is the tuning's.
            for (int i = 0; i < (night.Rules?.Count ?? 0); i++)
            {
                string rule = night.Rules![i];
                if (!RuleNames.Contains(rule))
                {
                    throw new JsonException(
                        $"'rules' of night {night.Number} names '{rule}', which is no house rule: there is {string.Join(", ", RuleNames)}.");
                }

                if (night.Rules.Take(i).Contains(rule))
                {
                    throw new JsonException($"'rules' of night {night.Number} names '{rule}' twice.");
                }
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
            foreach (string name in allowed.Where(name => plain.EnemyKinds.All(kind => kind.Name != name)))
            {
                throw new JsonException(
                    $"'kindsAllowed' of night {number} names '{name}', which 'enemyKinds' does not have.");
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
