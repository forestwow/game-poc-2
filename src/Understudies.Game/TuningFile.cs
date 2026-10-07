using System.Text.Json;
using Understudies.Core;

namespace Understudies.Game;

internal static class TuningFile
{
    private const string Name = "tuning.json";

    /// <summary>
    /// Reads tuning.json: the one in the current directory, which is the repository's own when the game is run from
    /// its root, or else the copy beside the executable. The console is told which file was read; a file that is
    /// not there or does not parse is reported there instead and gives null.
    /// </summary>
    public static Tuning? Read()
    {
        string path = Path.GetFullPath(File.Exists(Name) ? Name : Path.Combine(AppContext.BaseDirectory, Name));
        try
        {
            Tuning tuning = Tuning.Parse(File.ReadAllText(path));
            Console.WriteLine($"Tuning read from {path}");
            return tuning;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            Console.Error.WriteLine($"Tuning not read from {path}: {exception.Message}");
            return null;
        }
    }
}
