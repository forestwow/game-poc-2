using System.Text.Json;
using Understudies.Core;

namespace Understudies.Game;

/// <summary>The game's two files of numbers, tuning.json and nights.json, and where the player's progress is kept.</summary>
internal static class TuningFile
{
    /// <summary>
    /// Reads tuning.json and nights.json, each the one in the current directory, which is the repository's own
    /// when the game is run from its root, or else the copy beside the executable. The console is told which
    /// files were read. A file that is not there or does not parse, nights that are none, and a night that does
    /// not compose with the tuning (plan T51: a kind it allows that the tuning does not have) are reported there
    /// instead and give null: whoever asks has both or neither, and every night it is given can be played.
    /// </summary>
    public static (Tuning Plain, IReadOnlyList<Night> Nights)? Read()
    {
        if (Read("tuning.json", "Tuning", Tuning.Parse) is not { } plain
            || Read("nights.json", "Nights", json => Compose(plain, Night.Parse(json))) is not { } nights)
        {
            return null;
        }

        return (plain, nights);
    }

    /// <summary>
    /// The file of the player's progress (plan T54): a folder named for the game in the user's own folder for an
    /// application's data, which is ~/Library/Application Support on macOS, %LOCALAPPDATA% on Windows and
    /// $XDG_DATA_HOME or ~/.local/share on Linux. Never the repository, the current directory or the
    /// executable's folder. Null where the machine has no such folder: the progress is then not kept.
    /// </summary>
    public static string? ProgressPath() =>
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) is { Length: > 0 } folder
            ? Path.Combine(folder, "The Understudies", "progress.json")
            : null;

    private static IReadOnlyList<Night> Compose(Tuning plain, IReadOnlyList<Night> nights)
    {
        if (nights.Count == 0)
        {
            throw new JsonException("There is no night in it.");
        }

        foreach (Night night in nights)
        {
            Night.Compose(plain, nights, night.Number);
        }

        return nights;
    }

    private static T? Read<T>(string name, string what, Func<string, T> parse)
        where T : class
    {
        string path = Path.GetFullPath(File.Exists(name) ? name : Path.Combine(AppContext.BaseDirectory, name));
        try
        {
            T read = parse(File.ReadAllText(path));
            Console.WriteLine($"{what} read from {path}");
            return read;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            // A missing x or y names itself but not its point: the path and the line say which.
            string where = exception is JsonException { Path: { } key, LineNumber: { } line } ? $" (at {key}, line {line + 1})" : "";
            Console.Error.WriteLine($"{what} not read from {path}: {exception.Message}{where}");
            return null;
        }
    }
}
