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

WASD, the arrows or a gamepad's left stick walk the magician. The dark band along the top of the stage is the back wall: the floor starts at its foot, and the magician cannot walk onto it. Critics come in by the lit door and walk to the box office to strike it. A critic the magician comes near turns on the magician instead, walks at it and hurts it by touching it; the moment the magician is out of its reach it goes back to the box office. Nothing blocks the magician: it walks through critics. The magician throws cards by itself, at the nearest critic in range, and a critic that has taken enough of them falls. Space, or a gamepad's A, is the Vanish: the magician is at once a few steps further the way it walks (the way it last walked, when it stands), for a moment nothing hurts it, and where it stood it leaves a cloud that stuns the critics it touches: a stunned critic turns pale and neither walks nor hurts anything for a moment. The small bar over the magician's head fills as the Vanish comes back; a press before it is full does nothing. The small bar under the magician's feet is what the magician has left, and the bar above the box office, with the number beside it, what the box office has left: when either runs out the show closes and the stage goes dark, and a magician that has fallen lies flat.

A performance is ten acts of seventy-five seconds (`actsInPerformance` and `actLength` in `tuning.json`); the back wall says which act it is and how much of its time is left. An act ends when its time runs out, whatever is on the stage. The stage then stands until Enter, or a gamepad's Start, goes on to the next act: the magician begins it whole and on its mark, and the critics of the last act are still where they stood. After the last act the performance ends in a standing ovation. R starts a new performance, at any time. Esc quits.

The words are drawn in a system font found at start-up: Arial on macOS and Windows, DejaVu Sans or Liberation Sans on Linux. On a machine with none of them the game runs without its words and says so on the console.

Every number of the rules is in `tuning.json` at the repository root. The game reads the `tuning.json` of the directory it is started from, so run it from the root, and the copy beside the executable when there is none. Edit the file and press F5 (fn+F5 on a Mac's built-in keyboard): the game goes on with the new numbers. What an act starts with (its length, the magician's mark and hit points) counts from the next act, and the box office's hit points from the next performance, so press R as well; a countdown that is already running (the act's time, the next critic, a critic's next blow, the next throw, the Vanish coming back, a cloud's time, a stun) runs out at its old length first. A critic already on the stage keeps the hit points it entered with, and a card already in the air the range it was thrown with. A key the game does not know or a key that is missing is an error that names the key, and so is a stage door above `stageFloorTop`, the foot of the back wall (the third door stands at that foot, so the two numbers change together); the console shows it, and the game keeps the numbers it had (or, at start-up, does not start).

To look at a frame without playing, the game can walk a fixed script for a number of ticks (60 a second), save the frame it ends on and exit. The script keeps out of the way while critics gather at the box office, walks back to them and stands while they turn on the magician, vanishes when thirty-one seconds have gone by, and stands where the Vanish took it, throwing at the crowd, until the act is over. In every later act it stands on its mark, and between two acts it goes on by itself. So 1830 ticks end with the crowd round the magician and its hit points going, 1900 two thirds of a second after the Vanish, 4500 on the last tick of the first act, with the stage standing between two acts, and 45000 in the standing ovation:

    dotnet run --project src/Understudies.Game -- --capture /tmp/frame.png --ticks 1830

The script never closes the show. To see a closed one, run the capture from a directory whose `tuning.json` is a copy with few `boxOfficeHitPoints` (with 60 the box office has fallen by tick 1500) or few `magicianHitPoints` (with 10 the magician has fallen by tick 1800).

## Tests

    dotnet test Understudies.sln --configuration Release
