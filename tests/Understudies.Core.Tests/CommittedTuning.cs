namespace Understudies.Core.Tests;

/// <summary>The repository's tuning.json, which the project file copies beside the tests.</summary>
internal static class CommittedTuning
{
    public static string Json =>
        File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "tuning.json"));

    public static Tuning Parse() => Tuning.Parse(Json);
}

/// <summary>The repository's nights.json, copied beside the tests as tuning.json is.</summary>
internal static class CommittedNights
{
    public static string Json =>
        File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "nights.json"));

    public static IReadOnlyList<Night> Parse() => Night.Parse(Json);

    /// <summary>The committed tuning under the committed overlay of a night.</summary>
    public static Tuning Tuning(int night) => Night.Compose(CommittedTuning.Parse(), Parse(), night);
}
