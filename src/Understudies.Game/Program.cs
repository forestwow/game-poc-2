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

using var game = new Understudies.Game.UnderstudiesGame(capturePath, captureTicks);
game.Run();
return 0;
