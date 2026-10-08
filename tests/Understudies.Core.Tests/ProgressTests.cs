using System.Text.Json;

namespace Understudies.Core.Tests;

/// <summary>
/// What the player has done (plan T54): the record, its text, the gates that unlock a night, and its file. No test
/// here goes near the player's own folder: the file's tests are given a folder of their own.
/// </summary>
public class ProgressTests
{
    private string _folder = null!;

    private string ThePath => Path.Combine(_folder, "progress.json");

    [SetUp]
    public void MakeAFolder() => _folder = Directory.CreateTempSubdirectory("understudies-tests-").FullName;

    [TearDown]
    public void DeleteTheFolder() => Directory.Delete(_folder, recursive: true);

    /// <summary>Nights with nothing but their numbers: a gate reads the numbers alone.</summary>
    private static IReadOnlyList<Night> Nights(params int[] numbers) =>
        Night.Parse("[" + string.Join(",", numbers.Select(number => $$"""{ "night": {{number}} }""")) + "]");

    [Test]
    public void ToJson_AndParse_GiveTheSameProgress()
    {
        Progress progress = Progress.None.With(1, 5, won: true).With(10, 7, won: false).With(2, 3, won: false);

        Progress read = Progress.Parse(progress.ToJson());

        Assert.That(read.Nights, Is.EqualTo(new[] { new NightPlayed(1, 5, true), new NightPlayed(2, 3, false), new NightPlayed(10, 7, false) }));
    }

    [Test]
    public void ToJson_IsTheShapeTheFileHas()
    {
        using JsonDocument text = JsonDocument.Parse(Progress.None.With(2, 4, won: false).ToJson());

        Assert.That(text.RootElement.GetProperty("version").GetInt32(), Is.EqualTo(1));
        JsonElement night = text.RootElement.GetProperty("nights")[0];
        Assert.That(night.GetProperty("night").GetInt32(), Is.EqualTo(2));
        Assert.That(night.GetProperty("bestAct").GetInt32(), Is.EqualTo(4));
        Assert.That(night.GetProperty("won").GetBoolean(), Is.False);
    }

    [Test]
    public void With_ANightPlayedAgain_KeepsTheBestActAndAWin()
    {
        Progress progress = Progress.None.With(2, 6, won: false).With(2, 7, won: true).With(2, 3, won: false);

        Assert.That(progress.Nights, Is.EqualTo(new[] { new NightPlayed(2, 7, true) }));
    }

    [TestCase("", TestName = "Parse_AnEmptyText_IsRefused")]
    [TestCase("null", TestName = "Parse_Null_IsRefused")]
    [TestCase("[]", TestName = "Parse_AnotherShape_IsRefused")]
    [TestCase("""{ "version": 1, "nights": [ { "night": 1, "bestAct": 2, "won": fal""", TestName = "Parse_HalfAFile_IsRefused")]
    [TestCase("""{ "nights": [] }""", TestName = "Parse_NoVersion_IsRefused")]
    [TestCase("""{ "version": 1 }""", TestName = "Parse_NoNights_IsRefused")]
    [TestCase("""{ "version": 1, "nights": null }""", TestName = "Parse_NightsThatAreNull_AreRefused")]
    [TestCase("""{ "version": 0, "nights": [] }""", TestName = "Parse_AnOlderVersion_IsRefused")]
    [TestCase("""{ "version": 2, "nights": [] }""", TestName = "Parse_ANewerVersion_IsRefused")]
    [TestCase("""{ "version": 1, "nights": [], "score": 3 }""", TestName = "Parse_AnUnknownKey_IsRefused")]
    [TestCase("""{ "version": 1, "nights": [ { "night": 1, "bestAct": 2, "won": true, "seed": 7 } ] }""", TestName = "Parse_AnUnknownKeyOfANight_IsRefused")]
    [TestCase("""{ "version": 1, "nights": [ { "night": 1, "won": true } ] }""", TestName = "Parse_ANightWithNoBestAct_IsRefused")]
    [TestCase("""{ "version": 1, "nights": [ { "night": 1, "bestAct": 2 } ] }""", TestName = "Parse_ANightWithNoWon_IsRefused")]
    [TestCase("""{ "version": 1, "nights": [ null ] }""", TestName = "Parse_ANightThatIsNull_IsRefused")]
    [TestCase("""{ "version": 1, "nights": [ { "night": 0, "bestAct": 2, "won": false } ] }""", TestName = "Parse_NightNought_IsRefused")]
    [TestCase("""{ "version": 1, "nights": [ { "night": 1, "bestAct": 0, "won": false } ] }""", TestName = "Parse_ABestActOfNought_IsRefused")]
    [TestCase("""{ "version": 1, "nights": [ { "night": 1, "bestAct": -3, "won": false } ] }""", TestName = "Parse_ABestActBelowNought_IsRefused")]
    [TestCase("""{ "version": 1, "nights": [ { "night": 1, "bestAct": 99999999999, "won": false } ] }""", TestName = "Parse_ANumberTooLarge_IsRefused")]
    [TestCase("""{ "version": 1, "nights": [ { "night": 1.5, "bestAct": 2, "won": false } ] }""", TestName = "Parse_ANightThatIsNotWhole_IsRefused")]
    [TestCase("""{ "version": 1, "nights": [ { "night": 2, "bestAct": 2, "won": false }, { "night": 2, "bestAct": 5, "won": true } ] }""", TestName = "Parse_ANightTwice_IsRefused")]
    [TestCase("""{ "version": 1, "nights": [ { "night": 2, "bestAct": 2, "won": false }, { "night": 1, "bestAct": 5, "won": true } ] }""", TestName = "Parse_NightsOutOfOrder_AreRefused")]
    public void Parse_Refuses(string json) =>
        Assert.That(() => Progress.Parse(json), Throws.InstanceOf<JsonException>());

