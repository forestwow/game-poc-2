# The Understudies: Vision v0.2

- **Status:** preliminary vision, written 2026-10-07 after six rounds of a grill-me style interview (22 decisions, listed in §15). v0.2, the same day, adds the applause rule after the owner asked whether everyone would simply orbit the box office; §13 now opens with that risk. It describes the lead direction chosen in `docs/brainstorm/2026-10-07-direction-proposals.md` (Echo Loop) in its chosen skin. Every number is a hypothesis for the first prototype unless marked as a decision.
- **What it is not:** a spec, a plan, or a content list. Rules are frozen until the prototype gate in §12 is judged in writing. That discipline is the main lesson of `faith-defense`, which changed direction seven times in twelve days without a playtest.
- **Working title:** The Understudies. Alternatives: Encore, Standing Ovation, Many of Me.

## 1. Pitch

You are a stage magician with a terrible problem: every performance you give leaves behind a cardboard understudy that keeps performing it, forever, exactly as you did. The critics are coming for the box office. Each act lasts about a minute. By act eight you are defending the theatre beside seven paper copies of yourself, each still doing the trick you taught it that night. Where you run is what you build.

A short, fair, replayable roguelite for PC where tower defense and a survivors-style hero are the same input: your own route. And the audience applauds only the star, so hiding among your copies is the one thing you cannot afford.

## 2. Pillars

1. **Where you run is what you build.** There is no build menu. Every route you choose under pressure becomes a patrolling defender next act. Routing is the strategy; fighting is the action; they are one decision. And the stage applauds only you: the live magician's kills are the only ones that pay, so standing among your understudies is safe and sterile, and going out to meet the critics is how you grow.
2. **Every act, more of you.** The screen fills up with your own past choices. Progression is visible by act three without a long upgrade tree, because every effect you picked is multiplied by the number of understudies.
3. **A stage, not a spreadsheet.** Combat must feel physical: hit flash, hit-stop, smoke, paper tearing, a pit orchestra that gains an instrument per understudy. This is the direct answer to what felt static in faith-defense, and in MonoGame it is a system we build, not one we get.
4. **Short, fair, again.** Ten acts of 75 seconds, one loss condition, no power creep between runs. A lost performance costs fifteen minutes and teaches a route.

## 3. The world and the tone

- **Tone:** Plants vs. Zombies energy in a vaudeville theatre. Paper and cardboard, footlights, velvet, hand-painted backdrops. Nothing is explained seriously. Absurd, warm, a little mean.
- **The magician:** The Great Somebody, a touring act of modest fame. Their curse is the mechanic: each act leaves an understudy. They treat it as a staffing solution.
- **Understudies:** cardboard cutouts of the magician, painted in the colour of the act they came from, slightly creased, moving on a hidden rail. They never speak. They never miss their cue.
- **Applause:** the audience cheers the star, never the cardboard. Only the live magician's kills drop applause, and only the live magician can pick it up. This is the fiction behind the anti-orbit rule in §4.
- **The box office:** the thing you defend. If the critics reach it and tear it down, the show closes. In play it is the Heart from the proposals document: one object, one loss condition.
- **The enemies:** critics with pens (walkers), hecklers who throw things (ranged, later), stagehands who sabotage (runners), the understudy of a rival magician (brute), and The Reviewer, a boss who arrives with a column to write.
- **The stages:** a street corner (act one of the campaign, open on three sides), a variety theatre (aisles as lanes, a trapdoor), an opera house (balconies, an orchestra pit that swallows anything pushed in). Three stages in v1.
- **Why this skin:** it explains the echo mechanic in one sentence, it frees the art from the exhausted faith-defense world, and it gives every system a diegetic name: acts, the program, the curtain, the pit.

## 4. The core loop

A **performance** is a run. It has ten **acts** (rounds). Between acts you read the **program** (pick a card, paid for with the applause you collected in the act). The performance ends when the box office falls (loss) or act ten ends (a standing ovation, with a score).

