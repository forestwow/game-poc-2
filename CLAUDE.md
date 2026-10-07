# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

The Understudies is a prototype of a short roguelite for PC: a stage magician defends a box office, and every act played leaves behind an understudy that replays the route the player ran. `docs/vision.md` is the design; `docs/plan-prototype.md` is the plan, the decisions that settle the vision's open points, and the ticket list. Read the plan before changing a rule: where the two disagree, the plan is the later decision.

## Commands

All commands run from the repository root. `global.json` names the .NET SDK (10.0.1xx); warnings are errors.

```bash
dotnet build Understudies.sln --configuration Release                     # CI runs this first: `dotnet test` alone never compiles the game
dotnet test Understudies.sln --configuration Release                      # then this
dotnet test tests/Understudies.Core.Tests --filter "FullyQualifiedName~RngTests"   # one class or test
dotnet run --project src/Understudies.Game                                # play; Esc quits
dotnet run --project src/Understudies.Game -- --capture /tmp/frame.png --ticks 1830  # no play: a fixed script for that many ticks (the fight starts near 1650, the Vanish is on tick 1861, 1872 has scraps, a trail and a flash at once, the magician has fallen by 2100), save the frame, exit
```

## Architecture

Three projects, with one-way dependencies: **Core** ← **Game** and **Core.Tests**.

- `src/Understudies.Core` is the game's rules, with no reference to MonoGame. `Simulation` owns the state and advances it one tick at a time from the magician's input of that tick; everything else reads the state and the events of the last tick (`Simulation.Events`, whose kinds `TickEventKind` lists; the next `Step` starts the list afresh, so whoever is driven by it reads it after every tick). The accumulator clock that turns frame time into whole ticks lives here too, where it can be tested. The same seed and inputs are meant to give the same result on every machine (a pinned state hash checks it across two, plan T19), so:
  - positions are `System.Numerics.Vector2` in world units, and the only arithmetic is + − × ÷ and the square root: no trigonometry;
  - (0, 0) is the stage's top-left corner and y grows downward. The top of the stage is the back wall: the floor starts at `stageFloorTop`, the magician's whole circle is kept on the floor, and `Tuning.Parse` refuses a stage door above it. Critics are kept on the floor by no rule. A test whose scene stands in the stage's top edge gives its own tuning a floor that starts at 0;
  - randomness comes only from `Randomness/Rng` (SplitMix64), never `System.Random`: the simulation draws from the one it makes of the seed its creator gives it (the game takes a played show's seed from the clock, and a capture's is fixed);
  - the simulation never iterates a `Dictionary` or a `HashSet` and never reads a clock;
  - `Simulation.ComputeStateHash` is an FNV-1a hash of everything that decides what happens next. State added to the simulation is added to it in the same change, a `float` by its bits; what only the view reads (the positions before the last tick, the events) stays out;
  - a time in `tuning.json` is in seconds and a rule counts it in whole ticks;
  - `tuning.json` at the repository root is the only place a rule's number lives (the tick rate is the one constant in code; the juice's numbers decide no rule and are constants in the view). `Tuning` is the record parsed from it, and `Simulation` is given one by whoever creates it. A new number is a new member of `Tuning` and a new key in the file: an unknown key and a missing key are both refused, at any depth.
- `src/Understudies.Game` is the MonoGame (DesktopGL) layer: input, drawing, sound. It holds no rules: it feeds the simulation the player's input each tick and draws what the simulation reports, with positions interpolated between the last two ticks. Everything is drawn as shapes; there is no content pipeline and no art. What makes a blow felt (flashes, scraps, fading bodies, the shake, the hit-stop) is the view's own state, in `Juice.cs`: `UnderstudiesGame.Tick` feeds it the simulation after every tick (whatever else the events drive, the sound for one, is fed there too), the frame's time advances it, and the drawing asks it what to add. The capture mode feeds and advances it the same way, a sixtieth of a second a tick. Its numbers are named constants at the top of that file. A figure is drawn by `DrawFigure` alone, which is asked for a flash, a fallen pose and an opacity: nothing paints over a figure with a shape of its own, so the juice holds when figures become sprites. Inside this project `Game` means the namespace, so MonoGame's base class is written `Microsoft.Xna.Framework.Game`.
- `tests/Understudies.Core.Tests` is NUnit: the rule tests and, later, the scripted players that guard the balance.
- `tuning.json` is copied by the project files beside the tests and beside the built game. The tests read that copy through `CommittedTuning`, so a test never carries its own numbers. The game reads `tuning.json` in the current directory first (the committed file itself, as commands run from the root) and the copy beside the executable otherwise; F5 reads it again while the game runs.

## How the work runs

- One ticket of `docs/plan-prototype.md` is one branch (`tNN-short-name`) and one small pull request into `main`, whose title starts with the ticket id (`T04: ...`). Tickets are taken in order.
- Work in a git worktree; the owner's checkout stays on `main`.
- Write the test first for every rule in Core. The view and the juice have no tests: they are judged by eye at the plan's stops, and by a captured frame in review.
- Before a merge, a separate agent with a clean context reviews the pull request with the `mattpocock-skills:code-review` skill (fixed point `main`; the spec is the ticket in the plan) and the `ponytail:ponytail-review` skill. Fix or answer what it finds, wait for green CI, then merge.
- The plan's stops are where the owner plays and judges. The loop does not wait at a stop: it announces it and goes on.
- Build the smallest thing the ticket asks for: no abstraction, option or layer for a later ticket. Mark a deliberate shortcut that has a known ceiling with a `ponytail:` comment naming the ceiling.
- Everything in the repository is in English. There is no study journal in this project.
