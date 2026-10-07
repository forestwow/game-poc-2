# The Understudies

A prototype of a short roguelite for PC. You are a stage magician defending the box office from critics. Each act lasts about a minute, and every act you play leaves behind a cardboard understudy that repeats the route you ran, in every act that follows. Where you run is what you build.

- `docs/vision.md` is the design.
- `docs/plan-prototype.md` is the plan of the prototype and its ticket list.

## Layout

- `src/Understudies.Core`: the rules of the game, with no engine dependency.
- `src/Understudies.Game`: the MonoGame layer (window, input, drawing, sound).
- `tests/Understudies.Core.Tests`: NUnit tests for Core.

## Requirements

The .NET SDK pinned in `global.json`. Nothing else: MonoGame comes from NuGet.

## Running

    dotnet run --project src/Understudies.Game

Esc quits.

## Tests

    dotnet test Understudies.sln --configuration Release