| Element | v0 hypothesis | Decision or hypothesis |
|---|---|---|
| Act length | 75 s; test 60 and 90 | hypothesis |
| Acts per performance | 10 | hypothesis |
| Program pick | 12 s, one to three cards | hypothesis |
| Applause | a pickup drops where the live magician lands a killing blow, never where an understudy does; only the live magician can collect it; it fades after about 4 s | decision; the fade time is a hypothesis |
| Cards offered | by applause collected in the act: 0–2 one common card, 3–5 two cards, 6 or more three cards with a chance of a chorus card | decision that applause gates the program; the thresholds are a hypothesis |
| Performance length | ~14.5 min | follows from the above; the 10–15 min target is a decision |
| Act end | the timer, always | decision |
| Hero death | control ends for the act, the understudies finish it, the recording stops at the death | decision |
| Loss | box office HP reaches 0 | decision |
| Restart | a new performance from act one; nothing carries over except unlocks | decision |

The act timer is not presentation. The understudy needs a bounded recording, so the short round is the mechanic itself.

Applause is the rule against the obvious degenerate strategy. Ten understudies orbiting the box office are ten turrets in one place, and the faith-defense balance bot already showed that towers at the relic win. With applause only for the live magician's kills, hiding in the pile is safe and sterile: no applause, no cards, and the budget-driven acts outgrow you by act six. Growing means leaving the pile to meet fresh critics at the doors before your understudies reach them, and every such route is the next understudy's patrol. Doors open one by one over the acts, so new routes go to new doors.

## 5. The player

- **Controls (decision):** move (stick or WASD), auto-attack on the nearest enemy in range, one active skill on a button with a cooldown. Nothing else. Controller support from the first prototype.
- **Starting kit (decision):** thrown cards as the auto-attack, fast and readable, with a fan of cards as its evolution; the active skill is the Vanish, a short dash in a puff of smoke with a moment of invulnerability and a cloud that stuns what it touches.
- **Death rule (decision):** when the magician falls, the act goes on. You watch your understudies finish it. The recording of this act stops at the moment of death, so this understudy will vanish at the same second in every later act. The penalty is visible, natural, and needs no extra fail state.
- **What the player is doing, moment to moment:** reading where the critics come from this act, deciding which door to meet them at before the understudies get there, collecting the applause that falls where you kill, and timing the Vanish. The tension of every act is the same: stay in the safe pile and starve, or go to the doors and grow. Thronefall showed that two inputs are enough when positioning is the skill.

## 6. Understudies

- **Recorded (decision):** position per tick, facing, the tick of every Vanish, the kit and the cards of that act. Tiny data.
- **Replayed (decision):** the same route at the same speed. The Vanish fires at the recorded ticks. Auto-attack targets live enemies in range. An understudy never attacks nothing.
- **Durability (hypothesis):** invulnerable, like a tower. Critics target the box office and the live magician. Breakable or fading understudies are a later experiment, not v0.
- **Cap (hypothesis):** equal to acts per performance, ten. No "which understudy to replace" interface in v0.
- **The curtain (signature moment):** at the start of every act, one second in which every understudy snaps back to its starting mark along its own route, like a rewind. This is the visual that teaches the mechanic without a tutorial.
- **Resonance (considered and rejected on paper):** v0.1 had a bonus for standing near an understudy. It would pull the magician toward the pile at the box office, the exact behaviour the applause rule exists to punish. Out, unless G1 shows the opposite problem.
- **Re-record (later):** mark one understudy before an act to overwrite it with this act's route. It fixes understudies that patrol a route the critics no longer use. Not in v0: the prototype must first show whether dead routes are a problem or the strategy.
- **Why not full determinism:** because targeting is live, the world does not need to replay identically. A deterministic Core is still worth having for the balance bot and for run replays, as in faith-defense.

## 7. Progression

### 7.1 Inside a performance: the program

One to three cards after every act, as many as the applause of that act paid for (§4). No applause, no program.

