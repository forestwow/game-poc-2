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
        int burst = Simulation.Ticks(tuning.WaveBurstTime);
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

                // A share of them, picked evenly down the list, do not wait for their own tick: each enters at
                // the last moment of a crowd before it, so those of one stretch between two crowds come in
                // together.
                // ponytail: a crowd is all on one tick, and those of it at one door stand in the door's width on
                // top of each other until the push-apart has spread them, one pass a tick: some thirty at a door
                // three units wide by the tenth act. Entering a few ticks apart is what mends it when it shows.
                if (burst > 0 && (int)((i + 1) * tuning.WaveBurstShare) > (int)(i * tuning.WaveBurstShare))
                {
                    tick -= tick % burst;
                }

                entries.Add(new PlannedEntry(tick, open[rng.NextInt(open.Length)], bought[i]));
            }

            // A crowd's enemies were moved ahead of some that enter before them: the plan is in the order of
            // the ticks again, and those of one tick in the order they were bought.
            acts.Add([.. entries.OrderBy(entry => entry.Tick)]);
        }

        return acts;
    }

    /// <summary>
    /// What an act buys, in the order it buys it: a weighted draw among the kinds it may have and can still afford,
    /// until it can afford none, with those it has a number of (<see cref="EnemyKind.InAnAct"/>) spread among
    /// them.
    /// </summary>
    private static List<int> Buy(Rng rng, Tuning tuning, int act)
    {
        // Every act has the step more than the one before it, and the step itself is bigger by the rise in every
        // act after the second: the steps so far add up to this.
        int left = tuning.FirstActBudget
            + ((act - 1) * tuning.BudgetGrowthPerAct)
            + (tuning.BudgetGrowthRise * (act - 1) * (act - 2) / 2);

        // What an act has a number of (plan T46) it pays for first, as many as it can, and draws nothing for.
        var few = new List<int>();
        for (int kind = 0; kind < tuning.EnemyKinds.Count; kind++)
        {
            EnemyKind its = tuning.EnemyKinds[kind];
            for (int n = 0; n < its.InAnAct && its.FromAct <= act && its.Cost <= left; n++)
            {
                few.Add(kind);
                left -= its.Cost;
            }
        }

        var bought = new List<int>();
        while (true)
        {
            bool Affordable(EnemyKind kind) => kind.Weight > 0 && kind.FromAct <= act && kind.Cost <= left;

            int total = tuning.EnemyKinds.Where(Affordable).Sum(kind => kind.Weight);
            if (total == 0)
            {
                // The few stand in the order of buying each in a stretch of its own, the last first so that no
                // place moves: an act with four has one somewhere in each quarter. An act that has none draws
                // nothing here, so a kind's number changes no act before the kind's own.
                // ponytail: two kinds with a number are laid as one list, a kind after a kind, so each has its
                // own part of the act. Deal them round when there are two.
                int drawn = bought.Count;
                for (int n = few.Count - 1; n >= 0; n--)
                {
                    bought.Insert((int)((n + rng.NextFloat()) * drawn / few.Count), few[n]);
                }

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