    [Test]
    public void Unlocked_WithNoProgress_IsTheFirstNightAlone() =>
        Assert.That(Progress.None.Unlocked(CommittedNights.Parse()), Is.EqualTo(new[] { 1 }));

    [Test]
    public void Unlocked_WithNoNights_IsNothing() =>
        Assert.That(Progress.None.With(1, 5, won: true).Unlocked([]), Is.Empty);

    [Test]
    public void Unlocked_TheCommittedNights_OpenOneAfterAnotherByBeingPlayed()
    {
        IReadOnlyList<Night> nights = CommittedNights.Parse();

        // A lost night counts as played, in whatever act it was lost: night 2 follows night 1.
        Progress lostNightOne = Progress.None.With(1, 1, won: false);
        Assert.That(lostNightOne.Unlocked(nights), Is.EqualTo(new[] { 1, 2 }));

        // Only the nights in the file are offered: after night 2 the next is night 10, and night 2's gate is its
        // own number's, which is "played".
        Assert.That(lostNightOne.With(2, 1, won: false).Unlocked(nights), Is.EqualTo(new[] { 1, 2, 10 }));
    }

    [Test]
    public void GateOf_ANight_IsWhatItAsksBeforeTheNextOpens()
    {
        // What the poster says in words (plan T55) and what Unlocked opens by are the one rule.
        Assert.That(Progress.GateOf(1), Is.EqualTo(Gate.Played));
        Assert.That(Progress.GateOf(5), Is.EqualTo(Gate.Played));
        Assert.That(Progress.GateOf(6), Is.EqualTo(Gate.ActReachedOrWon));
        Assert.That(Progress.GateOf(15), Is.EqualTo(Gate.ActReachedOrWon));
        Assert.That(Progress.GateOf(16), Is.EqualTo(Gate.Won));
    }

    [TestCase(5, 1, false, true, TestName = "night 5 lost in act one: played")]
    [TestCase(6, 4, false, false, TestName = "night 6 lost in act four")]
    [TestCase(6, 5, false, true, TestName = "night 6 lost in act five")]
    [TestCase(16, 9, false, false, TestName = "night 16 lost in act nine")]
    [TestCase(16, 9, true, true, TestName = "night 16 won")]
    public void Unlocked_OpensTheNextNight_ByTheGateThatGateOfNames(int night, int act, bool won, bool opens) =>
        Assert.That(
            Progress.None.With(night, act, won).Unlocked(Nights(night, night + 1)),
            Is.EqualTo(opens ? new[] { night, night + 1 } : new[] { night }),
            $"the gate is {Progress.GateOf(night)}");

    [Test]
    public void Unlocked_ThroughNightFive_ByBeingPlayed() =>
        Assert.That(Progress.None.With(5, 1, won: false).Unlocked(Nights(5, 6)), Is.EqualTo(new[] { 5, 6 }));

    [Test]
    public void Unlocked_FromNightSix_ByReachingActFiveOrWinning()
    {
        IReadOnlyList<Night> nights = Nights(6, 7);

        Assert.That(Progress.None.With(6, 4, won: false).Unlocked(nights), Is.EqualTo(new[] { 6 }), "lost in act four");
        Assert.That(Progress.None.With(6, 6, won: false).Unlocked(nights), Is.EqualTo(new[] { 6, 7 }), "lost in act six");

        // A night shorter than five acts, won: a win opens the gate whatever the act.
        Assert.That(Progress.None.With(6, 3, won: true).Unlocked(nights), Is.EqualTo(new[] { 6, 7 }), "won in three acts");
    }

