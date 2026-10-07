# Game v2: Direction Proposals

- **Status:** brainstorm, written 2026-10-07 after three rounds of a grill-me style interview with the owner. No code, no project skeleton, no assets. The next step is interview round 4 and then a vision document for the chosen direction.
- **Owner's brief (Polish, summarised):** a second attempt at a game after `faith-defense`. Still strategy / tower defense, but the owner is looking for *the one mechanic that convinces them*. Inspirations: Thronefall (short, accessible, a hero fights beside troops, building in preset spots) and survivor.io (hero progression where every upgrade is visually loud). Nice to have: short, gripping rounds; a mix of mechanics; combat rich in effects, the player pulled into the fight and the world. The static feel of faith-defense combat was the thing that broke the owner's enjoyment. Story may be absurd (Plants vs. Zombies energy), mixed styles, abstraction welcome. Art from ludo.ai or PixelLab.

## 0. TL;DR

**Recommendation: Echo Loop.** Each 60–90 s round you play one run with a hero. When the round ends, that run becomes an Echo: a ghost that replays your path and skill timings forever after, but attacks live enemies. By round 8 you fight beside seven past selves, each carrying the upgrades you picked in its round. "Placing a tower" becomes "choosing where to run". The screen fills up by itself; rounds are short by construction; the fantasy is understood on sight.

Three interview rounds settled: hero in the centre, survivor.io-style controls (move, auto-attack, one active skill), runs of 10–15 minutes built from short rounds, MonoGame as the engine, and a first prototype of echoes plus one object to defend, nothing else. Rolling Fortress (convoy with wagons as build slots) was the runner-up and was dropped after a prior-art check: Bearicade and Land Trains already do it.

Six alternatives are kept below with a prior-art verdict each, so the choice is documented, not just made.

## 1. What faith-defense taught us

Facts from the repository (`/home/user/faith-defense`, squashed commit of 2026-10-03), not from memory.

| Lesson | Evidence | Consequence for v2 |
|---|---|---|
| Direction changed ~7 times in 12 days and the core fun was never playtested | `docs/superpowers/specs/`: classic TD → puzzle → mazing → siege → pilgrims → travelling relic → camp on the march; the last spec freezes rules "until the owner has played" | The biggest risk is not a lack of ideas but building systems before a toy is fun. v2 gets one verb, one arena, a playable toy within days, and a written pass/fail question before any second system |
| Combat read as static for concrete reasons | `unity/Assets/Scripts/Runtime/Gameplay/*View.cs`, `*Pool.cs`: no hero, no hit flash, enemies vanish on death, no damage numbers, no sound, no screen shake, no hit-stop, hits are instant in the sim and projectiles are cosmetic; the owner asked for motion "in measure" | A new mechanic alone will not fix this. Juice (particles, flash, hit-stop, shake, trails, sound) is a first-class system in the first prototype, especially in MonoGame where nobody ships it for you |
| The owner liked the travelling caravan, chunky block terrain, animated crews, measured motion | camp-on-the-march spec §1, direction synthesis §2 | Movement of the world and of the player's own things is what landed. Keep that instinct |
| The owner disliked fixed build slots ("artificial"), pilgrims ("nothing to care about"), palisades that did nothing | M6 tower spec, pilgrims specs, bot measurements | Slots in Thronefall work because each is a decision with a visible cost: the hero walks there and pays. In faith-defense a slot was an arbitrary point on a free-path map. In Echo Loop the "slot" is a path you ran yourself |
| Strong engineering is reusable regardless of engine | `src/FaithDefense.Core/` is netstandard2.1 with no Unity dependency: SplitMix64 streams per subsystem, fixed tick with interpolation, command queue, state hash and golden tests, strict JSON config with fingerprints, budget-based wave generator, flow fields, balance bot | The Core patterns move to MonoGame almost as they are. The Unity view layer does not |
| The art pipeline is a real asset | `art/pixellab/prompt-guide.md` (style bible, palettes, lessons), `art/pixellab/blocks.py` (autotiling), `art/pixellab/crew.py` (compositing, motion extraction), `tools/FaithDefense.AssetGen` (provider, lockfile, slicing). ludo.ai appears nowhere in the repo | PixelLab is the validated path. The tiles are isometric 64×32 and enemies have SE/NE walk sheets only, so a top-down game reuses the method and the style, not the sprites |

