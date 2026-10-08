# The first twenty nights

- **Status:** written 2026-10-08 at the owner's request: a plan for a player's first twenty performances ("nights"), easy enough at the start that the player learns the game and its rules by playing, and varied enough after that the game is never only "defend the middle". The owner named waves (how many, what kind), light that shows only part of the stage, more doors and doors that open and close, critics that behave unconventionally, and asked for more. Descriptive; no rule here is agreed and nothing is a ticket until the owner names it (plan §2). No code.
- **Agreed in conversation:** a night is one performance; nights 1 to 5 unlock by being played (a lost night counts, so a beginner never stalls), from night 6 the next unlocks by reaching act five or winning, from night 16 by winning only; night 1 has five acts, nights 2 and 3 seven, from night 4 ten; the details of a night (doors, crowds, house rules) are drawn from the performance's seed, never authored by hand.
- **What it builds on:** `main` at c1e81a6: one stage, three doors opening in acts 1, 3 and 6, three enemy kinds (the critic, the stagehand, and the rival's understudy of T29, which grows tougher by the act), the encore of decision 26, no applause within `applauseBoxOfficeRadius` of the box office by decision 27, the honest guard of T37 with its kiter, the three cards of T25 under the catalogue's names (decision 29), and the screens of decision 30 (a main menu, the HUD, the encore, the program: `docs/design/2026-10-08-screens/`). The cast, the synergies and the card catalogue are in `docs/design/`. The Balatro borrowing the owner parked, a house rule per act, returns here as house rules per night.
- **What does not exist yet and this plan needs:** memory between performances (the vision says unlocks only; plan §8 has it out of scope); the poster, a page of its own between the main menu's "Perform" and the curtain (the menu's small line "Tonight, at the street corner" is its first words); per-night overrides of `tuning.json`; light; and every house rule. The menu's "Matinee or gala" (a choice of difficulty, out by §8) is what the ladder replaces for the first twenty nights: the night's number sets the difficulty, and the gala conditions come after night 20. §6 names the tickets.
- **A finding this plan answers:** T29 looked for numbers on which the late acts threaten a winning player's box office and did not find them; what threatens the player who takes "one more card" first fells every other player in one act. So the ladder's difficulty cannot come from the budget alone, and does not: it comes from what the stage does (light, doors, objectives) as much as from how many come.

## 0. Seven principles

