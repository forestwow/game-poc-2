namespace Understudies.Core.Tests;

/// <summary>
/// Shows for the tests of the other rules, which say exactly who enters when and draw no plan: every critic is of
/// the first kind, unless a show says another, and enters by the first door.
/// </summary>
internal static class Shows
{
    /// <summary>A show whose critics enter on these ticks of the first act, the first tick being 0.</summary>
    public static Simulation WithCriticsOnTicks(Tuning tuning, params int[] ticks) =>
        new(tuning, seed: 1, [[.. ticks.Select(tick => new PlannedEntry(tick, Door: 0, Kind: 0))]]);

    /// <summary>A show with one critic, which enters on the first tick.</summary>
    public static Simulation WithOneCritic(Tuning tuning) => WithCriticsOnTicks(tuning, 0);

    /// <summary>
    /// A show with one enemy of a kind, by the kind's place in <see cref="Tuning.EnemyKinds"/>, which enters on the
    /// first tick.
    /// </summary>
    public static Simulation WithOneOfKind(Tuning tuning, int kind) =>
        new(tuning, seed: 1, [[new PlannedEntry(Tick: 0, Door: 0, kind)]]);

    /// <summary>
    /// A show with a steady stream: a critic enters on the first tick of the performance and on every
    /// <paramref name="ticks"/>th after it, counted on through the acts as <paramref name="tuning"/> has them, so
    /// that two acts are let into as one of twice the length is.
    /// </summary>
    public static Simulation WithACriticEvery(int ticks, Tuning tuning, ulong seed = 1)
    {
        int actTicks = (int)((tuning.ActLength * Simulation.TicksPerSecond) + 0.5f);
        var plan = new List<PlannedEntry>[tuning.ActsInPerformance];
        for (int act = 0; act < plan.Length; act++)
        {
            plan[act] = [];
        }

        for (int tick = 0; tick < actTicks * plan.Length; tick += ticks)
        {
            plan[tick / actTicks].Add(new PlannedEntry(tick % actTicks, Door: 0, Kind: 0));
        }

        return new Simulation(tuning, seed, plan);
    }

    /// <summary>The first kind of enemy, which is the critic.</summary>
    public static EnemyKind Critic(this Tuning tuning) => tuning.EnemyKinds[0];

    /// <summary>The second kind of enemy, which is the stagehand.</summary>
    public static EnemyKind Stagehand(this Tuning tuning) => tuning.EnemyKinds[1];

    /// <summary>The third kind of enemy, which is the rival's understudy.</summary>
    public static EnemyKind Rival(this Tuning tuning) => tuning.EnemyKinds[2];

    /// <summary>The tuning with every kind's strike on the box office taking this much.</summary>
    public static Tuning WithStrikesOf(this Tuning tuning, float damage) =>
        tuning with { EnemyKinds = [.. tuning.EnemyKinds.Select(kind => kind with { StrikeDamage = damage })] };

    /// <summary>The tuning with its first kind of enemy changed, and that kind alone on its list.</summary>
    public static Tuning WithCritic(this Tuning tuning, Func<EnemyKind, EnemyKind> change) =>
        tuning with { EnemyKinds = [change(tuning.Critic())] };
}
