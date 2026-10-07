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

WASD, the arrows or a gamepad's left stick walk the magician. Critics come in by the lit door and walk to the box office to strike it. A critic the magician comes near turns on the magician instead, walks at it and hurts it by touching it; the moment the magician is out of its reach it goes back to the box office. Nothing blocks the magician: it walks through critics. The magician throws cards by itself, at the nearest critic in range, and a critic that has taken enough of them falls. Space, or a gamepad's A, is the Vanish: the magician is at once a few steps further the way it walks (the way it last walked, when it stands), for a moment nothing hurts it, and where it stood it leaves a cloud that stuns the critics it touches: a stunned critic turns pale and neither walks nor hurts anything for a moment. The small bar over the magician's head fills as the Vanish comes back; a press before it is full does nothing. The small bar under the magician's feet is what the magician has left, and the bar above the box office what the box office has left: when either runs out the show closes and the stage goes dark, and a magician that has fallen lies flat. R starts the show again, at any time. Esc quits.

Every number of the rules is in `tuning.json` at the repository root. The game reads the `tuning.json` of the directory it is started from, so run it from the root, and the copy beside the executable when there is none. Edit the file and press F5 (fn+F5 on a Mac's built-in keyboard): the game goes on with the new numbers. What a show only starts with (the magician's mark and hit points, the box office's hit points) counts from the next show, so press R as well; a countdown that is already running (the next critic, a critic's next blow, the next throw, the Vanish coming back, a cloud's time, a stun) runs out at its old length first. A critic already on the stage keeps the hit points it entered with, and a card already in the air the range it was thrown with. A key the game does not know or a key that is missing is an error that names the key; the console shows it, and the game keeps the numbers it had (or, at start-up, does not start).

To look at a frame without playing, the game can walk a fixed script for a number of ticks (60 a second), save the frame it ends on and exit. The script keeps out of the way while critics gather at the box office, walks back to them and stands while they turn on the magician, vanishes when thirty-one seconds have gone by, and walks back into them until the magician falls. So 1830 ticks end with the crowd round the magician and its hit points going, 1900 two thirds of a second after the Vanish, and 2100 with the magician fallen:

    dotnet run --project src/Understudies.Game -- --capture /tmp/frame.png --ticks 1830

## Tests

    dotnet test Understudies.sln --configuration Release