1. **One new thing a night, named on the poster** before the curtain. The player is surprised by a situation, never by a rule.
2. **The first ovation inside seven minutes.** Night 1 is five acts, two doors, six tenths of the budget: a player who moves at all wins it.
3. **A twist changes what comes and what can be seen; rarely where things are.** An understudy replays a route. A change of geometry in the middle of an act makes every old route wrong, which is the one thing the mechanic cannot afford. Geometry changes only at a curtain, and only when the poster said so.
4. **The box office is not always the whole objective.** One family of twists changes what an act is for.
5. **Everything is drawn from a pool, and the pool grows with the night.** The draw is from the performance's seed, before the first curtain, never during an act.
6. **The guard measures night 10 on the plain tuning**, as today; every twist gets its own measurement in the instrument (`PrintTheVariants` with the night's overlay); and nights 1 to 3 carry the opposite assertion: any player that moves (the kiter, the doors player) wins them, and the orbit still loses.
7. **Every twist has the same card:** its poster name, its rule in one sentence, what it shows, what it teaches, what it needs in Core or the view, a *routes* flag (does it make old routes wrong) and a *guard* flag.

## 1. The levers

### 1.1 Waves, how many

| Twist | Rule | Shows | Teaches | Needs | Routes | Guard |
|---|---|---|---|---|---|---|
| The budget scale | the night multiplies `firstActBudget` and `budgetGrowthPerAct` by `nightBudgetScale` (0.6 on night 1, 1.0 on night 10, 1.4 on night 20) | more or fewer at the doors | nothing by itself | an overlay key | no | measured per night |
| Crowds | `waveBurstShare` and `waveBurstTime` from the overlay: no crowds before night 4, thicker crowds from night 15 | the lamp over a door flickers before a crowd (view) | that a door is about to spill | exists (T26); the lamp | no | none |
| Short acts, long acts | `actLength` 60 or 90 instead of 75 for the night | the clock on the back wall | pace | an overlay key; recordings are per act, so a night's length is the same for every act of it | no | none |
| The double act | two acts in a row with no program between; the understudy of the first appears only after the second | the back wall says "Acts 6 and 7" | holding out without a new card | a phase rule: `GoOn` without the program | no | watch |

### 1.2 Waves, what kind

| Twist | Rule | Shows | Teaches | Needs | Routes | Guard |
|---|---|---|---|---|---|---|
| Kinds by night | a kind enters the pool on its night (§2) and stays | its sprite | its question (cast) | the cast's tickets | no | each kind's own |
| Themed acts | an act drawn as "the critics' night" (critics only) or "the stagehands' night" (runners only) | the back wall names it | that a kind alone asks a different route | the planner reads a per-act allowed set | no | none |
| The editor | one critic of an act is the editor: three times the life, a hat, and three pieces of applause where it falls | the hat, a slower walk | focus | an elite flag on an entry | no | none |
| The quote | a row of six critics enters one door in a line (the Reviewer's trick, as an ordinary entry from night 12) | a line | the burst, the fan | the row entry (the Reviewer's) | no | none |

### 1.3 Doors

| Twist | Rule | Shows | Teaches | Needs | Routes | Guard |
|---|---|---|---|---|---|---|
| Four doors, five doors | the stage's door list grows with its stage (§2) | the doors | covering more than three lanes | stage files | no | measured |
| The seeded order | which door opens in which act is drawn from the seed, not fixed, from night 7 | the poster lists the order | reading the poster | the plan takes the order from `RngStream.House` | no | none |
| The timetable | a door closes and another opens on a timetable drawn from the seed, announced three seconds ahead by the lamp over it, from night 12; a door never changes in an act's first ten seconds | the lamp blinks; a shut door dims | re-reading the stage in the act | `DoorIsOpen` by tick, not by act; the planner routes entries to doors open at their tick | **yes, soft**: an understudy at a shut door guards nothing for a while; the poster says which doors are on the timetable | watch |
| The moving door | a door slides along its wall between two acts, by up to a third of the wall, said on the poster and shown at the curtain, from night 14 | the door slides during the curtain | that a route can be made for a door that is not there yet | door positions per act; the entry's spot follows | **yes, hard**: one change a night, at a curtain, on the poster only | watch |
| The front row | critics stand up from the audience and come over the footlights: the whole front edge is a door, from night 16 | figures rising from below the footlights | the stage has four sides | a door as a segment, not a point | no | measured |

### 1.4 Light

The stage's light is the one lever the owner named that the prototype has nothing of. The rule of every light twist: **what is dark is not drawn, and is still there.** Critics in the dark walk, strike and are struck; the understudies throw at them on their rails as ever; and every card's flick and burst is drawn whatever the light, so the past selves' cards are what reveals the dark. That is the whole point of putting light in a game about ghosts: the player's old routes see for the player.

| Twist | Rule | Shows | Teaches | Needs | Routes | Guard |
|---|---|---|---|---|---|---|
| The spotlight night | only a circle of `spotlightRadius` (8) round the magician and a cone at every open door are lit; everything else is drawn as the empty floor | a dark stage, two kinds of light, cards flashing in the black | that the understudies' cards are a map; that the doors are where to look | the view only: a mask over the stage; Core unchanged | no | none (the doors player plays blind the same) |
| The follow-spot | one spotlight wanders the stage on a path from the seed, lighting a circle of ten; the magician has no light of its own | a moving pool of light | moving with the light, or trusting the ghosts | the view, and a seeded path | no | none |
| The blackout | the lights fail for `blackoutTime` (10 s) on a timetable from the seed, three or four times a night, announced by a flicker two seconds before; cards, applause and the box office's bar stay lit | black, then the cards | holding a route by memory | the view, a seeded timetable | no | none |
| Footlights only | a strip of `footlightDepth` (7) along the front edge is lit; the back is dark | a bright front, a dark back; critics appear as they come forward | that the back doors are the blind ones | the view | no | none |
| The curtain half down | the back third of the stage is behind a half-lowered curtain: not drawn, and critics there are not drawn, from night 11 | the curtain's hem | playing a stage you cannot see all of | the view; the understudies' routes behind the hem are drawn as a dotted line | no | none |

### 1.5 Critics that behave unconventionally

The cast document has the scalper, the rival's understudy, the heckler, the prompter, the drunk patron, the intern, the Diva and the Reviewer. These are behaviours rather than kinds: each is a flag a kind can carry, so a night can give it to critics without a new sprite.

| Behaviour | Rule | Shows | Teaches | Needs | Routes | Guard |
|---|---|---|---|---|---|---|
| The flanker | walks along its wall and comes at the box office from the side opposite its door | a figure hugging the wall | that a lane is not a straight line | a two-leg path per entry | no | none |
| The coward | stays out of the magician's throw range; walks at the box office only while the magician is further than `cowardDistance` (14) from it | a figure that backs off | that leaving the box office has a cost | a rule in the walk that reads the magician's distance | no | **flagged**: it rewards staying near the box office; in with the kiter measured |
| The sleeper | sits down in the front row on entry and sleeps; when `sleeperCount` (6) are sitting they wake together and walk | figures seated along the footlights | the burst, timing | a seated state and a shared wake | no | none |
| The climber | enters over the back wall at a point from the seed, not by a door | a figure coming over the wall | that doors are not everything | an entry with no door | no | watch |
| The autograph hunter | walks at the magician, not the box office, wherever the magician is, and hurts on touch; it never strikes the box office | a figure with a pen and a book, running | moving; the first enemy that hunts the player | a target rule | no | helps: it finds a player who stands still |
| The photographer | when within four of the magician its flash whites the screen for half a second (view) and it is spent | a flashbulb | looking away from the lamp | a view effect on an event | no | none |
| The pickpocket | a touch takes two pieces off the encore's count | a hand in a pocket, two pieces flying off | guarding the count | a rule on the touch | no | none |
| The chain | critics that entered together hold hands and walk as one; when the leader falls the rest scatter and run | figures linked | the leader | a group entry with a leader | no | none |
| The costume | a critic dressed as an understudy: drawn with the ghost's wash, told apart by its walk and its kill; from night 17 | a ghost that walks wrong | reading the stage closely | a view flag, and the kind's own tint | no | none |

### 1.6 The objective

The owner's complaint is that the game is only "defend the middle". These twists change what an act is for. Each keeps the box office's fall as the one way to lose; what changes is what else costs.

| Twist | Rule | Shows | Teaches | Needs | Routes | Guard |
|---|---|---|---|---|---|---|
| The box office on wheels | a stagehand that reaches the box office pushes it three units toward its own door; the quiet floor moves with it; said on the poster | the box office rolling, a squeak | that the defence drifts from the old routes, and why runners matter | the box office's position as state (in the hash); the radius follows | **yes, soft**: routes near the box office drift; the doors do not | watch |
| Two box offices | a matinee box office and an evening one, on opposite sides; each has half the life; losing either closes the show | two box offices | two fronts | a list of box offices | no | measured |
| The front row's night | critics that reach the footlights cost the house (a mood bar) instead of striking anything; the house at zero closes the show | a mood bar beside the box office's | that the stage has an audience | the house as state | no | measured |
| The dressing-room cart | an act in which a cart crosses the stage from one wing to the other at walking pace; critics strike it; it must arrive | a cart with a star's name on it | escort | a moving object with life | no | measured |
| The applause quota | an act in which the house asks for `quotaPieces` (6) pieces by the act's end, or the mood drops a step | a target on the applause bar | going out on purpose | a per-act count the view reads | no | helps |
| The intermission | an act with no critics: pieces of applause scattered over the stage, two encores to be had, the understudies idle | a lit, empty stage with applause on it | the shop | a planner that enters nobody | no | none |
| Two fronts | the Diva and the Reviewer in the same night, acts 5 and 10, as the finale from night 15 | | everything | the bosses' tickets | no | measured |

### 1.7 The stage

| Twist | Rule | Shows | Teaches | Needs | Routes | Guard |
|---|---|---|---|---|---|---|
| The trapdoor | opens on a cycle from the seed and swallows whatever stands on it; critics and the magician alike (the magician loses two life and lands on its mark) | a square that drops | the cycle | a hazard with a timer | no | none |
| The revolving stage | at the curtain of act 6 the floor turns a quarter turn: every recorded route turns with it, the box office turns, the doors stay; said on the poster, shown in the curtain | the floor turning under the ghosts | that the defence can be turned to a new door | a rotation applied to recordings and the box office at one `GoOn` | **yes, hard**: once a night, at a curtain, on the poster only | watch |
| The mirror night | the stage is mirrored from the first curtain: doors, box office, mark; routes replay mirrored because they are recorded mirrored | the familiar stage the wrong way round | the stage anew, for a returning player | a flip of the stage file at load | no (it is the stage) | none |
| The pit | the orchestra pit along the front: anything pushed in is gone | a dark pit | the push cards | a hazard that removes | no | none |
| The chandelier | at a tick from the seed a chandelier falls where the most critics stand, with a warning shadow two seconds before; it hurts the magician too | the shadow, the crash | reading a warning | a timed area hit | no | none |
| Hot footlights | the strip along the front hurts critics that cross it (not the magician) | glowing lamps | using the edge | a damage strip | no | none |

### 1.8 The understudies' night

Twists on the mechanic itself. Late in the ladder, because they only mean something to a player who has learnt what an understudy is.

| Twist | Rule | Shows | Teaches | Needs | Routes | Guard |
|---|---|---|---|---|---|---|
| The rehearsal | understudies replay at double speed and finish their routes by the act's middle, then stand | fast ghosts, then still ones | that a route's length is a choice | a replay rate | no | watch |
| The strike | one understudy, drawn from the seed each act, sits the act out | a ghost on a chair | depending on no single ghost | a skip | no | none |
| The ghost light | understudies are not drawn; their cards and clouds are | cards from nowhere | reading the stage by its effects | the view | no | none |
| The ovation | the newest understudy carries the magician's current cards for the night (the catalogue's "Learns the Part", as a house rule) | a brighter ghost | | the catalogue's rule | no | none |

