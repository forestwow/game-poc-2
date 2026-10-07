string? capturePath = null;
int captureTicks = 0;
bool understood = true;
for (int i = 0; i < args.Length; i += 2)
{
    string? value = i + 1 < args.Length ? args[i + 1] : null;
    switch (args[i])
    {
        case "--capture" when value is not null:
            capturePath = value;
            break;
        case "--ticks" when int.TryParse(value, out captureTicks):
            break;
        default:
            understood = false;
            break;
    }
}

// Anything else is refused: a mistyped --capture must not leave the game open on the screen.
if (!understood || (args.Length > 0 && capturePath is null))
{
    Console.Error.WriteLine("Usage: Understudies.Game [--capture <file.png> --ticks <n>]");
    return 2;
}

// Without its numbers the game does not open a window: the reason is already on the console.
if (Understudies.Game.TuningFile.Read() is not { } tuning)
{
    return 1;
}

using var game = new Understudies.Game.UnderstudiesGame(tuning, capturePath, captureTicks);
game.Run();
return 0;
