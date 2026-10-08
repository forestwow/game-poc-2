# The Understudies: prototype plan

- **Status:** agreed with the owner on 2026-10-07 in three interview rounds after `docs/vision.md` v0.3. Rounds 1 and 2 were answered ("agreed", with one change: no study journal). Round 3 (decisions 19–22 below) was put to the owner with a recommendation each; the owner started the build without answering it, so those four stand as recommended and are the owner's to overturn. The same day, with the build under way, the owner asked for basic graphics to be planned too, with PixelLab allowed and a wish to try a new tool: decision 23 and tickets T07b to T07d. Decision 24 (the back wall) was not asked of the owner either and stands the same way as 19–22.
- **What this is:** the map of the work from an empty repository to a playable proof of concept. It is the ticket list too: one ticket is one small pull request.
- **Where the rules come from:** `docs/vision.md`. Where this plan and the vision disagree, this plan is the later decision, and §3 says which line of the vision it settles.

## 1. Destination

A small playable proof of concept that lets the owner check the idea: one stage, in placeholder shapes at first and with a basic set of generated art once the owner has picked a tool (decision 23), ten acts, understudies that replay the routes the owner ran, the applause rule, and two scripted players that measure whether hiding at the box office loses. The owner plays it and writes a verdict: does the idea hold?

The proof of concept ends at stop 3 (§6). The package for five outside testers and the G1 judgement of the vision (§12 there) are a separate step, taken only after a positive verdict and the owner's word that the game is ready to be given out (§6, §7).

## 2. How the work runs

- **One ticket, one branch, one small pull request into `main`.** The branch is named `tNN-short-name`; the pull request's title starts with the ticket id (`T04: ...`). A ticket is done when its pull request is merged; nothing else tracks status.
- **Tickets are taken in order.** Each builds on the one before it. The basic graphics (T07b to T07d) are the one exception: they wait for the owner and are taken when they are free.
- **Review before every merge.** A separate agent with a clean context reviews the pull request with the `mattpocock-skills:code-review` skill (fixed point `main`; the spec is the ticket in this file) and with `ponytail:ponytail-review`. What it finds is fixed or answered in the pull request, then CI must be green, then the merge.
- **Implementation may run in separate agents.** The lead session then only briefs them, waits, checks their result (tests, the diff, a captured frame) and merges.
- **Tests first in Core.** Every rule in `Understudies.Core` gets its test before its code. The view and the juice have no tests: they are judged by eye at the stops, and by a captured frame in review (T02).
- **The smallest thing that works.** No abstraction, option or layer that no ticket here asks for. A deliberate shortcut with a known ceiling is marked with a `ponytail:` comment that names the ceiling.
- **Language:** everything in the repository is in English. There is no study journal in this project.
- **Art tools.** Their servers are connected to this project by the owner; the loop never copies a key from another project. The loop never buys anything: no plan, no credits, no top-up. Inside a subscription the owner already has, generations need no asking, up to the number a ticket names; when a tool says its allowance has run out, the loop stops using it and tells the owner.
- **The owner's checkout stays on `main`.** Work happens in git worktrees, so the latest merged game can be run at any time.
- **Stops (§6)** are where the owner plays and judges. The loop does not wait at a stop: it announces it and goes on with tickets that do not depend on the judgement. What a judgement asks to be changed becomes a ticket when it is given.

## 3. Decisions