## 2. The ladder

Four tiers of five nights. A night's column "new" is the one thing its poster leads with; "rules" is how many house rules are drawn and from which pool (§3). The budget scale is a hypothesis, to be found with the instrument (§6, N4).

| Night | Acts | Stage | New | Doors | Light | Rules | Objective | Teaches | Gate | Scale | Poster |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 5 | street corner | critics only | 2 (the third shut) | full | none | the box office | move, throw, pick up; the first understudy at act 2 | played | 0.6 | "A quiet Tuesday" |
| 2 | 7 | street corner | the stagehand (act 4) | 3 | full | none | the box office | the side door | played | 0.7 | "The crew arrives" |
| 3 | 7 | street corner | the scalper (act 3); the quiet floor named | 3 | full | none | the box office | applause is fetched, not waited for | played | 0.75 | "Touts outside" |
| 4 | 10 | street corner | the rival's understudy (act 4); the Diva (act 5) | 3 | full | none | the box office | focus; the first boss | played | 0.85 | "A rival in town" |
| 5 | 10 | street corner | the first house rule | 3 | per rule | 1 gentle | the box office | reading the poster | played; unlocks the variety theatre | 0.9 | "House rules" |
| 6 | 10 | variety theatre | the heckler (act 5) | 4 | full | 1 gentle | the box office | standing still is punished | act 5 or a win on night 5 | 0.9 | "A new house" |
| 7 | 10 | variety theatre | the seeded door order; the prompter (act 6) | 4 | full | 1 gentle | the box office | the doors are where the support stands | act 5 or a win | 0.95 | "Doors in any order" |
| 8 | 10 | variety theatre | the spotlight night; the drunk patron (act 7) | 4 | spotlight | 1 gentle + the light | the box office | the ghosts see for you | act 5 or a win | 0.95 | "Lights down" |
| 9 | 10 | variety theatre | the autograph hunter (act 5) | 4 | per rules | 2 (gentle, sharp) | the box office | being hunted | act 5 or a win | 1.0 | "Fans" |
| 10 | 10 | variety theatre | the Reviewer (act 10) | 4 | per rules | 2 | the box office | the finale | act 5 or a win; unlocks the opera house | 1.0 | "Opening night" |
| 11 | 10 | opera house | balconies, the pit; the intern (act 8) | 4 + balconies | per rules | 2 | the box office | area damage; the pit | act 5 or a win | 1.05 | "The grand house" |
| 12 | 10 | opera house | the timetable of doors; the coward; the quote | 5 | per rules | 2 | the box office | re-reading the stage | act 5 or a win | 1.05 | "Doors come and go" |
| 13 | 10 | opera house | the box office on wheels; the flanker | 5 | per rules | 2 | the box office moves | the defence drifts | act 5 or a win | 1.1 | "On wheels" |
| 14 | 10 | opera house | the revolving stage (act 6); the photographer | 5 (one moves) | per rules | 2 | the box office | the defence turned | act 5 or a win | 1.1 | "The turn" |
| 15 | 10 | opera house | blackouts; the Diva and the Reviewer | 5 | blackouts | 2 | two fronts | everything so far | act 5 or a win; from here, wins only | 1.15 | "Both of them" |
| 16 | 10 | the gala (any stage) | the player picks one of three rules for score; the front row as a door | 5 + the front row | per rules | 1 chosen + 1 drawn | the front row's night | choosing a handicap | a win | 1.2 | "Your choice" |
| 17 | 10 | the gala | the mirror night; the chain; the costume | mirrored | per rules | 1 chosen + 2 drawn | the box office | the stage anew | a win | 1.2 | "Through the glass" |
| 18 | 10 | the gala | the understudies' night; the sleeper | 5 | per rules | 1 chosen + 2 drawn (one from §1.8) | the box office | the mechanic bent | a win | 1.25 | "The understudies' night" |
| 19 | 10 | the gala | two box offices; the pickpocket; crowds × 1.5 | 5 | per rules | 1 chosen + 2 drawn | two box offices | two fronts, for real | a win | 1.3 | "Matinee and evening" |
| 20 | 10 | the gala | everything in the pool; the editor in every act | 5 + the front row | per rules | 1 chosen + 3 drawn | drawn | the exam | a win | 1.4 | "The critics' ball" |

