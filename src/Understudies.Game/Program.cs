string? capturePath = null;
int captureTicks = 0;
string? artFolder = null;
if (args is ["--capture", var path, "--ticks", var ticks, .. var rest]
    && int.TryParse(ticks, out captureTicks)
    && captureTicks >= 0
    && rest is [] or ["--art", _])
{
    capturePath = path;
    if (rest is [_, var tool])
    {
        // Found as tuning.json is: the repository's own when the game is run from its root, or else beside the
        // executable. ponytail: nothing copies art/ there yet; the build does when a build leaves the repository.
        artFolder = Path.Combine(Directory.Exists("art") ? "art" : Path.Combine(AppContext.BaseDirectory, "art"), tool);
        if (!new[] { "magician.png", "critic.png", "box-office.png" }.All(name => File.Exists(Path.Combine(artFolder, name))))
        {
            Console.Error.WriteLine($"No art in {Path.GetFullPath(artFolder)}");
            return 2;
        }
    }
}
else if (args.Length > 0)
{
    // Anything else is refused: a mistyped --capture must not leave the game open on the screen.
    Console.Error.WriteLine("Usage: Understudies.Game [--capture <file.png> --ticks <n> [--art <tool>]]");
    return 2;
}

// Without its numbers the game does not open a window: the reason is already on the console.
if (Understudies.Game.TuningFile.Read() is not { } tuning)
{
    return 1;
}

using var game = new Understudies.Game.UnderstudiesGame(tuning, capturePath, captureTicks, artFolder);
game.Run();
return 0;
