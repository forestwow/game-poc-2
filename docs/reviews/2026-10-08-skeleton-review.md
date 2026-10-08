# Skeleton review, 2026-10-08

- **What was reviewed:** `main` at a7098df (T22 merged), the unmerged branch `t23-encore` (decision 26, tickets T23–T25), the five review captures in `art/frames/`, the manifests under `art/ludo/`, and the descriptions of pull requests #23 (T20) and #29 (T21). Two days of work, thirty pull requests.
- **How:** two read-only audits (the Core and the tests; the MonoGame layer and the art), then the claims that matter were checked by hand in the code. The .NET SDK was not available in the reviewing session, so no test was run; CI on `main` is green.
- **What this is:** an opinion on the skeleton and the ideas as implemented, for the owner. Nothing here is a ticket until the owner names it (plan §2).
- **Evidence:** every claim carries a path and a line, or says that it comes from a pull request's text. Line numbers are those of a7098df.

## 1. Verdict

The process and the engineering are a class above faith-defense, and the prototype answered its own question in two days: the owner's verdict at stop 3 is on record ("the idea holds", plan §6). That is the headline.

Three things threaten it, in this order. The one measurement that matters, the guard against hiding at the box office, passes partly for reasons that have nothing to do with the applause rule, and the tuning that made it pass turned the wave curve into a cliff. The understudy, which is the whole mechanic, is the least visible thing on the stage, and stop 2's question ("does an understudy read as past me?") was never put. And the encore of decision 26, as written, hands the hiding player free scaling.

None of this is a reason to change direction. All of it is a reason to fix the measurement before building on it.

## 2. Strengths

- **Discipline.** One ticket, one branch, one small pull request, a clean-context review, green CI, thirty times in a row. `docs/plan-prototype.md` is one source of truth with numbered decisions; `CLAUDE.md` is thorough and current enough to work from. faith-defense had twelve days and no verdict; this has two days and one.
- **The Core boundary is real.** `src/Understudies.Core` has no reference to MonoGame. About 1,900 lines of rules against about 6,600 lines of tests (234 `[Test]`, 63 `[TestCase]`, 15 source-driven cases, 19 classes). The view holds state of its own only for what the simulation does not report.
- **Determinism is taken seriously.** SplitMix64 streams with fixed numbers (`src/Understudies.Core/Randomness/Rng.cs`), an FNV-1a state hash over float bits (`Simulation.cs:410–536`), hash-coverage tests that change one thing and demand a different hash (`tests/Understudies.Core.Tests/StateHashTests.cs:126–177`), reference vectors for the RNG and the hash, pinned hashes of two scripted performances.
- **Every rule number lives in `tuning.json`**, parsed strictly (unknown and missing keys refused at any depth), reloaded with F5, copied beside the tests so a test never carries its own numbers.
- **The anti-orbit question became an ordinary CI test.** Two scripted players, a guard with a bar of 16 of 20, and a tuning changed to meet it (T19, T20). This is the faith-defense lesson applied, not just written down.
- **Juice and sound from the first week**, event-driven, with named constants (`src/Understudies.Game/Juice.cs`, `Sound.cs`); seven synthesised sounds and no sound file. The capture mode gives a reviewer a frame without playing.
- **The look has an identity for 53 credits.** "One odd part per figure" (a critic with an eye for a head, a box office with a mouth, doors with an eye) reads at a glance in `art/frames/1930.png` and `4700.png`. Every generation is in a manifest with its prompt, settings, job id and cost. `art/prompt-guide.md` is short and operational.
- **An unplanned good mechanic.** A critic turns on the magician within `criticTurnRadius` (4) and goes back when the magician leaves. The player can pull a crowd away from the box office. The doors player already uses it. This is the Herder proposal, emerging for free.

## 3. Weaknesses, ranked by what they threaten

### 3.1 The guard passes, but not for the rule's reason

From the text of pull request #23 (none of it is in the repository):

