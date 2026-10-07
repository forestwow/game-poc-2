# The Understudies: prototype plan

- **Status:** agreed with the owner on 2026-10-07 in three interview rounds after `docs/vision.md` v0.3. Rounds 1 and 2 were answered ("agreed", with one change: no study journal). Round 3 (decisions 19–22 below) was put to the owner with a recommendation each; the owner started the build without answering it, so those four stand as recommended and are the owner's to overturn. The same day, with the build under way, the owner asked for basic graphics to be planned too: decision 23 and tickets T07a and T07b.
- **What this is:** the map of the work from an empty repository to a playable proof of concept. It is the ticket list too: one ticket is one small pull request.
- **Where the rules come from:** `docs/vision.md`. Where this plan and the vision disagree, this plan is the later decision, and §3 says which line of the vision it settles.

## 1. Destination

A small playable proof of concept that lets the owner check the idea: one stage in placeholder shapes, ten acts, understudies that replay the routes the owner ran, the applause rule, and two scripted players that measure whether hiding at the box office loses. The owner plays it and writes a verdict: does the idea hold?

The proof of concept ends at stop 3 (§6). The package for five outside testers and the G1 judgement of the vision (§12 there) are a separate step, taken only after a positive verdict (§7).

## 2. How the work runs

- **One ticket, one branch, one small pull request into `main`.** The branch is named `tNN-short-name`; the pull request's title starts with the ticket id (`T04: ...`). A ticket is done when its pull request is merged; nothing else tracks status.
- **Tickets are taken in order.** Each builds on the one before it.
- **Review before every merge.** A separate agent with a clean context reviews the pull request with the `mattpocock-skills:code-review` skill (fixed point `main`; the spec is the ticket in this file) and with `ponytail:ponytail-review`. What it finds is fixed or answered in the pull request, then CI must be green, then the merge.
- **Implementation may run in separate agents.** The lead session then only briefs them, waits, checks their result (tests, the diff, a captured frame) and merges.
- **Tests first in Core.** Every rule in `Understudies.Core` gets its test before its code. The view and the juice have no tests: they are judged by eye at the stops, and by a captured frame in review (T02).
- **The smallest thing that works.** No abstraction, option or layer that no ticket here asks for. A deliberate shortcut with a known ceiling is marked with a `ponytail:` comment that names the ceiling.
- **Language:** everything in the repository is in English. There is no study journal in this project.
- **The owner's checkout stays on `main`.** Work happens in git worktrees, so the latest merged game can be run at any time.
- **Stops (§6)** are where the owner plays and judges. The loop does not wait at a stop: it announces it and goes on with tickets that do not depend on the judgement. What a judgement asks to be changed becomes a ticket when it is given.

## 3. Decisions

