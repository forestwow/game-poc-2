string? capturePath = null;
int captureTicks = 0;
bool captureTheMenu = false;
int? captureThePoster = null;

// The way round the gates (plan T54): "--night n" at the end plays that night, or captures it, whatever the
// player's progress says, and no progress is read or written.
int? night = null;
if (args is [.. var before, "--night", var number] && int.TryParse(number, out int asked))
{
    night = asked;
    args = before;
}

if (args is ["--capture", var path, "--ticks", var ticks] && int.TryParse(ticks, out captureTicks) && captureTicks >= 0)
{
    capturePath = path;
}
else if (args is ["--capture", var menuPath, "--menu"])
{
    // The main menu's frame: no show is played for it.
    capturePath = menuPath;
    captureTheMenu = true;
}
else if (args is ["--capture", var posterPath, "--poster", var page] && int.TryParse(page, out int ofNight))
{
    // The poster's page of that night, as a player with no progress has it: night 1 open and every other shut.
    // With "--night n" after it, the open page of that same night.
    capturePath = posterPath;
    captureThePoster = ofNight;
}
else if (args.Length > 0)
{
    // Anything else is refused: a mistyped --capture must not leave the game open on the screen.
    Console.Error.WriteLine("Usage: Understudies.Game [--capture <file.png> (--ticks <n> | --menu | --poster <n>)] [--night <n>]");
    return 2;
}

// Without its numbers the game does not open a window: the reason is already on the console.
if (Understudies.Game.TuningFile.Read() is not var (tuning, nights))
{
    return 1;
}

if (night is { } wanted && nights.All(written => written.Number != wanted))
{
    Console.Error.WriteLine($"No night {wanted}: nights.json has {string.Join(", ", nights.Select(written => written.Number))}");
    return 1;
}

if (captureThePoster is { } poster && (nights.All(written => written.Number != poster) || (night ?? poster) != poster))
{
    Console.Error.WriteLine($"No poster of night {poster}: nights.json has {string.Join(", ", nights.Select(written => written.Number))}, and under --night the one page is that night's");
    return 1;
}

// A capture is the same on every machine, and a night that was asked for is nobody's progress: neither has the file.
string? progress = capturePath is null && night is null ? Understudies.Game.TuningFile.ProgressPath() : null;

// Found as tuning.json is: the repository's own when the game is run from its root, or else beside the executable.
// A folder counts when it has every one of the files asked of it, so another fonts folder in the current directory
// does not stand in the way of the game's own. Without its pictures or its faces the game does not open a window.
static string? Found(string folder, string what, params string[] files)
{
    string beside = Path.Combine(AppContext.BaseDirectory, folder);
    foreach (string found in new[] { folder, beside })
    {
        if (Directory.Exists(found) && files.All(file => File.Exists(Path.Combine(found, file))))
        {
            return found;
        }
    }

    // The one line names the folder beside the executable, or the first file it lacks.
    string missing = files.FirstOrDefault(file => !File.Exists(Path.Combine(beside, file)), "");
    Console.Error.WriteLine($"No {what}: {Path.GetFullPath(Path.Combine(beside, missing))} is missing");
    return null;
}

if (Found(Path.Combine("art", "ludo", "sprites"), "sprites") is not { } sprites
    || Found("fonts", "fonts", Understudies.Game.UnderstudiesGame.FaceFiles) is not { } fonts
    || Found(Path.Combine("art", "ludo", "cards"), "cards' pictures", Understudies.Game.UnderstudiesGame.CardFiles) is not { } cards)
{
    return 1;
}

using var game = new Understudies.Game.UnderstudiesGame(tuning, nights, night, progress, capturePath, captureTicks, captureTheMenu, captureThePoster, sprites, fonts, cards);
game.Run();
return 0;