## 2. Criteria

Used in the comparison matrix in §7.

1. **One verb.** A single core action that is the reason to play, not a pile of systems.
2. **Short rounds.** 60–180 s, with a "one more round" pull.
3. **Inside the fight.** A controlled actor, loud feedback, and a screen that gets busier as you progress.
4. **Mix potential.** Room for a second and third layer *after* the verb is validated.
5. **Setting freedom.** The mechanic works in a medieval, absurd or abstract skin.
6. **Reuse.** Design, art pipeline, process and Core patterns from faith-defense apply.
7. **Prior-art openness.** Not already done by a visible commercial game.
8. **Prototype cost.** Days, not weeks, to a playable toy that answers the pass/fail question.

## 3. Decisions already made (interview rounds 1–3)

| # | Topic | Decision |
|---|---|---|
| 1 | Lead direction | **Echo Loop**. Rolling Fortress dropped after prior art; the convoy survives as a setting option |
| 2 | Hero | Yes, in the centre. Echoes and any structures are support |
| 3 | Rhythm | A run of 10–15 minutes made of 60–120 s rounds with an upgrade pick between rounds; restart on loss |
| 4 | Stack | Fresh. Unity is out. **MonoGame** for the prototype and the game, with a deterministic Core as a separate project like in faith-defense |
| 5 | Controls | Move + auto-attack on the nearest enemy + one active skill (survivor.io) |
| 6 | What an echo replays | Path and skill timings. Targets are live |
| 7 | Structures | Prototype: echoes plus one object to defend, nothing else. Structures are layer two, only if the playtest says the strategic layer is thin |
| 8 | Target platform | PC/Steam (Windows, Mac), as decided for faith-defense on 2026-10-03. An assumption to confirm in round 4 |

## 4. Lead direction: Echo Loop (vision v0 draft)

Working title: **Many of Me**. Everything numeric below is a hypothesis for the prototype, not a spec.

### 4.1 The one mechanic

You defend one object (the Heart) in one arena. A round lasts about 75 seconds. You play one hero: run, auto-attack whatever is nearest, use one active skill. When the round ends, your run is recorded and becomes an Echo.

An Echo replays your exact path and the moments you used your skill, every round from now on, but it attacks whatever is actually in range *now*. It is a tower that patrols the route you chose, with the weapon and upgrades you had in that round. Round two you fight beside one past self, round eight beside seven.

Between rounds you pick one of three upgrades. The pick applies to the *next* self you record. Old echoes keep what they had. The army you end up with is a museum of your own choices: the archer self from round two still runs its archer loop while the flame self from round six burns the gate.

### 4.2 Why this answers the brief

- **It is one verb.** "Where do I run this round" is the whole game. Routing is the strategy, fighting is the action, and they are the same input.
- **Rounds are short by construction.** The echo needs a bounded recording. The timer is the mechanic, not a presentation choice.
- **The screen fills up on its own.** Every effect you picked is on screen multiplied by the number of echoes. Survivor.io needs a long upgrade tree to get there; here it emerges from the mechanic by round five.
- **Slots have a reason.** The owner found fixed slots artificial. Here a "slot" is a path, chosen while under pressure, and its cost is that you could have been somewhere else.
- **It uses what the owner liked.** Moving things that are yours. Crews that animate. The caravan was loved for the same reason: your stuff moves.
- **It kills the static feel structurally.** The hero is the centre, and the number of moving, shooting allies grows every round.

### 4.3 A round, a run (hypotheses)