After night 20 the pool is full. A night draws two or three rules and one objective twist, the scale stays at 1.4, and the ladder becomes the score ladder of the vision's gala conditions: the player picks handicaps for a higher score, as Thronefall's perks and challenges.

What each tier is for: nights 1 to 5 teach the loop and the applause rule with nothing else in the way; 6 to 10 teach that the stage changes (the twist); 11 to 15 turn the mechanic itself (the defence drifts, turns, goes dark); 16 to 20 hand the player the dials.

## 3. Pools and compatibility

Three pools. A rule is drawn from the pool its tier allows, without replacement within a night.

- **Gentle:** the lamp before a crowd, themed acts, footlights only, the trapdoor, the intermission, the applause quota, the editor.
- **Sharp:** the spotlight night, the follow-spot, the timetable of doors, the flanker, the coward, the sleeper, the chain, the photographer, the pickpocket, the chandelier, hot footlights, short acts, the double act, the dressing-room cart, the quote.
- **Cruel:** the blackout, the curtain half down, the moving door, the box office on wheels, two box offices, the front row as a door, the climber, the costume, long acts, the revolving stage, the mirror night, and all of §1.8.

Compatibility, checked at the draw: the spotlight night and the blackout never together; the revolving stage and the box office on wheels never together; the moving door and the revolving stage never together; at most one change of geometry a night (the revolving stage, the box office on wheels, the moving door); the ghost light never with the spotlight night; the rehearsal never with the double act. A draw that fails is drawn again from the same stream.