    [Test]
    public void Unlocked_AShowThatClosedInActFiveItself_HasReachedActFive()
    {
        // The document's fifth open question, built as the document answers it: reaching is reaching.
        Assert.That(Progress.ActThatUnlocks, Is.EqualTo(5));
        Assert.That(Progress.None.With(15, 5, won: false).Unlocked(Nights(15, 16)), Is.EqualTo(new[] { 15, 16 }));
    }

    [Test]
    public void Unlocked_FromNightSixteen_ByWinningAlone()
    {
        IReadOnlyList<Night> nights = Nights(16, 17);

        Assert.That(Progress.None.With(16, 10, won: false).Unlocked(nights), Is.EqualTo(new[] { 16 }), "lost in act ten");
        Assert.That(Progress.None.With(16, 10, won: true).Unlocked(nights), Is.EqualTo(new[] { 16, 17 }), "won");
    }

    [Test]
    public void Unlocked_ANightIsOpenedByTheNightBeforeItInTheFileAlone()
    {
        // Night 1 won opens night 6 and nothing after it: night 7 waits for night 6.
        Assert.That(Progress.None.With(1, 5, won: true).Unlocked(Nights(1, 6, 7)), Is.EqualTo(new[] { 1, 6 }));
    }

    [Test]
    public void Unlocked_ANightThatWasPlayed_StaysOpenWhenANightIsWrittenInBeforeIt()
    {
        // Nights 1, 2 and 10 were played, and then the file got a night 3: it is open, by night 2, and night 10 is
        // not shut again behind it.
        Progress progress = Progress.None.With(1, 5, won: true).With(2, 2, won: false).With(10, 1, won: false);

        Assert.That(progress.Unlocked(Nights(1, 2, 3, 4, 10)), Is.EqualTo(new[] { 1, 2, 3, 10 }));
    }

    [Test]
    public void Save_AndLoad_GiveTheSameProgress_AndLeaveNoOtherFile()
    {
        Progress progress = Progress.None.With(1, 5, won: true).With(2, 3, won: false);

        progress.Save(ThePath);

        Assert.That(Progress.Load(ThePath, out _).Nights, Is.EqualTo(progress.Nights));
        Assert.That(Directory.GetFiles(_folder), Is.EqualTo(new[] { ThePath }));
    }

    [Test]
    public void Save_MakesTheFolder_AndWritesOverTheFileThatIsThere()
    {
        string file = Path.Combine(_folder, "a folder", "progress.json");

        Progress.None.With(1, 2, won: false).Save(file);
        Progress.None.With(1, 5, won: true).Save(file);

        Assert.That(Progress.Load(file, out _).Nights, Is.EqualTo(new[] { new NightPlayed(1, 5, true) }));
    }

    [Test]
    public void Load_WithNoFile_OrNoFolder_IsNoProgress_AndTheFileMayBeMade()
    {
        Assert.That(Progress.Load(ThePath, out string? noFile).Nights, Is.Empty);
        Assert.That(Progress.Load(Path.Combine(_folder, "nowhere", "progress.json"), out string? noFolder).Nights, Is.Empty);
        Assert.That(new[] { noFile, noFolder }, Is.All.Null);
        Assert.That(Directory.GetFileSystemEntries(_folder), Is.Empty);
    }

    [TestCase("", TestName = "Load_AnEmptyFile_IsNoProgressAndIsKeptAside")]
    [TestCase("""{ "version": 1, "nights": [ { "night": 1, "bestAct": 5, "wo""", TestName = "Load_ADamagedFile_IsNoProgressAndIsKeptAside")]
    [TestCase("""{ "version": 0, "nights": [], "stars": 4 }""", TestName = "Load_AFileOfAnOlderVersion_IsNoProgressAndIsKeptAside")]
    [TestCase("""{ "version": 1, "nights": [ { "night": 1, "bestAct": 0, "won": false } ] }""", TestName = "Load_AFileWithANumberOutOfRange_IsNoProgressAndIsKeptAside")]
    [TestCase("""{ "version": "2" }""", TestName = "Load_AVersionThatIsAText_IsNoProgressAndIsKeptAside")]
    [TestCase("""{ "version": null, "nights": [] }""", TestName = "Load_AVersionThatIsNull_IsNoProgressAndIsKeptAside")]
    [TestCase("""{ "version": true }""", TestName = "Load_AVersionThatIsTrue_IsNoProgressAndIsKeptAside")]
    [TestCase("""{ "version": [2] }""", TestName = "Load_AVersionThatIsAList_IsNoProgressAndIsKeptAside")]
    [TestCase("""{ "version": 2.5, "nights": [] }""", TestName = "Load_AVersionThatIsNotWhole_IsNoProgressAndIsKeptAside")]
    public void Load_WhatIsNotTheProgress(string text)
    {
        File.WriteAllText(ThePath, text);

        Progress read = Progress.Load(ThePath, out string? notKept);

        // The game goes on with no progress, and what was in the file is not lost to the next save.
        Assert.That(read.Nights, Is.Empty);
        Assert.That(notKept, Is.Null, "the file is this game's to write");
        read.With(1, 1, won: false).Save(ThePath);
        Assert.That(File.ReadAllText(ThePath + Progress.DamagedSuffix), Is.EqualTo(text));
        Assert.That(Progress.Load(ThePath, out _).Nights, Is.EqualTo(new[] { new NightPlayed(1, 1, false) }));
    }

