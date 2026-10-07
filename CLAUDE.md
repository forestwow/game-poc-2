# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

The Understudies is a prototype of a short roguelite for PC: a stage magician defends a box office, and every act played leaves behind an understudy that replays the route the player ran. `docs/vision.md` is the design; `docs/plan-prototype.md` is the plan, the decisions that settle the vision's open points, and the ticket list. Read the plan before changing a rule: where the two disagree, the plan is the later decision.

## Commands

All commands run from the repository root. The .NET SDK is pinned in `global.json`; warnings are errors.

```bash
dotnet test Understudies.sln --configuration Release                      # what CI runs
dotnet test tests/Understudies.Core.Tests --filter "FullyQualifiedName~RngTests"   # one class or test
dotnet run --project src/Understudies.Game                                # play; Esc quits
```

## Architecture

Three projects, with one-way dependencies: **Core** ← **Game** and **Core.Tests**.

- `src/Understudies.Core` is the game's rules, with no reference to MonoGame. It must give the same result for the same seed and inputs on every machine, so:
  - positions are `System.Numerics.Vector2` in world units, and the only arithmetic is + − × ÷ and the square root: no trigonometry, no `Math.Pow`;
  - randomness comes only from `Randomness/Rng` (SplitMix64), never `System.Random`;
  - the simulation never iterates a `Dictionary` or a `HashSet` and never reads a clock.
- `src/Understudies.Game` is the MonoGame (DesktopGL) layer: input, drawing, sound. It holds no rules: it feeds the simulation the player's input each tick and draws what the simulation reports. Everything is drawn as shapes; there is no content pipeline and no art. Inside this project `Game` means the namespace, so MonoGame's base class is written `Microsoft.Xna.Framework.Game`.
- `tests/Understudies.Core.Tests` is NUnit: the rule tests and, later, the scripted players that guard the balance.

## How the work runs

- One ticket of `docs/plan-prototype.md` is one branch (`tNN-short-name`) and one small pull request into `main`, whose title starts with the ticket id (`T04: ...`). Tickets are taken in order.
- Work in a git worktree; the owner's checkout stays on `main`.
- Write the test first for every rule in Core. The Game project has no tests: it is judged by eye.
- Before a merge, a separate agent with a clean context reviews the pull request with the `mattpocock-skills:code-review` and `ponytail:ponytail-review` skills. Fix or answer what it finds, wait for green CI, then merge.
- Build the smallest thing the ticket asks for: no abstraction, option or layer for a later ticket. Mark a deliberate shortcut that has a known ceiling with a `ponytail:` comment naming the ceiling.
- Everything in the repository is in English. There is no study journal in this project.
- End commit messages with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