## 4. The poster

Every night opens on a poster: the page between "Perform" on the main menu of decision 30 and the curtain, in the screens' own paper and ink. It says, in this order: the house (the stage), the number of acts, the doors, the light, each house rule with its icon and its one sentence, the new thing of the night in a bigger face, and what unlocks the next night. "Tonight at the Variety. Ten acts. Four doors, in any order. The spotlight night: you see what the lamps show. The heckler: he throws from the aisles. Reach act five for tomorrow's poster." A press raises the curtain. The poster is also where R goes after a show closes, and the one place the game explains a rule in words. Within a night, the program's "next act" panel (the screens' §4) carries the same duty act by act: which door opens, which kind comes, what the encore costs now.

The poster is drawn from the night's overlay (§6, N2) and the seed's draws, so it is always true for the show that follows.

## 5. Teaching without a tutorial

The prototype has two captions in the whole game (the first understudy, the first encore) and the plan wants no more. The ladder teaches by what it withholds.

- **Night 1** withholds the third door, the stagehand, the crowds and every rule: the player learns to move, that cards throw themselves, that applause lies where they kill and is theirs alone, and sees one understudy repeat act one in act two. Five acts end in an ovation for anybody who moves.
- **Night 2** adds the runner: the player learns that a lane left alone leaks.
- **Night 3** adds the tout and names the quiet floor on the poster: the player learns that applause must be fetched, and where it is never dropped.
- **Night 4** adds the brute and the Diva: the player learns to focus and to time pick-ups.
- **Night 5** adds the first rule from the gentle pool: the player learns to read the poster.
- From night 6 the new thing of each night is on the poster, and the player is expected to read.