- **Self cards** apply to the *next* understudy you record: a kit change (doves, wand, hat, saw), attack speed, Vanish cooldown, move speed, a status on hit (ink, burn, confetti slow).
- **Chorus cards** are rarer and apply to every understudy at once: damage, range, a shared status. They give late-performance scaling and a reason to care about the understudy from act two.
- **Evolutions:** two matching self cards on one understudy change its kit into a visibly different trick, as in survivor.io: a fan of cards becomes a card storm; two doves become a flock.
- **Applause is a magnet, not a currency:** it is collected or lost within seconds, so it moves the magician around the stage. It is never banked between acts.
- The army at act ten is a museum of your choices. The archer act stays an archer; the burning act keeps burning.

### 7.2 Between performances: unlocks only (decision)

No permanent power. Thronefall's model, not survivor.io's.

- **Kits:** cards (start), doves (homing), wand (beam), hat (rabbits as mines), saw (melee arc). Four kits in v1 besides the start.
- **Stages:** the three above, unlocked in order.
- **Modifiers:** matinee (easier, lower score) and gala conditions (harder, higher score), the Thronefall perk-and-challenge pattern: more critics, shorter acts, no Vanish, blind curtain (no rewind preview).
- **Scoring:** box office HP left, kills, modifiers. A seed and a shareable performance code, as faith-defense had for maps.
- **About twenty unlocks in v1 (decision on scope).** Enough to give a reason for the fifth performance, not enough to grind.

## 8. Enemies and acts

- **Waves:** budget-based, with the generator pattern from `faith-defense` (`src/FaithDefense.Core/Waves/`): each act has a budget, each enemy a cost, and the composition is drawn from the act's allowed set.
- **Stage doors (gates):** two to four per stage, opening over the acts, so routes stay stable enough for old understudies to stay useful. The spawn-schedule idea from the faith-defense direction synthesis (F2).
- **Cast of v1:**
  - **Critic** (walker): the baseline, in every act.
  - **Stagehand** (runner): fast, low HP, from act two. Punishes a route that ignores a side door.
  - **Rival's understudy** (brute): slow, high HP, from act four. Needs focused damage or a pit.
  - **Heckler** (ranged): throws from the aisles, from act six. The only enemy that threatens the magician at range.
  - **The Reviewer** (boss): acts five and ten. Walks straight at the box office writing a column; each paragraph is a shockwave.
- **v0 prototype uses only the critic and the stagehand.**

## 9. Stages

Three in v1, each a single screen, top-down three-quarter view, with a different door layout and one hazard that the Vanish or a route can exploit.

| Stage | Doors | Hazard | Teaches |
|---|---|---|---|
| Street corner | 3 (left, right, back alley) | a fountain that slows | the loop: run, record, repeat |
| Variety theatre | 4 (two aisles, stage left, stage right) | a trapdoor that opens on a cycle | covering gaps with understudies |
| Opera house | 4 plus balconies for hecklers | the orchestra pit: anything pushed in is gone | the Vanish as a tool, chorus cards |

The box office sits off-centre so routes are asymmetric. One stage in v0, placeholder shapes.

## 10. Audio-visual direction

- **Perspective (decision):** top-down three-quarter, as in survivor.io and Vampire Survivors. Four-direction walk cycles. No isometric depth sorting.
- **Art pipeline:** PixelLab, with the faith-defense prompt guide (`art/pixellab/prompt-guide.md`) rewritten for a theatre palette: warm footlights, deep velvet reds, paper whites, ink blacks. The tiles and enemies of faith-defense are isometric and do not transfer; the method and the lessons do. One afternoon to compare ludo.ai on the same prompt before committing.
- **Understudies on screen:** the same sprite as the magician through a shader: desaturated, paper-textured, tinted by the act it came from, semi-transparent, with a faint trail of its route. The live magician is the only bright, saturated figure on the stage.
- **Juice, required in the first prototype (decision):** particle system, hit flash, hit-stop on the Vanish and on kills of brutes, screen shake on big hits, projectile trails, enemies that tear into paper scraps and leave a fading corpse, floating damage numbers as an option, placeholder sound from day one.
- **Music:** a pit orchestra that gains one instrument per understudy. Act one is a lone piano; act ten is the full band. Implemented as stems layered in by understudy count. Cheap to build, and it makes the mechanic audible.
- **Readability cap:** ten understudies, one magician, up to about a hundred critics on one screen, at a fixed internal resolution with point filtering.