- Only the orbit at radius 0.5 loses for the vision's reason (no applause, so no cards). The orbits at radius 3 and 5 are kiting, and they lose mainly because T20 cut `magicianHitPoints` from 30 to 8.
- From act 3 the doors player's post is about 6.3 units from the box office (5.6 from act 6), so from then on the guard compares two players standing near the box office.
- The doors player is never touched by a critic.
- On held-out seeds the doors player reached 79 of 100 in one batch; the tuning was searched on seeds 1–20.
- Everything is decided in acts 2 and 3. For the doors player, acts 7–10 cost the box office nothing (it stays at 370.2 of 400) and the applause share falls to 0.01–0.09.

The margins are thin: after T21 the orbit is at 19 of 20 and the doors player at 17 of 20 (`docs/plan-prototype.md`, T21), and T21's own note says a pick-up reach of 1.0 would put the orbit at exactly 16, the floor.

Plainly: the applause rule from the vision is weaker than the vision claimed. On its own it did not make hiding lose; six tuning changes did (`firstActBudget` 25→15, `budgetGrowthPerAct` 8→80, critic speed 4→2, stagehand `fromAct` 2→4, `throwRange` 9→11, `magicianHitPoints` 30→8). The guard holds, and the design question "is it worth leaving the box office?" is answered only weakly.

### 3.2 The wave curve is a cliff

`tuning.json`: `firstActBudget` 15, `budgetGrowthPerAct` 80, both kinds at cost 1. Act 1 has 15 enemies, act 2 has 95, act 10 has 735. The readability and performance cap the vision set is about 100 on the stage; the push-apart is every pair, every tick (`Simulation.cs:803–812`, 4,950 pairs at 100, about 270,000 at 735). Late acts have no tension for the doors player (above), and the applause share, which divides pieces by entrants (`Simulation.cs:154`), collapses as entrants grow, so the late game offers no cards. That is the opposite of the "carnage" the owner asked for (plan §6). The owner's "act three probably cannot be won, balance later" is this cliff. The curve is an artefact of tuning to the guard, not a design decision.

### 3.3 The understudy is the least visible thing on the stage

In `art/frames/4700.png` the first understudy is a faint green figure at the bottom right with a hairline route. The treatment is a white silhouette copy tinted with the act's colour at half strength over the sprite at half opacity (`src/Understudies.Game/UnderstudiesGame.cs:986–991`); there is no render target and no shader anywhere in the game, though `docs/vision.md` §10–§11 planned both. The curtain is only the slide of the figures (`Rewound`, 766–802): nothing closes or opens, no sound marks it. The caption that explains the mechanic is a plain system font. An understudy's throw emits the same `Throw` event, flick and sound as the magician's (`Simulation.cs:997`; `TickEvent.cs` documents the event as the magician's), so a past self is neither seen nor heard apart from the present one.

Stop 2's question has no answer on record (plan §6). It is the gate for everything else.

### 3.4 The view holds rules

`UnderstudiesGame.cs` (1,129 lines) with `ProgramScreen.cs` (274) is one partial class with no screens; phases are `if` checks spread through `Update` (284), `Draw` (337), `DrawStage` (548, 694–701, 744), `DrawWords` (824–836) and `DrawFigure` (943). Decision 2 says this layer is thrown away after the look test, which excuses the shape but not the leaks, because they cost every ticket until then:

- The program's timeout picking the leftmost card is a Core rule repeated in the view (`Acknowledge(0)`, 382–386).
- `Describe` restates how each card changes the numbers (`ProgramScreen.cs:262–272`); `Held` re-maps cards to `SelfCards` fields (243–251).
- Enemy kinds by index: `critic.Kind == 0` (591).
- Hits are found by comparing hit points per critic between ticks (`Juice.cs:132–141`), because `TickEvent` carries a kind and a position and nothing else.
- A second facing lives in the view (553–558) and is not reset on `GoOn` or R.

