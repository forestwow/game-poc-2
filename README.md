# The Understudies

A prototype of a short roguelite for PC. You are a stage magician defending the box office from critics. Each act lasts about a minute, and every act you play leaves behind a cardboard understudy that repeats the route you ran, in every act that follows. Where you run is what you build.

- `docs/vision.md` is the design.
- `docs/plan-prototype.md` is the plan of the prototype and its ticket list.

## Layout

- `src/Understudies.Core`: the rules of the game, with no engine dependency.
- `src/Understudies.Game`: the MonoGame layer (window, input, drawing, sound).
- `tests/Understudies.Core.Tests`: NUnit tests for Core.

## Requirements

The .NET SDK named in `global.json` (10.0.1xx). Nothing else: MonoGame comes from NuGet.

## Running

    dotnet run --project src/Understudies.Game

WASD, the arrows or a gamepad's left stick walk the magician. Critics come in by the lit door and walk to the box office to strike it. The magician throws cards by itself, at the nearest critic in range, and a critic that has taken enough of them falls. The bar above the box office is what it has left: when that runs out the show closes and the stage goes dark. R starts the show again, at any time. Esc quits.

Every number of the rules is in `tuning.json` at the repository root. The game reads the `tuning.json` of the directory it is started from, so run it from the root, and the copy beside the executable when there is none. Edit the file and press F5 (fn+F5 on a Mac's built-in keyboard): the game goes on with the new numbers. What a show only starts with (the magician's mark, the box office's hit points) counts from the next show, so press R as well. A key the game does not know or a key that is missing is an error that names the key; the console shows it, and the game keeps the numbers it had (or, at start-up, does not start).

To look at a frame without playing, the game can walk a fixed script for a number of ticks (60 a second), save the frame it ends on and exit:

    dotnet run --project src/Understudies.Game -- --capture /tmp/frame.png --ticks 120

## Tests

    dotnet test Understudies.sln --configuration Release