## 6. What it needs

Tickets, each one pull request in the plan's shape; the plan numbers them when they are taken (the road's rule).

| Ticket | What | Needs | Tests |
|---|---|---|---|
| **N1 The nights' memory and the poster** | a progress file (nights played, the best act of each, wins), never in the state hash; the poster as the page after the main menu's "Perform" (decision 30's screens, after their own tickets), drawn from the night's overlay and the seed; R returns to the poster; "Perform the same seed" replays the night as it was | a file the view reads and writes; the poster screen in the screens' paper and ink | the file round-trips; a lost night counts as played; the gate rules of §2 (played; act five or a win; a win) |
| **N2 The nights' overlays** | `nights.json` beside `tuning.json`: twenty entries, each the keys of `tuning.json` it overrides (`actsInPerformance`, the budget scale, the doors, the kinds allowed, the pools allowed and how many rules), parsed as strictly as `Tuning`; `Simulation` is given the composed tuning and knows nothing of nights | the overlay parser; a composition step | an unknown key refused; the composed tuning of night 1 has five acts and two doors; night 10's composed tuning is `tuning.json` itself |
| **N3 House rules as named rules drawn from pools** | a rule is a name in Core (or in the view, for light) with a flag in the composed tuning; the draw is from `RngStream.House` before the first curtain, with the compatibility of §3; each rule is its own ticket after this one. The first three: the spotlight night (view), the timetable of doors (Core), the box office on wheels (Core, geometry on the poster) | the stream; the draw; the flags | the draw is seeded and repeatable; incompatible pairs never drawn; each rule's own tests |
| **N4 Measuring a night** | `PrintTheVariants` takes a night's overlay as a variant; assertions as ordinary tests: night 1 is won by the kiter and the doors player 20 of 20 and the orbit loses by act five on at least 10 of 20; night 10 is the guard as it stands; night 20 is won by the doors player on at least 12 of 20 (a ceiling of difficulty, a hypothesis) | the instrument reads an overlay | the three assertions on the seeds 1 to 20 and 101 to 120 |

Dependencies: the kinds come from the cast's tickets (one new kind every two nights at most, since each needs a sprite and a walk); the rules of §1.8 need T32 and the catalogue's "Learns the Part"; the stages need the vision's variety theatre and opera house, which are stage files and art (T07d's recipe twice more); the bosses are the cast's.

## 7. Risks

- **Dead routes.** Every geometry twist (the timetable, the moving door, the box office on wheels, the revolving stage) makes some old routes wrong. Principle 3 bounds it: one a night, at a curtain, on the poster. If G1's playtest shows players hate their ghosts going stale, the cruel pool loses its geometry and keeps its light.
- **Reading the dark.** That the understudies' cards are enough of a map on the spotlight night is a hypothesis for a capture, not a rule. If it is not, the dark gets silhouettes.
- **Art before rules.** Every new kind and every stage is a sprite and a prompt; the ladder asks for ten kinds, three stages and two bosses. One kind every two nights is the pace the art can hold; the house rules are cheap by comparison, since most of them are a flag and a lamp.
- **A new kind of state.** The progress file is the first thing the game remembers between runs. It is never in the hash and never read by Core.
- **Scope.** Twenty nights are about five hours of play, which is the whole of the vision's v1. This document is the content plan of v1, not an addition to it.
- **The guard under twists.** A twist that makes the kiter win nights the doors player loses (the coward is the suspect) is a finding for the instrument, not a reason to keep the twist.
- **The budget is not the dial.** T29 showed that the late acts do not threaten a player who takes "one more card" first, at any budget that leaves the other players alive. The ladder's scale (0.6 to 1.4) sets how a night feels, not whether it can be won; whether it can is the twists' job, and night 20's assertion in N4 (the doors player wins at least 12 of 20) is the first measure of that.

## 8. Open questions for the owner

1. Does "the house" (a mood bar for the audience) come in with the first objective twist (the front row's night, the applause quota), or does it wait until a twist needs it on its own?
2. Are the mirror night and the revolving stage for every player at nights 14 and 17, or only for a returning one after night 20?
3. The names of the three houses, if the street corner, the variety theatre and the opera house are not to stay.
4. Night 16's choice of a rule for score: three drawn, pick one, or the vision's gala conditions list?
5. Whether a lost night 6 to 15 counts as "act five reached" when the box office fell in act five itself. The ladder says yes: reaching is reaching.
