using Understudies.Core.Randomness;

namespace Understudies.Core;

/// <summary>One critic of an act's plan: when it enters, by which door and of which kind.</summary>
/// <param name="Tick">The tick of its act on which it enters, the act's first being 0.</param>
/// <param name="Door">The door's place in <see cref="Tuning.StageDoors"/>.</param>
/// <param name="Kind">The kind's place in <see cref="Tuning.EnemyKinds"/>.</param>
public readonly record struct PlannedEntry(int Tick, int Door, int Kind);

/// <summary>
/// The plan of a whole performance's waves, after the generator of faith-defense: every act has a budget and buys
/// critics with it, and each of them is given a tick of the act and one of the doors open in it.
/// </summary>
public static class Waves
{
    /// <summary>
    /// The entries of every act of a performance, an act after an act, each act's in the order of their ticks. The
    /// same tuning and the same seed give the same plan.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<PlannedEntry>> Plan(Tuning tuning, ulong seed)
    {
        Rng rng = Rng.ForStream(seed, RngStream.Waves);

        // Nobody enters in an act's last seconds: a critic that enters as the act ends is one nobody could meet.
        int window = Math.Max(1, Simulation.Ticks(tuning.ActLength - tuning.ActQuietEnd));
        var acts = new List<IReadOnlyList<PlannedEntry>>();
        for (int act = 1; act <= tuning.ActsInPerformance; act++)
        {
            List<int> bought = Buy(rng, tuning, act);
            int[] open = [.. Enumerable.Range(0, tuning.StageDoors.Count)
                .Where(door => tuning.StageDoors[door].OpensInAct <= act)];

            // Spread evenly: the time for entering is cut into as many stretches as there are critics, and each
            // enters somewhere in its own, by any of the open doors.
            var entries = new List<PlannedEntry>(bought.Count);
            for (int i = 0; i < bought.Count; i++)
            {
                int tick = Math.Min(window - 1, (int)((i + rng.NextFloat()) * window / bought.Count));
                entries.Add(new PlannedEntry(tick, open[rng.NextInt(open.Length)], bought[i]));
            }

            acts.Add(entries);
        }

        return acts;
    }

    /// <summary>
    /// What an act buys, in the order it buys it: a weighted draw among the kinds it may have and can still afford,
    /// until it can afford none.
    /// </summary>
    private static List<int> Buy(Rng rng, Tuning tuning, int act)
    {
        int left = tuning.FirstActBudget + ((act - 1) * tuning.BudgetGrowthPerAct);
        var bought = new List<int>();
        while (true)
        {
            bool Affordable(EnemyKind kind) => kind.Weight > 0 && kind.FromAct <= act && kind.Cost <= left;

            int total = tuning.EnemyKinds.Where(Affordable).Sum(kind => kind.Weight);
            if (total == 0)
            {
                return bought;
            }

            int roll = rng.NextInt(total);
            for (int kind = 0; kind < tuning.EnemyKinds.Count; kind++)
            {
                if (!Affordable(tuning.EnemyKinds[kind]))
                {
                    continue;
                }

                if (roll < tuning.EnemyKinds[kind].Weight)
                {
                    bought.Add(kind);
                    left -= tuning.EnemyKinds[kind].Cost;
                    break;
                }

                roll -= tuning.EnemyKinds[kind].Weight;
            }
        }
    }
}