## 11. Technology

- **Engine (decision):** MonoGame, DesktopGL, Windows and Mac via Steam. No editor; data in JSON with fingerprints, as in faith-defense; stages as text files, as faith-defense maps were.
- **Core (decision):** a separate deterministic C# project with no engine dependency, carrying the faith-defense patterns: fixed tick with interpolation, a command queue, SplitMix64 streams per subsystem, a state hash with golden tests, the budget wave generator, the balance bot. An understudy's recording is a list of commands, which the command queue already models.
- **Tick rate (hypothesis):** faith-defense ran 20 ticks per second, which was enough for towers. A controlled hero may need 30 or 60. Decide in the prototype by feel.
- **Rendering:** a render target at a fixed internal resolution scaled to the window, point sampling, HLSL for the paper shader.
- **Libraries to evaluate in week one, not before:** MonoGame.Extended, Nez, Myra. Load PNG and WAV directly at runtime for the prototype; the content pipeline can come later or never.
- **Co-op (decision):** solo in v1. The command queue and deterministic Core keep the door open for local co-op after v1, where the second player is a live understudy.
- **Web:** not a target. The KNI fork exists but playtests ship as a zip.

## 12. Scope, roadmap and gates (decision on scope)

**v1 scope:** three stages, four unlockable kits plus the start, ten acts per performance, about twenty unlocks, three to five hours of content, a price in the 8–13 USD range, PC on Steam, solo. About six months of evening work.

| Phase | When | Deliverable | Gate |
|---|---|---|---|
| Prototype | week 1 | one stage in placeholder shapes with real juice: magician (move, cards, Vanish), box office, critic and stagehand with budget waves, 75 s acts, recording and replay, applause pickups and the applause-gated program, the curtain, placeholder sound, and two scripted players (orbit and doors) for the guard in G1 | **G1** below |
| Playtest and decision | weeks 2–3 | five testers who are not the owner; written judgement of G1 | go, or the next proposal gets its week |
| Vertical slice | months 2–3 | the variety theatre with final art for the magician, understudies, critics and the stage; the pit orchestra; four kits; the Reviewer | **G2** |
| Demo | month 4 | the street corner and the variety theatre on itch.io, a Steam page | **G3** |
| Content and polish | months 5–6 | the opera house, unlocks, modifiers, score codes, controller polish, Steam release | release |

**G1, prototype (the only question that matters now):** after act five, does the tester (a) say something like "that is my army" unprompted, (b) plan a route deliberately rather than kiting at random, (c) start a second performance without being asked, and (d) leave the box office's surroundings of their own accord by act three? With five testers, three of each is a pass. The guard is measured, not asked: (e) a scripted orbit player, which circles the box office and never leaves its radius, must lose by act six, and a scripted doors player, which runs to the nearest open door, must reach act ten. This is the faith-defense map standard's rule ("static plans lose") applied to routes, checked by the balance bot at every tuning as `LevelTuningTool` did. A fail on (a)–(d) means the understudy is a gimmick, and the Herder proposal gets its week. A fail on (e) alone means the applause rule is too weak and the fallback in §13 goes in before the next playtest.

**G2, vertical slice:** does the cardboard read as "past me" without a caption; does the orchestra layering land; does a performance stay interesting to act ten with one stage. Judged by five testers and written down.

**G3, demo:** share of testers who finish a performance and share who start a second one, measured, with a target set after G2.

Rules may change only at a gate, in writing. Between gates, only content and polish.

## 13. Risks

