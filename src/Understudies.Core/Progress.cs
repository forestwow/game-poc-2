using System.Text.Json;
using System.Text.Json.Serialization;

namespace Understudies.Core;

/// <summary>One night the player has played (plan T54), as the progress file holds it.</summary>
/// <param name="Night">Which night, by its number in nights.json.</param>
/// <param name="BestAct">The furthest act a show of that night was in when it ended or was given up, 1 or more.</param>
/// <param name="Won">Whether a show of that night ended in its ovation.</param>
public sealed record NightPlayed(int Night, int BestAct, bool Won);

/// <summary>What a night asks of the player before the night after it opens (<see cref="Progress.GateOf"/>).</summary>
public enum Gate
{
    /// <summary>The night was played, whatever came of it.</summary>
    Played,

    /// <summary>A show of the night reached act <see cref="Progress.ActThatUnlocks"/>, or the night was won.</summary>
    ActReachedOrWon,

    /// <summary>The night was won.</summary>
    Won,
}

/// <summary>
/// What the player has done, between one run of the game and the next (plan T54, decision 33): the nights played,
/// in the order of their numbers, each once. It is no part of a show: <see cref="Simulation"/> never reads it, it
/// is in no state hash, and no rule of a show follows from it. It decides one thing, which nights the menu offers
/// (<see cref="Unlocked"/>). It is here and not in the view because it has rules of its own, and the tests reach
/// this project alone. The file is the game's to place: every method that touches one is given its path.
/// </summary>
public sealed record Progress(IReadOnlyList<NightPlayed> Nights)
{
    /// <summary>
    /// The file's shape. A file that says another number is not read (<see cref="Load"/>): whoever changes the
    /// shape counts one more and says there what an older file becomes.
    /// </summary>
    public const int Version = 1;

    /// <summary>The three gates (the ladder's document): a night before this one opens the next by being played.</summary>
    public const int PlayedUnlocksBefore = 6;

    /// <summary>A night from this one on opens the next by being won, and by nothing else.</summary>
    public const int WinAloneUnlocksFrom = 16;

    /// <summary>
    /// The act a show of a night between the two must have been in to open the next, when it was not won. A show
    /// that closed in act five itself has reached it (the document's fifth open question, as the document answers
    /// it: "reaching is reaching"). With 6 here act five would have to be outlived.
    /// </summary>
    public const int ActThatUnlocks = 5;

    /// <summary>
    /// The gate of a night, by its number: what <see cref="Unlocked"/> opens the next night by, and what the
    /// poster says of it in words (plan T55), so that the two are one rule.
    /// </summary>
    public static Gate GateOf(int night) =>
        night < PlayedUnlocksBefore ? Gate.Played : night < WinAloneUnlocksFrom ? Gate.ActReachedOrWon : Gate.Won;

    /// <summary>What <see cref="Load"/> adds to the name of a file it could not read, to keep it.</summary>
    public const string DamagedSuffix = ".damaged";