| # | Topic | Decision |
|---|---|---|
| 1 | Plan | this file: the build of the prototype and, after the verdict, the G1 protocol; the look test gets its own plan after G1 |
| 2 | Fate of the code | Core is written to stay (tests, a clean boundary); the MonoGame layer, with its shapes and its basic art, is thrown away after the look test |
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
| 19 | The owner's verdict | the proof of concept ends at stop 3; the testers' package follows a positive verdict and the owner's word that the game is ready to be given out (§6) |
| 20 | Cards | self cards: damage, attack speed, range, Vanish cooldown, one more card per throw. Chorus card: damage for every understudy. Any card may be taken again. An understudy is a snapshot of the magician in the act it was recorded: it has every self card the magician had then |
| 21 | The measure of applause | a share: applause collected in the act as a percentage of the critics released in that act, with two thresholds (15 % and 35 % to start with) and a bar with two notches on the screen. More than nothing but under the first threshold offers one card, the first threshold a choice of two, the second a choice of three with a chance of the chorus card. Replaces the counts (0–2, 3–5, 6 or more) of the table in vision §4 |
| 22 | The bot guard | 20 seeds. The orbit player loses the box office by the end of act six in at least 16 of them; the doors player finishes act ten in at least 16. This reads the "must lose" and "must reach act ten" of vision §12 (e) as 16 of 20, the measure of faith-defense's map standard. Both pick cards in a fixed order, so the result depends on the route |
| 23 | Basic graphics | generated art, pulled forward at the owner's wish: a sprite for every figure and thing that stands on the stage in the prototype, and the stage set. This overturns the "no art is generated before G1" of vision §10 and the split of tools in the vision's decision 25. PixelLab may be used, and the owner wants to try a new tool beside it: ludo.ai is the candidate (the vision gave it one afternoon on what PixelLab does not cover; here it is tried on the figures too). Which tool and which look is the owner's pick on captured frames (T07b). An understudy is never new art: it is the magician's sprite through a treatment, as vision §10 says. The look test with its four candidate styles stays where the vision put it, after G1, and what is made now may be replaced by it |
| 24 | The back wall | the top of the stage is a back wall, and the floor that can be walked starts at its foot (T07a). The screen stays the 48 × 27 of decision 14; the floor is what lies below the wall. The review of T02 left this to the owner as a design question (a figure at the top edge was drawn mostly off the screen) and it was not asked: it stands as recommended and is the owner's to overturn |
| 25 | The look | after the art spike (T07b) the owner liked the pixel-art look best of the five: **ludo.ai, pixel art**. The owner's words for the look: it should mix abstraction and imagination, as the game's idea does. The frame the owner chose has a magician whose head is a dove in a top hat, a critic whose head is an eye under a bowler and a box office whose window is a mouth, all with a thick dark outline: those are the prompt's and not the owner's words. The view stays top-down three-quarter: the owner asked whether it was to be isometric and kept it. The owner asked whether the spike's stills were only pictures, and then agreed that the magician and the critic get a walk cycle from the tool in four directions (T07c) |
| 26 | Upgrades in the act | agreed with the owner on 2026-10-08, who finds that upgrades taken in play draw a player in more: **the encore**. Applause picked up fills a bar; when it is full the act stands for a moment and the magician takes one of three self cards, there and then; every encore taken makes the next cost more. Applause not spent when the act ends is lost, as before. The program between two acts stays and changes its part: one card for the chorus, after an act in which the magician took at least one encore; an act without an encore has no program. (First agreed as a card after every act, always; the owner changed it the same day, 2026-10-08, after a review showed that the free card paid a player that hides: growth without applause, which is the hole the applause rule is there to close.) An understudy takes its act's encores at the ticks the magician took them (T24). This replaces decisions 15 and 21: the thresholds, the bands and "no applause, no card" go. Decision 22's guard stands as it is written and the numbers are tuned to it again |

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
- **`src/Understudies.Game`** is the MonoGame layer: positions interpolated between the last two ticks, the stage drawn in world units scaled to the window, everything as shapes. A figure is drawn upright from its feet and sorted by its y. From T07c on a figure is a sprite read from an image file under `art/`, and whatever a later ticket adds to the stage gets its sprite by the same recipe, except an understudy, which is the magician's sprite through a treatment. Until then, and for bars, the thrown card, the Vanish's cloud and other marks of the screen, shapes.
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