| Risk | Mitigation |
|---|---|
| Everyone orbits the box office, which is the faith-defense result (towers at the relic win) in a new skin | applause only for the live magician's kills, the applause-gated program, the orbit-must-lose guard in G1. Fallback if the guard fails: the audience as the HP bar (every living critic on stage costs mood per second, so intercepting at the doors beats waiting at the centre) and positional threats (hecklers on balconies, stagehands killing the lights at the doors) |
| The mechanic is not understood in the first two acts | the curtain moment; the first understudy appears in act two with one caption; G1 measures it |
| Understudies patrol routes the critics no longer use | stable stage doors; chorus cards; re-record only if G1 shows it is a problem rather than the strategy |
| Ten paper copies plus a horde become mush | the paper shader, one bright magician, the hard cap, fixed internal resolution |
| One stage per performance feels samey | doors opening over acts, the brute from act four, the Reviewer in five and ten, a second stage only after G2 |
| MonoGame ships no juice, so it gets postponed | juice is in the week-one deliverable list, not a later phase |
| The owner pivots again before a playtest | rule changes only at gates; the prototype is thrown away on a fail, not extended |
| Comparison to Time Rifters and the jam games | different genre, platform and progression; say so openly on the store page |
| The comedic art is hard to generate consistently | the vertical slice exists to find out; the abstract skin (Brushstrokes) is the fallback |

## 14. Out of scope for v1

Co-op, structures and build slots, a meta map, more than three stages, mobile, web, online leaderboards, localisation beyond English and Polish, a level editor, breakable understudies, re-record.

## 15. Decisions from the interview (rounds 1–6)

| # | Topic | Decision |
|---|---|---|
| 1 | Direction | Echo Loop; Rolling Fortress dropped after prior art (Bearicade, Land Trains) |
| 2 | Hero | yes, in the centre; understudies are support |
| 3 | Rhythm | a 10–15 min performance of short acts, a card between acts, restart on loss |
| 4 | Stack | fresh; Unity out; MonoGame; deterministic Core as a separate project |
| 5 | Controls | move, auto-attack, one active skill |
| 6 | What an understudy replays | route and skill timings; targets are live |
| 7 | Structures | none in the prototype; a later layer only if G1 says the strategy is thin |
| 8 | Platform | PC/Steam, Windows and Mac, controller from day one |
| 9 | Kingshot | the hero-versus-horde fantasy the owner remembered is Kingshot's ads, not its game; a validated fantasy with no game behind it |
| 10 | Perspective | top-down three-quarter |
| 11 | Act end | the timer; hero death cuts the recording and hands the act to the understudies |
| 12 | Meta | unlocks only, no power |
| 13 | Setting | The Understudies |
| 14 | Scope | small v1 on Steam, about six months |
| 15 | Co-op | solo in v1, door open |
| 16 | Starting kit | thrown cards and the Vanish |
| 17 | Arena | one stage with doors opening over acts in v0 |
| 18 | Prototype budget | one week |
| 19 | Process | gates with written pass/fail questions; rules frozen between gates |
| 20 | Applause | drops only on the live magician's kills, is collected only by the live magician, and is the only source of cards |
| 21 | Orbit guard | a scripted orbit player must lose by act six on every stage and a scripted doors player must reach act ten; measured by the bot at every tuning |
| 22 | Resonance | cut; it pulled the magician toward the pile |

## 16. Open questions (small, not blocking the prototype)

1. The name: The Understudies, Encore, Standing Ovation, or something else.
2. Tick rate: 20, 30 or 60, by feel in week one.
3. Applause thresholds: how much applause buys one, two or three cards; and whether bouquets thrown from the audience onto the stage's edges are needed as extra pickups if kills alone are too weak a magnet.
4. Whether the Vanish cloud stuns or only blinds; whichever reads better on screen.
5. Which four gala modifiers ship in v1.

## 17. Glossary

- **Performance:** a run. Ten acts.
- **Act:** a round. About 75 seconds.
- **Program:** the card pick between acts.
- **Understudy:** an echo. A recorded act that replays its route and its Vanish timings with live targeting.
- **Box office:** the object to defend. The only loss condition.
- **Applause:** the pickup that drops where the live magician kills and that only the live magician can collect. The only source of cards.
- **Curtain:** the one-second rewind at the start of every act.
- **Stage door:** a spawn gate.
- **The Reviewer:** the boss of acts five and ten.
- **Gate (G1–G3):** a written pass/fail judgement between phases.
