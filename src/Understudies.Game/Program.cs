string? capturePath = null;
int captureTicks = 0;
if (args is ["--capture", var path, "--ticks", var ticks] && int.TryParse(ticks, out captureTicks) && captureTicks >= 0)
{
    capturePath = path;
}
else if (args.Length > 0)
{
    // Anything else is refused: a mistyped --capture must not leave the game open on the screen.
    Console.Error.WriteLine("Usage: Understudies.Game [--capture <file.png> --ticks <n>]");
    return 2;
}

// Without its numbers the game does not open a window: the reason is already on the console.
if (Understudies.Game.TuningFile.Read() is not { } tuning)
{
    return 1;
}

// Found as tuning.json is: the repository's own when the game is run from its root, or else beside the executable.
// Without its pictures or its faces the game does not open a window either.
static string? Found(string folder, string what)
{
    string found = Directory.Exists(folder) ? folder : Path.Combine(AppContext.BaseDirectory, folder);
    if (Directory.Exists(found))
    {
        return found;
    }

    Console.Error.WriteLine($"No {what} in {Path.GetFullPath(found)}");
    return null;
}

if (Found(Path.Combine("art", "ludo", "sprites"), "sprites") is not { } sprites || Found("fonts", "fonts") is not { } fonts)
{
    return 1;
}

using var game = new Understudies.Game.UnderstudiesGame(tuning, capturePath, captureTicks, sprites, fonts);
game.Run();
return 0;