### 3.5 Concrete bugs and regressions

| Where | What |
|---|---|
| `UnderstudiesGame.cs:83–89` | `FontFiles` lists only the macOS and Windows Arial paths; the comment and `README.md` promise two Linux fonts. On Linux the game has no text at all, so the program's panels are blank |
| `Simulation.cs:124` | `MagicianIsInvulnerable` is read by no view code; the cue for "the moment nothing hurts" that T07c required is gone |
| `art/ludo/sprites/kill-burst.png` | black ink and grey shards on a floor the guide made deliberately dark; the burst barely shows in `4700.png` |
| `Juice.cs:243` | the shake takes a fresh random offset every frame, so it buzzes harder at high refresh rates; its reach is 0.2 units, about 4 px |
| `UnderstudiesGame.cs:944` | every figure walks at 12 fps whatever its speed: critics at speed 2 shuffle, the stagehand at speed 8 slides |
| `UnderstudiesGame.cs` (R) | a new performance resets the simulation but not the clock, `_vanishAsked`, `_magicianToward` or `_highlighted` |
| `Juice.cs:165–179` | hit-stop only on the Vanish; shake only when the magician is hurt or the box office struck. A kill does nothing to the camera |
| `TickEvent.cs` | `Throw` is documented as the magician's; understudies emit it too (`Simulation.cs:997`, asserted by `UnderstudyTests.cs:236`) |
| `CLAUDE.md:34` | "the stagehand … from act two"; `tuning.json` says act 4 since T20 |
| `art/ludo/styles/manifest.json` | points at `art/frames/styles/` and a `--art` flag, both removed by T07c |
| `UnderstudiesGame.cs:1069` | `DrawUpright` is dead, left from the shape renderer |
| `Simulation.cs:154` | the applause share may exceed 1 (documented); a card in flight at the act's end scores applause in the next act |

### 3.6 Determinism on two machines is checked on one

The pinned hashes claim macOS ARM and Linux x64 (`ScriptedPlayersTests.cs:33–34, 61–62`); `.github/workflows/ci.yml` runs on `ubuntu-latest` only. The macOS half of the claim exists only when the author runs the tests locally.

### 3.7 Performance before the hundredth critic

Each shadow is 48 one-pixel strips (`FillDisc`, 1077–1092); every understudy's whole route is redrawn every frame (497–515), about 6,750 quads at nine understudies, and overlapping half-transparent lines will become spaghetti; the sorted batch switches among about fifteen textures, so hundreds of draw calls; the state hash walks every route on every call (`Simulation.cs:500`). None of it hurts today. It will at act 5 with five understudies and a hundred critics.

### 3.8 The encore has a hole

Decision 26 (branch `t23-encore`) gives the chorus card after every act, always, and drops "no applause, no card". A player that never leaves the box office then scales every understudy every act without touching applause. The plan says the guard stays and the numbers are tuned again, which is right, but the guard as it stands (3.1) will not see it: a kiting player that fetches applause is not among the scripted players.

## 4. Design notes beyond the code

- **The game that exists is a kiting game with a pull.** Critics at speed 2 against a magician at 9, a turn radius of 4, and nothing blocking the magician. That is good, and it is not quite what the vision describes (it describes routing). The understudy ties the two together only if a route is a decision; today it is a kiting trace.
- **"Is the stage always the same?"** (the owner, plan §6). A seeded door schedule and door placement per performance is cheap and answers half of it. The vision's three stages answer the rest and are out of the prototype's scope.
- **Mid-act upgrades (decision 26) are the right instinct** for "drawn in", and they also repair the collapsing share. The chorus card needs a price.
- **The owner wants to add "a good deal" before testers.** That is the faith-defense pattern. The package of plan §7 (a performance log, two zips with a font, the G1 sheet) is two or three tickets. Three friends on a zip would answer stop 2 and G1 (a)–(d) faster than ten tickets of content.

## 5. Proposed tickets

