string? capturePath = null;
int captureTicks = 0;
bool captureTheMenu = false;
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
else if (args.Length > 0)
{
    // Anything else is refused: a mistyped --capture must not leave the game open on the screen.
    Console.Error.WriteLine("Usage: Understudies.Game [--capture <file.png> (--ticks <n> | --menu)]");
    return 2;
}

// Without its numbers the game does not open a window: the reason is already on the console.
if (Understudies.Game.TuningFile.Read() is not { } tuning)
{
    return 1;
}

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

using var game = new Understudies.Game.UnderstudiesGame(tuning, capturePath, captureTicks, captureTheMenu, sprites, fonts, cards);
game.Run();
return 0;