    [Test]
    public void Load_ASecondDamagedFile_IsTheOneKeptAside()
    {
        File.WriteAllText(ThePath, "the first");
        Progress.Load(ThePath, out _);
        File.WriteAllText(ThePath, "the second");

        Progress.Load(ThePath, out _);

        Assert.That(File.ReadAllText(ThePath + Progress.DamagedSuffix), Is.EqualTo("the second"));
        Assert.That(File.Exists(ThePath), Is.False);
    }

    [Test]
    public void Load_AFileOfANewerVersion_IsNoProgress_IsLeftAsItIs_AndIsNotToBeWritten()
    {
        const string newer = """{ "version": 2, "nights": [ { "night": 1, "bestAct": 5, "won": true, "stars": 3 } ] }""";
        File.WriteAllText(ThePath, newer);

        Progress read = Progress.Load(ThePath, out string? notKept);

        Assert.That(read.Nights, Is.Empty);
        Assert.That(notKept, Is.Not.Null);
        Assert.That(File.ReadAllText(ThePath), Is.EqualTo(newer));
        Assert.That(Directory.GetFiles(_folder), Is.EqualTo(new[] { ThePath }));
    }

    [Test]
    public void Load_AFileThatCannotBeRead_IsNoProgress_IsLeftAsItIs_AndIsNotToBeWritten()
    {
        // A file nobody may read is something only Unix lets a test make.
        if (OperatingSystem.IsWindows() || Environment.UserName == "root")
        {
            Assert.Ignore("Needs a file's Unix mode, and a user the mode holds for.");
            return;
        }

        string valid = Progress.None.With(1, 5, won: true).With(2, 3, won: false).ToJson();
        File.WriteAllText(ThePath, valid);
        File.SetUnixFileMode(ThePath, UnixFileMode.None);

        Progress read = Progress.Load(ThePath, out string? notKept);

        File.SetUnixFileMode(ThePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        Assert.That(read.Nights, Is.Empty);
        Assert.That(notKept, Is.Not.Null, "a save here would write one night over two");
        Assert.That(File.ReadAllText(ThePath), Is.EqualTo(valid));
    }

    [Test]
    public void Load_ADamagedFileThatCannotBePutAside_IsLeftAsItIs_AndIsNotToBeWritten()
    {
        // In a folder nothing may be made in, the file can be read and not moved.
        if (OperatingSystem.IsWindows() || Environment.UserName == "root")
        {
            Assert.Ignore("Needs a file's Unix mode, and a user the mode holds for.");
            return;
        }

        File.WriteAllText(ThePath, "damaged");
        File.SetUnixFileMode(_folder, UnixFileMode.UserRead | UnixFileMode.UserExecute);

        Progress read = Progress.Load(ThePath, out string? notKept);

        File.SetUnixFileMode(_folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        Assert.That(read.Nights, Is.Empty);
        Assert.That(notKept, Is.Not.Null);
        Assert.That(File.ReadAllText(ThePath), Is.EqualTo("damaged"));
    }

    [Test]
    public void Save_GivesAnotherFileTheName_AndDoesNotWriteIntoTheOneThatIsThere()
    {
        // On Unix a file that may not be written can still be replaced by another of its name, and cannot be
        // written into: a save that wrote straight into the file would be refused here.
        if (OperatingSystem.IsWindows() || Environment.UserName == "root")
        {
            Assert.Ignore("Needs a file's Unix mode, and a user the mode holds for.");
            return;
        }

        Progress.None.With(1, 2, won: false).Save(ThePath);
        File.SetUnixFileMode(ThePath, UnixFileMode.UserRead);
        Assert.That(() => File.WriteAllText(ThePath, "straight into it"), Throws.InstanceOf<UnauthorizedAccessException>());

        Progress.None.With(1, 5, won: true).Save(ThePath);

        Assert.That(Progress.Load(ThePath, out _).Nights, Is.EqualTo(new[] { new NightPlayed(1, 5, true) }));
    }
}