| # | Topic | Decision |
|---|---|---|
| 1 | Plan | this file: the build of the prototype and, after the verdict, the G1 protocol; the look test gets its own plan after G1 |
| 2 | Fate of the code | Core is written to stay (tests, a clean boundary); the MonoGame layer in shapes is thrown away after the look test |
| 3 | Who writes | the agent loop of §2; the owner plays at the stops |
| 4 | Machines | the playtest build targets `win-x64` and `osx-arm64`; the keyboard is the main control, a gamepad works from the first playable ticket but testers need not own one |
| 5 | Playtest | live or on a call for at least three of five testers, and the game always writes a log of the performance (after the verdict, §7) |
| 6 | What an understudy replays | **positions per tick and the ticks of its Vanish**, not inputs. Nothing slows or pushes an understudy: it is on its rail. Settles vision §6 against the "list of commands" of vision §11 |
| 7 | Journal | none |
| 8 | Repository | `main` is the default branch; CI runs `dotnet test` |
| 9 | Numbers and target | `float` in Core, `net10.0` everywhere, nullable reference types on, warnings are errors. Two rules keep a seed repeatable: Core, and the scripted players that drive it, use only + − × ÷ and the square root (no trigonometry), and one test pins the state hash of a scripted performance, run on macOS ARM locally and on Linux x64 in CI |
| 10 | Tests | first, for every Core rule; none for the view; the bot guard is an ordinary CI test |
| 11 | Tuning | one `tuning.json`, read with `System.Text.Json`, unknown and missing keys are errors, reloaded with a key while the game runs. The fingerprints of vision §11 wait until after G1 |
| 12 | Libraries | MonoGame DesktopGL 3.8.5.1 and `FontStashSharp.MonoGame` for text. No framework (MonoGame.Extended, Nez, Myra) and no content pipeline in the prototype |
| 13 | Stops | four, in the order of §6 |
| 14 | Stage | the street corner: 48 × 27 units, the box office off-centre, three doors opening in acts 1, 3 and 6 (the acts are a hypothesis). No fountain |
| 15 | No applause | **no applause, no card.** Settles vision §7.1 against the "0–2: one common card" of the table in vision §4 |
| 16 | Critics and the magician | a critic walks to the box office, turns on the magician inside a short radius and goes back to the box office when the magician leaves it; a stagehand never turns. The magician is whole again at every curtain |
| 17 | Critics alive when an act ends | they stay on the stage, frozen while the program is read, and the next act adds to them |
| 18 | Not in the prototype | the list of §8 |
| 19 | The owner's verdict | the proof of concept ends at stop 3; the testers' package follows a positive verdict |
| 20 | Cards | self cards: damage, attack speed, range, Vanish cooldown, one more card per throw. Chorus card: damage for every understudy. Any card may be taken again. An understudy is a snapshot of the magician in the act it was recorded: it has every self card the magician had then |
| 21 | The measure of applause | a share: applause collected in the act as a percentage of the critics released in that act, with two thresholds (15 % and 35 % to start with) and a bar with two notches on the screen. More than nothing but under the first threshold offers one card, the first threshold a choice of two, the second a choice of three with a chance of the chorus card. Replaces the counts (0–2, 3–5, 6 or more) of the table in vision §4 |
| 22 | The bot guard | 20 seeds. The orbit player loses the box office by the end of act six in at least 16 of them; the doors player finishes act ten in at least 16. This reads the "must lose" and "must reach act ten" of vision §12 (e) as 16 of 20, the measure of faith-defense's map standard. Both pick cards in a fixed order, so the result depends on the route |
| 23 | Basic graphics | placeholder sprites built in code from small pixel grids, and a dressed stage, before the juice: stop 1 is judged on figures, not on rectangles. No art tool, no image file and no content pipeline. This is not the style decision: the look test of vision §10 still makes that after G1, nothing is generated before it (the vision's own decision 23), and these sprites are thrown away with the rest of the view (decision 2) |

Taken without a question, each a setting or a hypothesis:

- **Tick rate:** 60 per second. It is the one number that lives in code and not in `tuning.json`, because recordings are counted in ticks; 30 is tried at stop 1.
- **Sound:** synthesised in code at start-up. No sound files.
- **Text:** a system font found at start-up. `ponytail:` a font file is shipped when a build leaves the owner's machine (§7).
- **Collisions:** critics push each other apart softly and pass through understudies. Nothing blocks the magician; a critic touching the magician hurts on a cooldown.
- **The mark:** every act starts with the magician on the same mark beside the box office, so every recording begins there.
- **The program:** the world stands, twelve seconds to choose, and when they run out the leftmost card is taken.
- **Solution:** a classic `.sln`, NUnit for tests, as in faith-defense.

## 4. Shape

Three projects, with one-way dependencies: `Understudies.Core` ← `Understudies.Game` and `Understudies.Core.Tests`.

- **`src/Understudies.Core`** has no reference to MonoGame. `Simulation` owns the state and advances it one tick at a time from the magician's input of that tick (a move direction and whether the Vanish was pressed); a card pick is its own call between acts. Everything else reads the state and the list of events of the last tick (a hit, a kill, applause dropped, the box office struck, the curtain), which is what the view, the sound and later the log are driven by.
  - Positions are `System.Numerics.Vector2` in world units; everything that stands on the stage is a circle on the floor.
  - Randomness only through the seeded SplitMix64 streams ported from `faith-defense` (`src/FaithDefense.Core/Randomness/`); never `System.Random`. The simulation never iterates a `Dictionary` or a `HashSet`, and never reads a clock.
  - `Tuning` is one immutable record parsed from `tuning.json`. The committed file is the only place a tunable number lives; the tests read the same file.
  - The accumulator clock that turns frame time into whole ticks (the `SimulationClock` of faith-defense) lives here too: it needs nothing from the engine, and this is where it can be tested.
- **`src/Understudies.Game`** is the MonoGame layer: positions interpolated between the last two ticks, the stage drawn in world units scaled to the window, everything as shapes. A figure is drawn upright from its feet and sorted by its y. From T07a on a figure is a sprite built in code from a pixel grid, and whatever a later ticket adds to the stage gets its sprite the same way; bars and other marks of the screen stay shapes.
- **`tests/Understudies.Core.Tests`** holds the rule tests and the scripted players.

## 5. Tickets

Every ticket's acceptance includes: `dotnet test Understudies.sln --configuration Release` is green, and the game still starts.

### Stop 1: the toy

**T01 Skeleton.**
The solution with the three projects, `Directory.Build.props`, `global.json`, `.gitignore`, `README.md`, `CLAUDE.md` (the rules of §2 and the shape of §4) and CI.
- `dotnet test` runs one real Core test and CI runs it on every pull request.
- `dotnet run --project src/Understudies.Game` opens a window that closes on Esc.

**T02 The magician walks the stage.**
The fixed tick, the magician moving at a constant speed in any direction and kept inside the 48 × 27 stage, drawn with the box office as shapes and interpolated between ticks. WASD or the arrows, and a gamepad's left stick.
- Tests: speed is the same on a diagonal; the stage's edge stops the magician; the clock turns frame time into whole ticks and caps a long frame.
- `--capture <file.png> --ticks <n>` runs a scripted input for n ticks, saves the frame and exits, so a reviewer can look at a frame without playing.

**T03 The tuning file.**
`tuning.json` beside the game with every number so far; F5 reloads it while the game runs.
- Tests: an unknown key and a missing key are both refused with the key's name; the committed file parses.

**T04 Critics come for the box office.**
Critics enter at the first stage door at a steady rate (the waves come in T14), walk to the box office and strike it. When its hit points run out the show closes; R starts again.
- Tests: a critic reaches the box office and strikes on its cooldown; critics do not stack on one point; the show closes at zero.

**T05 Thrown cards.**
The magician throws a card at the nearest critic in range on a cooldown; the card flies, hits and hurts; a critic at zero falls. The tick reports its hits and kills as events. The simulation can hash its state.
- Tests: the nearest in range is the target; nothing is thrown at nothing; a card that reaches its critic hurts it once; a kill removes the critic and is reported; the same seed and the same inputs played twice end in the same state hash.

**T06 The Vanish.**
Space, or the gamepad's A: a short dash in the direction of travel, a moment in which nothing hurts the magician, and a cloud left behind that stuns the critics it touches. It has a cooldown, shown on the screen as a bar.
- Tests: the dash's length; the cooldown refuses a second press; a stunned critic neither walks nor strikes until the stun ends.

**T07 Critics turn on the magician.**
Decision 16: the short radius, the return to the box office, a touch that hurts on a cooldown, the magician's hit points. At zero the magician falls and the show closes (the fall inside an act becomes T13).
- Tests: a critic inside the radius walks at the magician and outside it goes back; a touch hurts once per cooldown and never during the Vanish.

**T07a Sprites.**
The figures stop being rectangles (decision 23). A sprite is a small grid of characters with a palette, written in the Game project and made into a texture at start-up: the magician (a top hat and a cape), the critic (a notepad and a pen), the box office (a booth with a striped awning), the thrown card, the cloud of the Vanish. Ten sprite pixels to a world unit and no smoothing, so the sizes of vision §10 hold: the magician is 30 pixels tall, a critic 20, the box office 40 wide. A figure faces the way it goes, bobs as it walks and has a shadow on the floor under its feet. What a tint said before, a tint still says: a stunned critic, the magician in the moment nothing hurts.
- A captured frame shows it: the magician is told from a critic at a glance, one critic from the next in a crowd, and a sprite pixel is the same size everywhere on the screen at the window's default size.
- No test. No image file, no art tool, no new dependency.

**T07b The stage set.**
The stage stops being a brown rectangle: floorboards, a back wall with a curtain along the top, footlights along the bottom edge, and the three stage doors drawn as doors, the open one lit. The back wall is a rule too: the floor that can be walked starts below it (a new number in `tuning.json`), the back alley's door is in that wall, and so no figure is drawn above the floor's edge any more.
- Tests: the magician cannot walk onto the back wall; a critic from the back door enters on the floor.
- A captured frame shows the set, with the magician standing as far up the stage as it can go and whole on the screen.

**T08 Juice.**
Driven by the tick's events: a flash on a hit, paper scraps and a fading body on a kill, a trail behind a thrown card, a shake of the screen when the box office is struck, a hit-stop on the Vanish.
- A captured frame taken in the middle of a fight shows scraps, a trail and a flash.
- No test; the owner judges it at stop 1.

**T09 Sound.**
Five synthesised sounds on the same events: a throw, a hit, a kill, the Vanish, a strike on the box office. A key mutes them.

### Stop 2: the loop

**T10 Acts and the performance.**
An act ends on its timer, always (75 seconds); a performance is ten acts and ends in a standing ovation after the tenth or when the box office falls. Between acts the world stands until the player goes on (the program comes in T17 and T18). Critics alive at the end of an act stay (decision 17). Every act starts with the magician whole and on the mark. The act, its timer and the box office's hit points are on the screen: this is where text first appears (decision 12).
- Tests: the timer ends the act with critics still alive, and they are there in the next one; ten acts end the performance; the box office falling ends it at once.

**T11 Understudies.**
Every act is recorded: the magician's position per tick, the facing, the ticks of the Vanish and the magician's numbers in that act. From the next act on, an understudy replays it: the same route at the same speed, the Vanish at the same ticks with its cloud, thrown cards at whatever critic is in range now. Nothing hurts, pushes or blocks an understudy. Each is tinted by the act it came from, half transparent, with a faint line along its route; the magician is the only bright figure.
- Tests: an understudy stands, tick for tick, where the magician stood; it throws at live critics and never at nothing; its Vanish stuns at the recorded tick; act five has four understudies.

**T12 The curtain.**
One second at the start of every act in which the simulation stands. The slide of every understudy back along its own route to the mark is drawn by the view; in the simulation an understudy is simply at the start of its recording. In act two a single caption says what the cardboard figure is.
- Test: while the curtain is up nothing in the simulation moves, strikes or is released.

**T13 The magician's fall.**
When the magician falls inside an act, control ends, the understudies finish the act and the recording stops at the fall: that understudy is gone from the same tick in every later act. The show goes on while the box office stands.
- Tests: the act runs to its timer after the fall; the understudy of that act disappears at the tick of the fall; the next act starts with the magician whole and on the mark.

**T14 Stage doors and waves.**
Three doors that open in acts 1, 3 and 6. Each act has a budget and each enemy a cost; the act's critics are bought from its allowed set with weights and spread over the act and its open doors, from the performance's seed (the generator of faith-defense, `src/FaithDefense.Core/Waves/WaveGenerator.cs`, and its named random streams).
- Tests: an act's purchases never exceed its budget and leave less than the cheapest enemy; nothing enters by a closed door; the same seed gives the same performance and another seed a different one.

**T15 The stagehand.**
A second enemy from act two: fast, frail, and it never turns on the magician (decision 16).
- Tests: no stagehand in act one; a stagehand inside the radius keeps walking to the box office.

### Stop 3: the rule

**T16 Applause.**
Where a critic falls to the magician's own card, applause drops; never where an understudy's card did it. Only the magician picks it up, by walking over it, and it fades after four seconds. The act's applause is the share of decision 21, shown as a bar with two notches.
- Tests: an understudy's kill drops nothing; an understudy walking over applause leaves it; it is gone after its time; the share counts against the critics released in this act.

**T17 The cards.**
In Core: after each act the program offers cards by the act's applause (decisions 15 and 21): none, one, a choice of two, or a choice of three with a chance of the chorus card. The six cards of decision 20 and what each changes. A pick is one call; when the program's twelve seconds run out the leftmost card is taken. Until T18 the game shows the offer as plain text and takes a pick from the keys 1 to 3.
- Tests: each band of applause offers its number of cards; a self card changes the magician and the next recording and leaves older understudies as they were; the chorus card changes every understudy; the timeout takes the leftmost.

**T18 The program screen.**
The offer drawn as cards with their names and what they change, the countdown, the applause that paid for it. Left and right with a confirm, the keys 1 to 3, or the gamepad.
- No test; a captured frame shows a choice of three.

**T19 Scripted players.**
Two players in the test project that drive the simulation as a player does, tick by tick, and pick cards in a fixed order:
- the **orbit player** circles the box office at a fixed radius and steps to applause only when it lies inside that radius;
- the **doors player** holds the door that opened last, a throw's range inside it, and steps to applause within a short reach before going back.

The radius and the reach are constants of the players. A test run by name (`[Explicit]`) plays both over 20 seeds and prints how each performance ended.
- Tests: one pinned state hash of a scripted performance holds on macOS ARM and on Linux x64 (decision 9). If the two machines disagree, that is the finding: the test is kept per machine and the owner is told. A later change to `tuning.json` or to a rule re-pins it, and its pull request says so.

**T20 Tuned to the guard.**
`tuning.json` changed until decision 22 holds, with the guard as an ordinary test. The pull request names every value changed, old and new, and the table of T19's test before and after.
- If no tuning makes it hold, that is the finding and not a reason to go on: the pull request lands the best tuning with its table, the guard stays a test run by name, and at stop 3 the owner decides whether the fallback of vision §13 goes in.

## 6. Stops

| Stop | After | The question the owner answers by playing |
|---|---|---|
| 1. The toy | T09 | Is running and hitting pleasant by itself? |
| 2. The loop | T15 | Does an understudy read as "past me"? |
| 3. The rule | T20 | Is it worth leaving the box office? And the verdict: does the idea hold? |
| 4. The package | §7 | G1 of the vision, with five testers |

## 7. Not yet specified

In scope, but not sharp enough for a ticket yet. The first four wait for the verdict of stop 3:

- The log of a performance (a line per act: time near the box office, applause, cards, whether a second performance was started).
- The start screen and the way back to it.
- The two zips, checked on a real Windows machine and a real Mac, with a font file shipped in them.
- The G1 sheet: what is watched, what is asked, the template of the written judgement.
- What the owner's judgements at the stops ask to be changed: each becomes a ticket when it is given.

## 8. Out of scope

Not in the prototype (decision 18): the brute, the heckler and the Reviewer; kits other than thrown cards; evolutions and statuses; stage hazards; music and the pit orchestra; unlocks, modifiers, the score and the performance code; a settings menu; re-recording an understudy; art made with an art tool or kept as image files, and the look test (the placeholder sprites of decision 23 are neither).
