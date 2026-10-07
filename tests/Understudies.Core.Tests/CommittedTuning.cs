namespace Understudies.Core.Tests;

/// <summary>The repository's tuning.json, which the project file copies beside the tests.</summary>
internal static class CommittedTuning
{
    public static string Json =>
        File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "tuning.json"));

    public static Tuning Parse() => Tuning.Parse(Json);
}