    /// <summary>A player who has played nothing.</summary>
    public static readonly Progress None = new([]);

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        AllowDuplicateProperties = false,
        RespectRequiredConstructorParameters = true,
        WriteIndented = true,
    };

    /// <summary>The file as it is written: the shape's number and the nights.</summary>
    private sealed record Saved(int Version, IReadOnlyList<NightPlayed> Nights);

    /// <summary>Reads the text of a progress file, as strictly as the tuning is read.</summary>
    /// <exception cref="JsonException">
    /// The text is not the progress: not JSON, a key unknown, missing or twice, another <see cref="Version"/>, a
    /// night or a best act below 1, a night twice or out of order.
    /// </exception>
    public static Progress Parse(string json)
    {
        Saved saved = JsonSerializer.Deserialize<Saved>(json, Options) ?? throw new JsonException("The progress is null.");
        if (saved.Version != Version)
        {
            throw new JsonException($"'version' is {saved.Version}: this game reads {Version}.");
        }

        int before = 0;
        foreach (NightPlayed? played in saved.Nights ?? throw new JsonException("'nights' is null."))
        {
            if (played is null || played.Night <= before || played.BestAct < 1)
            {
                throw new JsonException(
                    $"The night after night {before} is not a night played: the nights are numbered from 1 and "
                    + "stand in order, each once, with a best act of 1 or more.");
            }

            before = played.Night;
        }

        return new Progress(saved.Nights);
    }

    /// <summary>The text of the progress file.</summary>
    public string ToJson() => JsonSerializer.Serialize(new Saved(Version, Nights), Options);

    /// <summary>
    /// The progress with one more show of a night in it: the night is played, its best act is the furthest of
    /// this show's and the ones before, and a night won once is won.
    /// </summary>
    public Progress With(int night, int act, bool won)
    {
        NightPlayed? before = Nights.FirstOrDefault(played => played.Night == night);
        var now = new NightPlayed(night, Math.Max(act, before?.BestAct ?? 1), won || before is { Won: true });
        return new Progress([.. Nights.Where(played => played.Night != night).Append(now).OrderBy(played => played.Night)]);
    }

    /// <summary>
    /// The numbers of the nights the player may play, of those the file of nights has, in its order. The first is
    /// always open. Another is open when the night before it in that file has passed its gate, which is read by
    /// that earlier night's number: played, whatever came of it, before night <see cref="PlayedUnlocksBefore"/>;
    /// act <see cref="ActThatUnlocks"/> reached or the night won from there; won, from night
    /// <see cref="WinAloneUnlocksFrom"/>. And a night that was played is open, so that a night written into the
    /// file before it does not shut it again.
    /// </summary>
    public IReadOnlyList<int> Unlocked(IReadOnlyList<Night> nights)
    {
        NightPlayed? Played(int night) => Nights.FirstOrDefault(played => played.Night == night);

        bool HasPassedItsGate(int night) =>
            Played(night) is { } played
            && GateOf(night) switch
            {
                Gate.Played => true,
                Gate.ActReachedOrWon => played.Won || played.BestAct >= ActThatUnlocks,
                _ => played.Won,
            };

        var unlocked = new List<int>();
        for (int i = 0; i < nights.Count; i++)
        {
            if (i == 0 || Played(nights[i].Number) is not null || HasPassedItsGate(nights[i - 1].Number))
            {
                unlocked.Add(nights[i].Number);
            }
        }

        return unlocked;
    }

    /// <summary>
    /// The progress in a file, and <see cref="None"/> when there is no file, or none that can be read: it never
    /// stops the game. <paramref name="notKept"/> says whether the file may be written from here, which is what
    /// keeps a player's progress from being written over by a game that did not read it:
    /// <list type="bullet">
    /// <item>no file, or no folder: no progress, and the file is this game's to make (null);</item>
    /// <item>a file that is not the progress (damaged, empty, of an older <see cref="Version"/>): it is moved
    /// aside, to its own name with <see cref="DamagedSuffix"/>, one such file kept, the last, and the file is
    /// this game's to make (null);</item>
    /// <item>a file of a newer version: it is a newer game's and is left as it is (the reason);</item>
    /// <item>a file that is there and could not be read, or one that could not be moved aside: it is left as it
    /// is (the reason).</item>
    /// </list>
    /// Whoever is given a reason does not <see cref="Save"/> to that path in this run.
    /// </summary>
    public static Progress Load(string path, out string? notKept)
    {
        notKept = null;
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return None;
        }
        catch (Exception exception)
        {
            notKept = $"it could not be read ({exception.Message})";
            return None;
        }

        try
        {
            return Parse(json);
        }
        catch (JsonException)
        {
            if (IsOfANewerVersion(json))
            {
                notKept = "it is a newer game's";
                return None;
            }
        }
        catch (Exception exception)
        {
            // Nothing is known to come here. This stands between a file on the player's disk and the game
            // starting, so whatever a file's content throws is a file that was not read: left as it is.
            notKept = $"it could not be read ({exception.Message})";
            return None;
        }

        try
        {
            File.Move(path, path + DamagedSuffix, overwrite: true);
        }
        catch (Exception exception)
        {
            notKept = $"it is not the progress and could not be put aside ({exception.Message})";
        }

        return None;
    }

    /// <summary>Whether a text that is not this game's progress says a <see cref="Version"/> after this one.</summary>
    private static bool IsOfANewerVersion(string json)
    {
        try
        {
            using JsonDocument text = JsonDocument.Parse(json);
            return text.RootElement.ValueKind == JsonValueKind.Object
                && text.RootElement.TryGetProperty("version", out JsonElement version)
                && version.ValueKind == JsonValueKind.Number
                && version.TryGetInt32(out int number)
                && number > Version;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Writes the progress to a file, whole: to another file beside it first, which then takes its name, so that
    /// a game that dies while it writes leaves the old file or the new one and never half of one. The folder is
    /// made when it is not there.
    /// </summary>
    /// <exception cref="IOException">The file could not be written.</exception>
    /// <exception cref="UnauthorizedAccessException">The file may not be written.</exception>
    public void Save(string path)
    {
        if (Path.GetDirectoryName(path) is { Length: > 0 } folder)
        {
            Directory.CreateDirectory(folder);
        }

        // ponytail: nothing is flushed to the disk before the rename, so a power loss at that moment can leave an
        // empty file on some file systems: it is then put aside as damaged and the player starts afresh. And two
        // games running at once share the one name beside the file: the last to write wins. Flush the stream
        // (FileStream.Flush(true)) and give the name a process id when either is ever seen.
        string beside = path + ".tmp";
        File.WriteAllText(beside, ToJson());
        File.Move(beside, path, overwrite: true);
    }
}