**T07a The back wall.**
The top of the stage is a back wall (decision 24): the floor that can be walked starts at its foot, a new number in `tuning.json`. The back alley's door moves down to that foot, so that the two numbers agree, and a door above the floor's top is refused when the file is read. The view draws the wall as a plain band until the stage set (T07d).
- Tests: the magician cannot walk onto the back wall; a critic entering by a door in the back wall enters on the floor (in a tuning of the test's own, since only the first door is open before T14); a door above the floor's top is refused with its key's name.
- A captured frame shows the magician standing as far up the stage as it can go, whole on the screen.

T07b to T07d are the basic graphics of decision 23, the one exception to "in order" (§2). T07b waits for the owner (a server to connect, then a pick), so the loop does not stand at it: it goes on with T08 and later on shapes and takes these three as soon as each is free. They change only the view.

**T07b The art spike.** *Needs the owner twice: an art tool's server connected to this project before it, the pick after it.*
The same three things from every art tool that is connected: the magician standing, a critic, and the box office; each alone on a transparent background, as a PNG, seen as the game sees them (top-down three-quarter) and in the proportions of vision §10. Each tool in the look it is good at: PixelLab pixel art, a tool that is not a pixel-art tool a flat cut-out (the vision's candidate B). What a tool returns is kept under `art/<tool>/`, with the prompt and the settings of every generation in `art/<tool>/manifest.json`, as faith-defense kept them. At most thirty generations a tool.

The game gets the smallest way to draw a figure from an image file: `Texture2D.FromFile` and the upright draw there already is, no atlas and no animation. `art/` is found as `tuning.json` is, in the current directory first and beside the executable otherwise. Pixel art is drawn at ten sprite pixels to a world unit without smoothing; anything else at thirty or more pixels to a unit, smoothed. The capture mode takes one more argument, `--art <tool>`; without it the game draws shapes as before.
- One captured frame per tool and one in shapes, all of the same ticks; the pull request shows them side by side and says how many generations each tool took.
- If only one tool is connected the spike still runs, and its pull request says plainly that nothing new was tried.
- The owner picks the tool and the look. Nothing more is generated before the pick.
- **The pick (2026-10-08): ludo.ai, pixel art** (decision 25). The first round, paper cut-outs, the owner found default-looking; of the five looks of the second round (their pictures and frames went with T07c, the picked look's too, since its sprites replaced it; `art/ludo/styles/manifest.json` is the record) the owner liked the pixel art best. 29 of the thirty generations were used. The picked pictures are about 64 sprite pixels a figure (some 18 to a unit, so the ten to a unit above does not hold) and the spike draws them smoothed: T07c settles the scale and draws them unsmoothed at a whole scale.

**T07c Sprites.** *After the pick.*
*Changed with the pick (decision 25), agreed with the owner on 2026-10-08:* the magician and the critic get a walk cycle from the tool in four directions, in place of the mirroring and the bob below where a cycle is there; everything else below stands. What goes is the first round's pictures (`art/ludo/*.png`) and the second round's, the picked look's too once its sprites are made, with the spike's frames; the two manifests stay. At most forty pictures and ten animations, and fewer where fewer will do. The recipe in `art/prompt-guide.md` names no artist.
The picked tool's recipe is written down in `art/prompt-guide.md`, so that later figures match, and makes a sprite for everything that stands on the stage when this ticket is built: the magician, the critic, the box office, and whatever later tickets have added by then. The other tool's folder and the `--art` argument go: the game draws sprites. A figure faces the way it goes (mirrored), bobs as it walks and has a shadow on the floor under its feet; there is no walk cycle. What a tint said before, a tint still says (a stunned critic, the magician in the moment nothing hurts), and the flash of T08 still works: a white copy of the sprite, since a tint can only darken.
- A captured frame shows it: the magician is told from a critic at a glance, and one critic from the next in a crowd.
- No test.

**T07d The stage set.** *After the pick.*
*Allowance:* the ticket was written without a number; the owner said on 2026-10-08 that the limits are not the problem and not to overdo it either. It took ten pictures (5 credits).
The stage stops being a brown rectangle: a floor, a curtain along the back wall of T07a, footlights along the bottom edge, and the three stage doors drawn as doors, the open one lit; made with the picked tool or composed from its pieces.
- A captured frame shows the set.
- No test.

**T08 Juice.**
Driven by the tick's events: a flash on a hit, paper scraps and a fading body on a kill, a trail behind a thrown card, a shake of the screen when the box office is struck, a hit-stop on the Vanish. The juice goes through a figure's own draw call and assumes no rectangle, so it holds when figures become sprites (T07c).
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
A second enemy from act two: fast, frail, and it never turns on the magician (decision 16). From this ticket on the throw aims ahead of a moving target: a straight card at T05's speed misses anything faster than a critic (the ceiling is written where the throw is made).
- Tests: no stagehand in act one; a stagehand inside the radius keeps walking to the box office; a card thrown at a stagehand running across the line of fire hits it.

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

### After the verdict

What the owner asked for after stop 3 (§6), a ticket each as it is named.

**T21 Applause within reach.**
The owner's report from play: a piece is picked up only by walking onto it exactly, with the feet. Two causes: the reach was 0.4 past the magician's circle, and the piece was drawn 0.8 above the place it lies, so the eye aimed the body at a place the feet had to find. `applausePickUpReach` goes to 0.8 (1.4 from the middle of the magician's circle) and the diamond is drawn with its tip on its place.
- The guard still holds on the new number (orbit 19 of 20, doors 17 of 20), and the two pinned hashes are pinned again.
- **What it costs, for the owner to weigh.** A critic that touches the magician stands 1.1 from its middle: the old reach of 1.0 left its piece on the floor, and any reach that answers the report takes it without a step. Standing on the box office still earns nothing, but a player that circles near it earns more than before. The review measured 1.4 first (reach 2.0): the orbit on its outer circle went from 0.6 to 1.5 cards a performance and one seed of the middle circle ended in the ovation, so the smaller number was taken. The guard is jumpy here: a reach of 1.0 past the circle gives the orbit 16 of 20, the floor itself.

**T22 The cards' effects.**
The owner misses effects when the cards are thrown. A thrown card spins as it flies, with a dark edge about its face; a flick of light at the hand that throws it; a burst of the card's suits where it strikes; a splash of ink and torn newsprint where its critic falls, beside the scraps and the fading body there already. They change only the view (`Juice.cs` and the drawing), driven by the events `Throw`, `Hit` and `Kill`. The two bursts are sheets from the tool; at most six pictures and four animations.
- A captured frame of a fight shows a burst. No test; the owner judges the rest by playing, since a frame does not show a spin or a flick.

**T23 The encore.**
Decision 26, the rule itself. Tests first.
- A piece of applause picked up counts toward the encore. An encore costs `encoreFirstCost` pieces and `encoreCostGrowth` more for every encore taken so far in the performance.
- On the tick the count reaches the cost, in an act that has time left, the act stands (a phase of its own): three different self cards are offered, drawn from a random stream of their own. `Pick` gives the card to the magician at once, takes the cost from the count (what is over stays toward the next) and the act goes on; `encoreTime` seconds without a pick take the leftmost. Nothing else moves and the act's timer does not run while an encore is read.
- What is in the count when the act ends is lost. A fallen magician picks nothing up, so it earns no encore.
- After an act in which at least one encore was taken, and which has another after it, the program offers the chorus card, alone. An act without an encore has no program: the stage is between two acts at once. An encore of an earlier act does not count for a later one.
- `applauseFirstThreshold`, `applauseSecondThreshold`, `cardChorusChance`, the bands and the offer by band go.
- An understudy still has the cards its act began with (T24 changes that).
- The scripted players take an encore by their one card order. The guard of decision 22 holds on the tuned numbers, the table is in the pull request and the two hashes are pinned again. If no tuning holds the guard, that is the finding: the pull request says so with the best table and does not loosen the guard.
- The view: the encore's three cards on the program's panels, the bar as the way to the next encore, the capture's script taking an encore's leftmost card.
- The numbers the guard asked for: `encoreFirstCost` 3, `encoreCostGrowth` 1, `encoreTime` 6, and `budgetGrowthPerAct` from 80 to 200 (the second act has 215 enemies and the tenth 1815). `cardChorusDamage` stays 0.5. On them the orbit loses by act six in 20 of 20 and the doors player finishes act ten in 20 of 20, with 19.5 encores a performance: one in the first act on every seed, and some in every act (2.4, 5.6, 2.7, 1.9, 1.2, 4.5, 0.8, 0.3, 0.1 and 0.1 on average). On the seeds 101 to 120, which nothing was tuned on, the counts are 19 and 20.
- **What it costs, for the owner to weigh.** On the waves of before (80 more an act) one early encore was enough to carry a player that hides past act six: at a first cost of 3 the orbit never lost on its two wider circles, and only a first cost of 18, more pieces than the first act has critics, held the guard, with six encores a performance and none in act one. The owner asked for many upgrades taken in play, so the waves were made fuller instead. The second act is now a cliff, fourteen times the first: the orbit loses in it on every seed and every circle, and a player with no card by then does too. The doors player has at most 69 enemies on the stage at once (2, 45, 42, 55, 64, 42, 48, 51, 67 and 69 by act); a player that loses has up to 136, in the second act, where they pile up at the box office. The tests take some 15 seconds where they took 4.
- Nearby numbers under the rule as it stands, each over the seeds 1 to 20 (orbit, doors, the doors player's encores a performance): cost 3+1 on 180 more an act 18, 20, 18.9; on 160 more 12, 20, 17.9; on 220 more 20, 20, 20.3; cost 4+1 on 200 more 20, 20, 19.1; cost 2+1 on 200 more 18, 20, 19.9; and with the chorus card at 1, cost 3+1 on 200 more 20, 20, 19.3.

- **The ramp, looked for and not found: a finding for the next ticket.** Linear ramps with a fuller first act (30 to 80) and a gentler step (60 to 175 more an act) were played on the seeds 1 to 20 and 101 to 120, with first costs from 3 to 20. None whose second act is at most three times its first keeps the guard on both sets with ten encores a performance: 60 and 120 more needs a first cost of 20 (orbit 20 and 16, doors 16 and 19, 8.3 and 9.1 encores), and 70 and 140 more gets no further than 15 and 12 of 20. The two that keep it are no gentler where it counts: 50 and 150 more at a first cost of 12 (orbit 18 and 16, doors 18 and 20, 12.2 and 13.4 encores) has the same 200 enemies in act two, and 70 and 175 more at 14 (20 and 17, 17 and 20) fells the orbit in act one. In every row the orbit loses the same way, on every circle: the magician is felled, in act one or two, and the box office falls after it. It never loses by starving with the magician up. And on a fuller first act with cheap encores the two wider circles pick up enough by the box office to take four to eight encores and do not lose at all. So what holds the guard is a wave of some 200 by act two that fells a magician with no card, not the applause rule; the committed numbers were kept. A wave curve of another shape, a cap on the living, or a rule that makes applause near the box office worth less is what the next ticket has to weigh.

- **What the guard measures now, from the review: the first question of the next ticket.** It is saturated on both halves. The orbit loses in act two on every seed and circle with the magician felled, so "by the end of act six" tells nothing apart; and the doors player's box office stands at 376 from act five to act ten, so the acts of 1015 to 1815 enemies never touch it. As it passes, the guard says that a player with no card by act two dies and that a player with two encores in act one is not threatened again. It does not say what decision 22 means it to: that hiding loses and going out wins over ten acts.
- **The encore dries up after act six** (4.5 encores in act six, then 0.8, 0.3, 0.1, 0.1): by act eight an encore costs some 22 pieces and the magician picks up 16 to 18 an act, because the understudies make the kills and theirs leave no applause (144 pieces drop of the 1815 that enter act ten). "Many upgrades taken in play" is true of acts one to six. It bears on T24 and on the next tuning.
- **The owner's word on it (2026-10-08):** shown the three ways on (a rule that pays no applause near the box office, the numbers as they are, or dear encores on a gentle ramp), the owner chose many encores and still more enemies. The numbers stay; the step from the first act to the second, the waves' shape and how many critics are on the stage at once are the next ticket's, tuned to the owner's play.

**T24 An understudy takes its encores.** *After T23.*
The recording of an act keeps the tick of every encore and the card taken; an understudy has the cards its act began with and gains each of the others on its tick, in every later act.

**T25 Cards that change what a card does.** *After T24.*
Self cards that are not a number more: a card that goes on through the critic it strikes, one that turns to the next critic, one that bursts where it strikes. Each shows on the thrown card. Which of them, and what a second copy does, is settled with the owner when the ticket is taken.

## 6. Stops

| Stop | After | The question the owner answers by playing |
|---|---|---|
| 1. The toy | T09 | Is running and hitting pleasant by itself? |
| 2. The loop | T15 | Does an understudy read as "past me"? |
| 3. The rule | T20 | Is it worth leaving the box office? And the verdict: does the idea hold? |
| 4. The package | §7 | G1 of the vision, with five testers |

### The owner's verdict (stop 3, 2026-10-08)

**The idea holds** (the agent's wording, which the owner confirmed in chat). The owner played the build with the sprites and the set (after T07d) and said: it is taking shape; it is good to play when something is happening and one has to steer; by the second attempt it was already easier; the game already asks something of the player, and just standing does not win the game. Earlier the same day, on the build of T20, the owner had thought the third act probably could not be won and put the balance off ("balance later").

What follows from it:

- The proof of concept has answered its question. Stops 1 and 2 got no judgement of their own. On the build of T20 the owner said that it works, that the Vanish is on Space, that the magician throws by itself at single enemies, that the sounds are fine and the play is fine too, and that the idea "so far starts to get interesting". Stop 2's question (does an understudy read as "past me"?) has no answer on record.
- **The package for outside testers (stop 4) waits.** The owner wants to add a good deal to the game before anything is given out.
- The owner counts it a gain that the game can go to nearly any platform. As built it is MonoGame DesktopGL, which builds for Windows, macOS and Linux; decision 4's two targets for the testers' build stand, and the check on a real Windows machine (§7) is still open. A phone is another MonoGame project around the same Core, a console that and the platform holder's licence; neither is a part of this plan.

What the owner named the same day as missing or open, none of it a ticket yet:

- Is the stage always the same? (The owner did not know how other games have it.)
- Effects when the cards are thrown.
- The upgrades should show more, and there should be many more of them, so that at some point the screen is one great carnage.

## 7. Not yet specified

In scope, but not sharp enough for a ticket yet. The first four wait for the owner's word that the game is ready to be given out (§6):

- The log of a performance (a line per act: time near the box office, applause, cards, whether a second performance was started).
- The start screen and the way back to it.
- The two zips, checked on a real Windows machine and a real Mac, with a font file shipped in them.
- The G1 sheet: what is watched, what is asked, the template of the written judgement.
- What the owner's judgements at the stops ask to be changed: each becomes a ticket when it is given.

## 8. Out of scope

Not in the prototype (decision 18): the brute, the heckler and the Reviewer; kits other than thrown cards; evolutions and statuses; stage hazards; music and the pit orchestra; unlocks, modifiers, the score and the performance code; a settings menu; re-recording an understudy; art beyond decision 23 (walk cycles, portraits, a title screen), and the look test with its four candidate styles.