Each is one small pull request. They are proposals; the owner picks.

| # | Ticket | What | Why now |
|---|---|---|---|
| R1 | An honest guard | a second assertion on held-out seeds (for instance 101–120 at 14 of 20); an assertion of the mechanism: the orbits at 3 and 5 lose with fewer cards taken than the doors player, not with less life; a third scripted player, the kiter (orbit at 5, fetches applause within 9, the doors player's reach), as the true rival of "go to the doors"; the doors player's post anchored to its door (at most a third of the way to the box office); the caveats of pull request #23 written into the plan | the guard is the only measurement of the rule, and today it measures something else |
| R2 | The curve and the cap | a geometric budget (about 1.35× an act, a hypothesis) under a cap of enemies alive on the stage (about 100); a cell grid for the push-apart; the tuning searched again under R1 | the cliff of 15 → 95 → 735, and the pairs |
| R3 | A legible understudy, and stop 2's question | stronger opacity and a paper treatment (a render target and one shader, or a better wash); an act badge over its head; the route as a fading ribbon of the last few seconds instead of the whole line; a visible curtain in the rewind, with a sound; a quieter, different flick and sound for an understudy's throw; then a capture of act 5 with four understudies and the owner's written answer | the mechanic is the least visible thing on the stage; the gate is unpassed |
| R4 | No free chorus | the chorus card only after an act with at least one encore (or one piece of applause); the guard with the kiter before T23 merges | decision 26's hole |
| R5 | Core speaks, the view listens | `TickEvent` with the critic's id and who threw (the magician or an understudy's index); Core exposes the card a timeout took; kinds by name; a `Describe` of a card in Core; one `Pressed` helper; `DrawUpright` gone | every new rule is an edit in two places until then |
| R6 | The small bugs | an OFL font in the repository and in `FontFiles` now, not "when a build leaves the machine"; the invulnerability cue; the kill burst's contrast; the shake sampled per tick; the walk's frame rate from the figure's speed; a full reset on R; `CLAUDE.md`'s stagehand act; the `Throw` doc; the paths in `styles/manifest.json` | each is small; together they are the difference between "prototype" and "sloppy" |
| R7 | macOS in CI | a macOS job, even weekly, or the claim reduced to one machine | a pin without its second machine |
| R8 | Before the hundredth critic | a sprite atlas, a shadow as a sprite, the ribbon of R3, a hash that does not walk every route on every call | it ignites at act 5 |
| R9 | G1 now | the performance log, the two zips with a font, the G1 sheet; three friends on a zip with stop 2's question and G1 (a)–(d) | content before testing is the pattern to avoid |

## 6. What not to change

The process. The Core boundary. `tuning.json` as the only home of a number. The art manifests. Synthesised sound at this stage. The `ponytail:` comments: they name every known ceiling honestly (26 of them), and they are why this review could be written in an afternoon. The comment style in general: about 38 % of Core's lines are prose in a voice of its own, slow for a reader who is not the author; shorten it when a line is touched, do not rewrite it.

## 7. For the owner

1. Which of R1–R9, and in what order? The review's order is R1, R3, R2, R4, then the rest.
2. Does G1 go now (R9) or after the content the owner has in mind?
3. Stop 2's question, answered in writing on a capture of act 5: does the figure read as "past me"?

## Appendix: what was verified directly

Checked in the code of a7098df: the font list, `MagicianIsInvulnerable` unread by the view, `DrawUpright` unused, the share's definition, the shake and hit-stop triggers, the shape of `TickEvent`, the pairwise push-apart and its `ponytail:` note, the CI runner, the stale stagehand act in `CLAUDE.md`, the line counts, the test counts, the tuning numbers. Taken from the text of pull requests #23 and #29 and from `docs/plan-prototype.md` T20–T21: the reasons each orbit radius loses, the doors player's post distances, the held-out result of 79 of 100, the six numbers T20 changed, the margins after T21.