| Element | v0 hypothesis | Why |
|---|---|---|
| Arena | One screen, the Heart off-centre, 2–4 spawn gates on the edges that open over the rounds | Stable enemy routes keep old echo paths relevant (faith-defense's spawn schedule idea, F2 in the synthesis) |
| Round length | 75 s; test 60 and 90 | Long enough to run a loop, short enough to replay in your head |
| Rounds per run | 10 | 10 × (75 s + 12 s card pick) ≈ 14.5 min |
| Round end | Timer. If the hero dies, control ends for the round, the echoes finish it, and the recording stops at the death | Death cuts the echo short in every later round. A natural, visible penalty with no extra fail state |
| Run end | Heart HP reaches 0 | One loss condition |
| Enemies | Budget-based waves (reuse the faith-defense generator idea), 2 types in v0 (walker, runner), a brute from round 4 | Variety only once the loop works |
| Upgrade pick | 3 cards after each round: "self" cards (weapon, speed, skill) applied to the next recording, and rarer "chorus" cards that buff every echo | Chorus cards give late-run scaling and a reason to care about old echoes |
| Echo cap | Equal to rounds per run (10) | Perf and readability; no "which echo to overwrite" UI in v0 |
| Echo durability | Invulnerable, like a tower; enemies target the Heart and the live hero | Simplest rule. Fading or breakable echoes are a later experiment |

### 4.4 The hero

Move with stick or WASD. Auto-attack on the nearest enemy in range, with a weapon that defines the attack pattern (arc, line, burst). One active skill on a button with a cooldown (dash in v0; later skills: a pull, a ward, a shout that re-targets echoes). Nothing else. Thronefall proved that two inputs are enough when positioning is the skill.

### 4.5 Echoes: the rules

- **Recorded:** position per tick, facing, the tick of each skill use, the weapon and upgrades of that round. Tiny data.
- **Replayed:** the same path at the same speed. Skills fire at the recorded ticks. Auto-attack targets live enemies in range.
- **Not required:** full determinism of the whole world. Because targeting is live, an echo never shoots at nothing. A deterministic Core is still worth having for the balance bot and for run replays, as in faith-defense.
- **Resonance (hypothesis):** when the live hero is within range of an echo, both get a small bonus and a visible link. This rewards running *with* your past selves and makes the choreography visible. Test it; cut it if it makes routing feel forced.
- **Re-record (later):** mark one echo before a round to overwrite it with this round's run. Fixes echoes that patrol a route the enemies no longer use. Not in v0: first find out whether dead routes are a real problem or the strategy.
- **The signature moment:** at round start, one second of "rewind": every echo snaps back to its start position along its own path. This is the visual that explains the mechanic without a tutorial.

### 4.6 Progression

- **Inside a run:** cards. Self cards change the next echo (weapon type, attack speed, skill cooldown, movement speed, a status on hit). Chorus cards change all echoes (plus damage, longer range, a shared status). Evolutions as in survivor.io: two matching self cards on one echo turn its weapon into a visibly different one.
- **Between runs (round 4 question):** unlocks, not power. New starting weapons, new arenas, new skill. Thronefall's perk model is the reference; survivor.io's paid power creep is not.
- **Score:** Heart HP left and kills. Later: a seed and a shareable run code, as faith-defense had for maps.

### 4.7 Where the visual richness comes from

- Each echo renders as a ghost: tinted by the round it comes from, semi-transparent, desaturated, with a faint trail of its path. The live hero is the only bright, saturated figure.
- Every weapon has its own projectile and impact. Hit flash on enemies. Hit-stop on the active skill. Screen shake on big hits. Enemies die into particles and leave a fading corpse. Sound from day one, placeholders included.
- The multiplication is the point: pick a flame weapon in round three and from then on there is a flame echo on the screen every round, forever.
- Readability cap: ten ghosts, one hero, up to ~100 enemies on one screen. Pixel art with point filtering and a fixed internal resolution keeps this cheap.

### 4.8 Genre mix

Action (your run) + tower defense (echoes are patrolling towers you place by running) + roguelite drafting (cards per round, restart on loss). Later layers, only after the gate in §4.12: structures (an anchor that holds an echo in place, a gate that redirects enemies), a second arena, bosses every five rounds, a meta map.

### 4.9 Setting options

The mechanic does not care about the skin. Four options, with the owner's "story can be totally made up" in mind.

| Option | Pitch | Pro | Con |
|---|---|---|---|
| **The Relic Vigil** (medieval) | A monk in a time loop keeps vigil over a relic; every vigil leaves a ghost that keeps vigil with you | Fits the faith-defense world and its PixelLab style bible; enemies and props exist | No hero art exists; iso sprites do not fit a top-down arena; the owner may be tired of the world |
| **The Understudies** (absurd) | A stage magician's every performance leaves a cardboard cutout that keeps performing; the Heart is the box office; enemies are critics and hecklers | PvZ energy, instantly memorable, explains echoes diegetically ("the understudies") | Comedic art is harder to generate consistently |
| **Brushstrokes** (abstract) | You are a stroke of paint; every stroke you paint stays and keeps painting; enemies are ink blots | Cheapest art, strongest identity, every effect is the art | Harder to market to the Thronefall audience; risk of feeling like a tech demo |
| **The Last Convoy** (the dropped direction as a skin) | Echoes run in convoy space on the roofs of a moving caravan; the world scrolls | Keeps the caravan the owner loved | Doubles the prototype's complexity; only after the arena version works |

### 4.10 Reuse from faith-defense

- Core patterns: fixed tick + interpolation, command queue, SplitMix64 streams, state hash, JSON config with fingerprints, budget wave generator, the balance bot idea (`src/FaithDefense.Core/`). The recording of an echo is a list of commands, which the command-queue design already models.
- Spawn gates opening over rounds (synthesis F2).
- Process: spec → plan → small PRs → fresh-context reviewer → journal entry, and the "frozen rules until the owner plays" discipline of the camp spec.
- Art: the PixelLab prompt guide (style, palettes, lessons) and `crew.py`'s motion-extraction trick for cheap animation. Tiles and enemies only if the game stays isometric.

### 4.11 Prior art and verdict

| Game | Year | What it does | Difference |
|---|---|---|---|
| Time Rifters (Proton Studio) | 2014 | FPS; 4 runs through an arena, each joined by ghosts of the previous runs | FPS, puzzle-like planning, no hero progression, no base to defend |
| Super Time Force (Capybara) | 2014 | Platformer shooter; rewind and fight alongside past attempts | Platformer, time-rewind, deterministic replays |
| Past Me, Assemble! (itch) | jam | Survival shooter; every 10 s a past self joins, replaying movement and attacks | Jam scope; clones inside one run; no drafting, no defence object |
| ChronoClone (itch) | jam | Failed attempts fight alongside you on retry | Jam scope; clones from deaths |
| Temporal Titans (Lost Tower Games) | 2026 | TD with mechs; send a mech back to a previous wave to duplicate it | Units, not the player; replaying waves, not recording a hero |

**Verdict:** the trope "past selves fight beside you" exists, mostly in 2014 and in jam games. No visible commercial game combines a survivor.io-style hero, an object to defend, roguelite drafting, and echoes whose path *is* the tower placement and whose upgrades stay with them. The jam games are useful evidence: the fantasy lands immediately, and the prototype is cheap.

Kingshot (Century Games, 2025), which the owner mentioned as "a TD where you control a hero against ever larger hordes", turned out to be a mobile city builder whose hero-versus-hordes gameplay exists only in its ads (PocketGamer: "Kingshot's not-tower-defence ads"). That is a validated fantasy with no real game behind it, and an argument for a hero who visibly outgrows the horde.

### 4.12 Risks and mitigations

| Risk | Mitigation |
|---|---|
| The mechanic is not understood in the first two rounds | The rewind moment at round start; the first echo appears in round two with a one-line caption; playtest question below |
| Echoes patrol routes the enemies no longer use | Stable spawn gates; chorus cards; re-record as a later feature if the playtest shows it is a problem rather than the strategy |
| Ten ghosts plus a hero plus a horde become mush | Ghost shader (desaturated, transparent), one bright hero, hard cap, fixed internal resolution |
| A run feels samey because every round is the same arena | Gates opening over rounds, a brute from round four, bosses later; a second arena only after the gate |
| MonoGame ships no juice | Particle, flash, hit-stop, shake, trail and sound systems are the first week's work alongside the echo |
| Comparison to Time Rifters | Different genre, different platform, different progression; mention it openly on the store page rather than hide it |

### 4.13 Week-1 prototype and pass/fail question

**Build:** one arena with placeholder shapes but real juice systems; hero with move, auto-attack and dash; the Heart; two enemy types with budget waves; a 75 s round timer; echo recording and replay; three cards (damage, attack speed, dash cooldown); the rewind moment; placeholder sound.

**Pass/fail question:** after round five, does the player (a) say something like "that is my army" unprompted, (b) plan a route deliberately rather than kiting at random, and (c) start a second run without being asked? With five testers, three of each is a pass. A fail means the echo is a gimmick and the next proposal gets its week.

### 4.14 Open questions for interview round 4

1. What ends a round: timer only, or timer and hero death as above?
2. What persists between runs: unlocks only, or some power?
3. Setting and tone: one of the four above, or decide after the prototype?
4. Perspective: top-down (simplest for a hero and effects) or isometric (reuses the faith-defense tiles)?
5. One arena with gates, or several small arenas per run?
6. Prototype budget: one week, two?
7. Platform: PC/Steam confirmed? Controller support from day one?

## 5. Alternatives

Each kept short, with the prior-art verdict that decided its fate.

### 5.1 Herder (Shepherd of the Damned)

- **Mechanic:** the hero deals no damage. The hero pulls (hold) and flings (release) crowds into the big dumb AoE zones that towers make. Upgrades mutate the pull: wider cone, a vortex that orbits enemies around you, chain pull, a stampede where flung enemies damage each other.
- **Why it fits:** the crowd itself is the effect: piling, swirling, flying, exploding in clumps. Combat is physical. One verb.
- **Loop:** waves of 60–90 s; one tower placed or upgraded between waves.
- **Reuse:** the AoE tower family (cauldron, bell, bombard) and flow fields from faith-defense.
- **Prior art:** Magnetic Rumble (itch: you are a magnet, dash into enemies, place towers), Tower Lab (physics TD roguelike where towers push, pull and launch enemies off the map), The Legend of Gravity Hero (itch). **Verdict:** exists in small form, not at commercial scale. Open.
- **Risks:** physics feel in an integer simulation must be snappy; isometric pixel piles may read as mush; relies on the fling feeling good from day one.
- **Prototype:** one arena, pull and fling, one cauldron, 30 enemies. Question: is dragging twenty enemies into a cauldron fun for ten minutes?
- **Status:** best second candidate. Also a possible active skill for the Echo Loop hero (echoes replaying your pulls).

### 5.2 Rolling Fortress (Ironcaravan)

- **Mechanic:** the base is a convoy that never stops. Each wagon is a build slot in a line and order matters: the front wagon rams, the rear is chased, the middle is safe for support. Boarders climb on from the sides. The hero runs on the roofs. Routes fork at stations.
- **Why it fit:** builds on the caravan the owner loved and gives slots a reason.
- **Prior art:** Bearicade (Steam, Q2 2026, Gut Punch Studio: towers bolted onto a toy train's wagons, twelve wagon colours that modify what rides on them, drafting between waves, thirty waves plus endless), Land Trains (itch: TD + snake + survivors + roguelite on train cars), Choo Choo Defenders (Steam demo), Tracks n' Turrets, Loopstructor, Voidtrain, Last Train Home. **Verdict:** the wagon-slot core is done. Only the hero on the roof and boarding would be new, which is not enough for a core.
- **Status:** dropped as a direction (round 3). Survives as a setting option for Echo Loop (§4.9).

### 5.3 Lantern Relay (Nightwatch)

- **Mechanic:** towers only fire while lit. The hero carries the flame and runs relays, lighting towers in the order the wave demands; a lit tower burns out after a while. Upgrades: longer burn, chain lighting, a burning trail, lanterns that relight neighbours.
- **Why it fits:** the planned "light and grade" look of faith-defense (synthesis V6) becomes the mechanic; where you run is where the fight is.
- **Prior art:** Everwarder (a mote of light with free movement protecting a crystal in a roguelite TD), Twilight Tower Defense (gun towers fire only at lit zombies), Darkest Defense, Of Shadow and Light. **Verdict:** Everwarder is close enough that this would read as a follow-up.
- **Risks:** running laps becomes a chore; tower count must stay at 5–7.
- **Status:** demoted. The "light as resource" idea could still be one Echo Loop chorus card.

### 5.4 Possession Hop (Saint in the Machine)

- **Mechanic:** no hero body. You are a spirit possessing one tower at a time: the possessed tower is manually aimed and overcharged, the rest fire weakly on auto. Hopping is instant along lines between towers you built. Building is placing new bodies for yourself.
- **Prior art:** Soul Defender (possess towers and shoot them yourself, level design forces constant movement between them), Man the Towers, Attack of the Place Holder Cubes, Manual Tower Defense (itch), plus Sanctum, Dungeon Defenders and Orcs Must Die! as the commercial ancestors. **Verdict:** done many times; the hop-along-lines twist is thin.
- **Status:** dropped.

### 5.5 The Swarm (Hive)

- **Mechanic:** you are not one hero but a flock. Steer it; hold a button and draw a formation (wall, spear, ring, spiral) and the flock takes it. Towers are hives that spawn more flock. Upgrades change the flock: split on kill, sticky, burning.
- **Why it fits:** hundreds of cheap sprites moving as one organism; mass motion for the price of one sprite.
- **Prior art:** academic and jam projects only (a Chalmers thesis TD built around flocking, Beautiful Boids for VR, swarm-herding research at Würzburg). **Verdict:** open.
- **Risks:** readability; a flocking simulation in an integer Core; "draw a formation" may be a gimmick; conflicts with the decision that the player controls a single hero.
- **Status:** parked. Worth a jam-sized experiment some day, not this project.

### 5.6 Harvest (Garden of War)

- **Mechanic:** towers are plants that grow across waves. Harvest now for one giant burst and replant, or let it grow for a stronger passive. The hero is the gardener: water to speed growth, scythe to harvest, carry seeds.
- **Prior art:** Chloroblast (plant seeds, spend pollen, harvest for gold), Gnome More Shrooms (grow crops and feed them to towers), Bloom & Doom (gardener and a mother flower), Gardener's Luck (2026, roguelike TD with drawn plants), Plants vs. Zombies. **Verdict:** crowded.
- **Risks:** cosy rather than "inside the fight", which is the opposite of the brief.
- **Status:** dropped.

## 6. Combinations (to note, not to build first)

- **Echo Loop + Herder:** the hero's active skill is a pull; echoes replay your pulls, so past selves herd for the present one. The most promising combination, and a natural second skill once the echo gate passes.
- **Echo Loop + Convoy:** echoes in convoy space (§4.9). Only after an arena version works.
- **Echo Loop + Lantern:** a chorus card that lights echoes and makes them stronger near the Heart. A card, not a system.

The faith-defense lesson applies: no combination before the single verb passes its gate.

## 7. Comparison matrix

Scores 1–3 against the criteria in §2 (3 is best). Prior-art openness and prototype cost included.

| Proposal | One verb | Short rounds | Inside the fight | Mix potential | Setting freedom | Reuse | Prior-art open | Prototype cheap | Total |
|---|---|---|---|---|---|---|---|---|---|
| **Echo Loop** | 3 | 3 | 3 | 3 | 3 | 2 | 2 | 3 | **22** |
| Herder | 3 | 2 | 3 | 2 | 3 | 2 | 2 | 2 | 19 |
| Rolling Fortress | 2 | 2 | 2 | 3 | 2 | 3 | 1 | 1 | 16 |
| Lantern Relay | 2 | 2 | 2 | 2 | 2 | 2 | 1 | 3 | 16 |
| Possession Hop | 2 | 2 | 3 | 2 | 2 | 2 | 1 | 2 | 16 |
| The Swarm | 2 | 2 | 2 | 2 | 3 | 1 | 3 | 1 | 16 |
| Harvest | 2 | 1 | 1 | 2 | 2 | 2 | 1 | 2 | 13 |

Reading the matrix: Echo Loop and Herder are the only two that score 3 on "one verb" and "inside the fight" at once. Echo Loop wins on rounds, mix potential and prototype cost. Herder's weakness is the physics-feel bet.

## 8. Stack and art pipeline

### 8.1 MonoGame (decided in round 3)

- **For:** C#/.NET matches the owner (a C# Core and a .NET journal in faith-defense). Code instead of an editor matches how the owner worked in Unity (`ProjectSetup.cs`, 1,435 lines of "setup as code"). Fixed timestep is built in. Full control of pixel-art rendering: SpriteBatch, point sampling, a render target at a fixed internal resolution for pixel-perfect scaling. HLSL shaders. Steamworks.NET for Steam. Windows, Mac and Linux through DesktopGL. The faith-defense Core moves over almost verbatim.
- **Against:** no editor, no scenes, no particles, no UI, no tweens, no tilemap. Either write them (small, for pixel art) or evaluate libraries at prototype time: MonoGame.Extended, Nez, Myra for UI. The MGCB content pipeline is tedious; loading PNG and WAV directly at runtime is fine for a prototype. Web is unofficial: the KNI fork targets Blazor WebAssembly with gaps (no gamepad, incomplete touch), so a playtest is a zip for friends, not a link. Current release line at the time of writing: 3.8.5 preview (April 2026).
- **Condition:** juice is a first-class system from day one, because nobody ships it for you. In faith-defense its absence was the complaint; in MonoGame its absence would be the default.

### 8.2 Art

- **PixelLab** is the validated path: the prompt guide, the palettes and the lessons transfer whatever the perspective. If the game goes top-down, the isometric tiles and the SE/NE walk sheets do not transfer; the method does.
- **ludo.ai** was never used in faith-defense. It is worth one afternoon of comparison on the same prompt (a hero with a four-direction walk and a ghost variant) before committing, not more.
- **Ghost rendering** needs no new art: tint, alpha and desaturation in a shader over the same sprite.

## 9. Process guardrails

Carried over from the faith-defense post-mortem, so the new project does not repeat the old one.

1. One verb first. No second system before the §4.13 gate.
2. A playable toy within days. Placeholder art, real juice.
3. Written hypotheses with a pass/fail question before each gate, judged in writing after the playtest, as the camp spec did.
4. Playtesters who are not the owner, from the first week.
5. Setting and art direction are chosen after the mechanic passes, not before.
6. Small PRs, a fresh-context reviewer, a journal entry per lesson, as before.

## 10. Sources

Prior-art facts come from web search summaries on 2026-10-07; Steam and itch.io pages were not reachable from this session, so store-page details (dates, prices, review counts) should be re-checked before anything is quoted publicly.

- grill-me skill: https://github.com/mattpocock/skills (skills/productivity/grill-me and grilling)
- Kingshot ads vs game: https://www.pocketgamer.com/kingshot/hands-on/
- Time Rifters: https://kotaku.com/colorful-indie-shooter-lets-you-play-with-yourself-1491514347
- Temporal Titans: https://steambase.io/games/temporal-titans/info
- Past Me, Assemble!: https://dhiraj007.itch.io/past-me-assemble
- ChronoClone: https://train-enthusiasts.itch.io/chrono-clone
- Bearicade: https://store.steampowered.com/app/4876850/Bearicade/
- Land Trains: https://bashamer.itch.io/land-trains
- Choo Choo Defenders: https://store.steampowered.com/app/4692540/Choo_Choo_Defenders_Demo/
- Magnetic Rumble: https://kinda-good-games.itch.io/magnetic-rumble
- Tower Lab: https://devonpowell.itch.io/tower-lab
- Everwarder: https://www.pcgamesn.com/everwarder/release
- Twilight Tower Defense: https://prettyflygames.itch.io/twilight-tower-defense
- Soul Defender: https://www.moddb.com/news/introduction-and-prototype-phase-1
- Man the Towers: https://chihelios.itch.io/man-the-towers
- Flocking TD thesis (Chalmers): https://odr.chalmers.se/handle/20.500.12380/301968
- Chloroblast: https://phibian.itch.io/chloroblast
- Gnome More Shrooms: https://www.therookies.co/entries/50955
- MonoGame releases: https://www.nuget.org/profiles/MonoGame
- KNI (MonoGame fork with a web target): https://docs.wavedash.com/engines/kni.md
