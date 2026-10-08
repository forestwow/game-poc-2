# Enemies and cards: a descriptive design

- **Status:** a proposal, written 2026-10-08 at the owner's request, after the skeleton review of the same day (`docs/reviews/2026-10-08-skeleton-review.md`). Descriptive only: no rule here is agreed, no number is tuning, and nothing becomes a ticket until the owner names it (plan §2).
- **What it builds on:** `main` at a7098df (two enemy kinds, six cards as counts) and the branch `t23-encore` (decision 26: self cards taken in the act through encores paid with applause, one chorus card after every act; T24: an understudy takes its act's encores at the recorded ticks; T25: cards that change what a thrown card does). The vision's cast and card ideas (`docs/vision.md` §7–§8) are the seed.
- **What the owner asked for:** more upgrades, each visible, until the screen is "one great carnage" (plan §6), and a cast to match.

## 0. The six constraints every entry respects

1. **An understudy replays places and ticks, not inputs** (decision 6). So no upgrade may need aiming or a button other than the Vanish. Everything works through the auto-throw, the Vanish, the magician's movement, passives and pick-ups. An understudy with the same cards does the same thing on its rail.
2. **Nothing minds an understudy, and nothing hurts one.** An enemy that hunts understudies is a change of rule, not a new kind. It is not in this list.
3. **The guard** (decision 22). Every enemy and every card carries a guard flag: a card that pays for standing at the box office, or an enemy that walks into the pile by itself, is a suspect. Flagged entries go in only with the kiting player of review ticket R1 in the guard.
4. **One odd part per figure** (decision 25), and the tone of the vision: absurd, warm, a little mean.
5. **The smallest thing.** Every entry names what it needs: a field in `EnemyKind` or `Tuning`, a rule in `Simulation`, an event kind. The owner can see the cost before naming a ticket.
6. **Numbers are sketches relative to the critic**, never tuning. Tuning goes through the guard, as T20 did.

## 1. What exists today

| | Critic | Stagehand |
|---|---|---|
| Odd part | an eye under a bowler, a pen | a spotlight for a head |
| Walks | to the box office at speed 2; turns on the magician within 4 units and goes back when it leaves | to the box office at speed 8; never turns |
| Life | 3 (three cards) | 1 |
| From act | 1 | 4 |
| Asks | the baseline | do you watch the side door |

Six cards, all counts: sharper cards (+0.5 damage), quicker hands (throws more often), a longer arm (+1.5 range), a quicker Vanish, one more card (an extra card at the next nearest), and the chorus card (+0.5 damage for every understudy). T25 adds three that change what a card does: a card that goes on through the critic it strikes, one that turns to the next critic, and one that bursts where it strikes.

## 2. How an enemy is designed

- **It asks the player one question**, and the answer is a route or a card. An enemy that only adds hit points asks nothing.
- **It has one odd part**, said first in its prompt and sized, as the prompt guide says.
- **It never sees an understudy.** Its targets are the box office, the magician, or applause on the floor.
- **It drops applause only to the live magician's killing blow**, like everything else. Bosses drop more.
- **A new behaviour is a named field** in `EnemyKind`, read in one place in `Simulation`. Two enemies that need the same field are cheaper than one that needs two.
- **It dies into something.** The paper scraps are the baseline; each kind adds one thing of its own (tickets, feathers, newsprint) so the carnage is legible.

## 3. The roster

The summary first, then each in turn. "v1" means it belongs in the prototype after the encore lands; "later" means after G1.

| Enemy | Odd part | Asks | From act | Needs | Status |
|---|---|---|---|---|---|
| Critic | eye, bowler, pen | baseline | 1 | exists | exists |
| Stagehand | spotlight head | the side door | 4 | exists | exists |
| Scalper | a fan of tickets for a face | fetch the applause or kill the thief | 3 | a target of applause; `ApplauseEaten` | v1 |
| Rival's understudy | a flat cardboard rival on a stand | can you focus damage | 4 | `strikeDamage` per kind | v1 |
| Heckler | a megaphone for a head | are you standing still | 5 | `holdsAtRange`; an enemy projectile | v1 |
| Prompter | a prompter's box, a speaking trumpet | kill the support at the door | 6 | `holdsAtDoor`; a speed aura | v1 |
| Drunk patron | a bottle for a head | do you have cards that cannot miss | 7 | a random stream per critic | v1 |
| Intern | a notebook for a head, small | do you have area damage | 8 | spawn on death per kind | v1 |
| The Diva | a headdress of feathers, a mouth | time your pick-ups between the notes | 5 (boss) | magician knockback; applause drift | v1 |
| The Reviewer | a fountain pen for a head, a scroll | the finale | 10 (boss) | shockwaves; a summoned row | v1 |
| The Claque | hands for heads | do you have burst | 5 | group entries; formation | later |
| Phantom | a mask | intercept at the door | 8 | teleport; invulnerability | later |
| Usher | a torch for a head | cross the rope or go round | 7 | a trailing hazard | later |

### 3.1 Scalper (the ticket tout)

- **Odd part:** a fan of tickets where the face should be, a coat too long for it.
- **Asks:** fetch your applause now, or kill the thief first.
- **Behaviour:** enters by a door and runs to the nearest piece of applause on the floor. When it reaches one it eats it, with a puff of torn tickets, and runs to the next; with none left it walks to the box office like a critic. It never turns on the magician.
- **Numbers (sketch):** life 1, speed 6, radius 0.4, cost 1, weight 1, from act 3.
- **With applause:** this is the first enemy that touches the applause rule directly. A piece left lying is a piece lost, which is the point: the magician must go to where it killed. Under the encore, a stolen piece is a stolen encore.
- **Guard:** helps the guard. Applause near the box office is eaten as readily as applause at a door, and a player who orbits earns none anyway. Flag: none.
- **Needs:** a target of "nearest applause" (one new branch in the walk), one event `ApplauseEaten` for the view and the sound.
- **Look and death:** runs with tickets flying off it. Dies into tickets.

### 3.2 Rival's understudy (the brute)

- **Odd part:** a flat cardboard cut-out of a rival magician, a visible wooden stand behind it, slightly bent.
- **Asks:** can you focus damage, and does a route of yours cross its path.
- **Behaviour:** walks slowly and straight to the box office, pushes critics ahead of it (its radius is large), ignores the magician entirely (never turns), and strikes the box office for three.
- **Numbers:** life 15, speed 1.5, radius 0.9, cost 4, weight 1, from act 4.
- **With understudies:** a lane that two understudies cover kills it; a lane none covers costs the box office nine or twelve. It is the enemy that makes the understudies' placement matter, which the critic alone does not.
- **Guard:** neutral. It walks into the pile, but slowly, and the pile can kill it. Flag: none.
- **Needs:** `strikeDamage` per kind (today the critic's strike is one number for all).
- **Look and death:** flat, so it turns edge-on when it changes direction (a cheap, funny effect). Dies by falling over flat, as a sheet, and lies there.

### 3.3 Heckler

- **Odd part:** a megaphone for a head, a crate of tomatoes under one arm.
- **Asks:** are you standing still.
- **Behaviour:** enters and holds about nine units from the magician, along the aisles (the stage's sides). It throws a tomato at the magician every two seconds, aimed ahead of the magician's movement as the magician's own cards are; a tomato hurts for one and splashes. It never walks to the box office. When the magician comes within four it backs away; when the magician is out of its range it edges closer.
- **Numbers:** life 2, speed 3, range 9, cost 2, weight 1, from act 5.
- **With applause:** a heckler that falls to the magician's card drops applause where it stood, which is away from the pile by construction.
- **Guard:** helps the guard strongly. A magician that stands among its understudies is a target that does not move, and tomatoes land on it. The vision's fallback (positional threats) in a single kind.
- **Needs:** `holdsAtRange`, and an enemy projectile: the existing `ThrownCard` with a flag for who threw it and a target of the magician (the lead-shot arithmetic is already there).
- **Look and death:** red splashes on the floor that fade. Dies into a burst of tomatoes.

### 3.4 Prompter

- **Odd part:** a prompter's box worn like a hat, a speaking trumpet where the mouth is.
- **Asks:** will you go to the door and kill the support, or let the critics come "on cue".
- **Behaviour:** enters and stops just inside its door, and stays there. Critics within six units of it walk half again as fast and do not turn on the magician (they are on cue). It never walks to the box office.
- **Numbers:** life 4, speed 2, cost 3, weight 1, from act 6.
- **With understudies:** an understudy whose route passes the door kills it on schedule, every act. The prompter is the enemy that turns "I ran to the door once" into "the door is covered".
- **Guard:** helps the guard. The only way to stop the aura is at the door. Flag: none.
- **Needs:** `holdsAtDoor`, and a speed and turn-radius aura read in the critics' walk (one loop over prompters per critic, or a flag set once a tick).
- **Look and death:** a faint cone from the trumpet toward the critics it drives. Dies with a squeal from the trumpet, into pages of the script.

### 3.5 Drunk patron

- **Odd part:** a bottle for a head, a cravat undone.
- **Asks:** do you have cards that cannot miss (pierce, burst, the fan).
- **Behaviour:** walks to the box office in a zigzag drawn from a random stream of its own, so the lead shot that hits a critic misses it half the time. It turns on the magician like a critic, and its touch hurts the same.
- **Numbers:** life 2, speed 3, cost 1, weight 1, from act 7.
- **Guard:** neutral. Flag: none.
- **Needs:** a random stream per critic (deterministic: the entry's index and the seed), and a `wander` field on the kind. The vision's determinism rules hold.
- **Look and death:** staggers; dies into a splash and a bottle that rolls.

### 3.6 Intern (the critic's intern)

- **Odd part:** a notebook for a head, half a critic's size, running.
- **Asks:** do you have area damage.
- **Behaviour:** does not enter by a door. From act 8, when a critic falls, an intern runs out of its body toward the box office. Life 1, speed 4, never turns on the magician. It drops no applause (it is not an entry, so it would inflate nothing).
- **Numbers:** life 1, speed 4, radius 0.3, from act 8.
- **With the curve:** this is how late acts get dense without the entry cliff the review describes (§3.2 there): every kill is a second, smaller body. It must respect the cap on live enemies of review ticket R2, or it is the cliff by another door.
- **Guard:** neutral. Flag: watch the cap.
- **Needs:** `spawnsOnDeath` on the critic's kind, naming the intern's kind.
- **Look and death:** a tiny figure with pages flying. Dies into a single page.

### 3.7 The Diva (boss of act 5)

- **Odd part:** a headdress of feathers three times her height, a mouth that is most of the face.
- **Asks:** can you time your pick-ups between the notes, and can you burst her down.
- **Behaviour:** enters at the curtain of act 5 and walks slowly to the centre of the stage. There she sings. Every eight seconds a note: a ring that spreads from her and pushes the magician and every critic outward by three units (the first time anything moves the magician). While she sings, every piece of applause on the floor drifts toward her and is gone when it reaches her: she takes the applause. She does not strike the box office; the critics of the act do, and her notes throw them about too. When she falls the act goes on.
- **Numbers:** life 60, speed 1, radius 1.2, the note every 8 s with a reach of 10, applause drift at 2 units a second.
- **With applause:** the only enemy that competes for applause with the magician rather than eating it where it lies. A player who kills near her loses the pieces; a player who kills at the edge and runs keeps them.
- **Guard:** helps. The box office is near the centre; her notes push the pile apart and her drift empties its applause. Flag: none.
- **Needs:** magician knockback (a new motion the recording must include, since the understudy of act 5 was pushed too), applause drift, a boss entry in `Waves` (one planned entry of a kind with `boss: true`, outside the budget).
- **Look and death:** feathers everywhere; the ring is a visible wave in the floorboards. Dies into a storm of feathers and drops a bouquet (applause worth five, one piece).

### 3.8 The Reviewer (boss of act 10)

- **Odd part:** a fountain pen for a head, a scroll of the column unrolling behind it as it walks.
- **Asks:** everything at once.
- **Behaviour:** enters at the curtain of act 10 and walks straight to the box office, writing. Every six seconds a paragraph: a shockwave from the pen that hurts the magician for one within six units and pushes critics. Every ten seconds a quote: a row of six critics enters by one door in a line. It strikes the box office for five. When it falls the act goes on; the ovation is the act's end as ever.
- **Numbers:** life 150, speed 1.2, radius 1.2.
- **Guard:** neutral; by act 10 the guard has decided.
- **Needs:** the shockwave (the Diva's ring with damage), a summoned entry (the wave planner called in the act, from its own stream), `strikeDamage`.
- **Look and death:** the scroll grows along its route. Dies into an explosion of newsprint that covers the floor and fades over the ovation.

### 3.9 Later: the Claque, the Phantom, the Usher

- **The Claque** (hands for heads): five that enter together and walk in formation; all five killed within two seconds leave a bouquet (applause worth three). It rewards burst and the fan. Needs group entries in `Waves` and a leader to follow. Later because formations are a system of their own.
- **The Phantom** (a mask): every three seconds it vanishes and reappears four units nearer the box office, invulnerable in between. It cannot be kited; it is killed at the door or at the box office. Later because invulnerability windows need their own events and the view's cue.
- **The Usher** (a torch for a head): walks to the box office trailing a velvet rope; a magician that crosses the rope is slowed for a second. The first hazard. Later because a trailing hazard is a new kind of thing on the floor, and nothing today blocks or slows the magician by design.

## 4. Which act brings what

| Act | New | What the act teaches |
|---|---|---|
| 1 | critics | run, throw, pick up |
| 2 | more critics, the first understudy | your route is a defender now |
| 3 | scalper, the second door | applause left lying is lost; go to where you killed |
| 4 | stagehand, rival's understudy | the side door; a lane must be covered, not a point |
| 5 | the Diva, heckler | standing still is punished; time the pick-ups |
| 6 | prompter, the third door | the doors are where the support stands |
| 7 | drunk patron | the cards that cannot miss |
| 8 | intern | area damage; the stage fills |
| 9 | everything | the performance's exam |
| 10 | the Reviewer | the finale |

The acts a kind enters in are a hypothesis each (`fromAct` in `tuning.json`), to be moved by play.

## 5. How a card is designed

- **An understudy can replay it.** No aiming, no second button. If a card needs the player to do something, it is a kit, not a card.
- **It changes something the eye can see**: the thrown card's look (its size, spin, trail, suits), the smoke's look, the magician's gait, the applause's glow. A card that changes only a number also changes the look of the thing it changes, or it is not loud enough for this game.
- **It is a count.** Two of a card add again; the numbers are worked out where they are used, as today.
- **It has a family**, a rarity and a guard flag. Rarity: common (a number), uncommon (a behaviour), rare (an evolution or a one-off).
- **Where it is taken:** self cards through an encore in the act (decision 26); chorus cards after the act. The encore's offer is three self cards; the program's is one chorus card.

## 6. Self cards

Three families: the Hands (the throw), the Feet (movement and applause), the Smoke (the Vanish).

### 6.1 The Hands

| Card | Does | Shows | Rarity | Guard | Needs |
|---|---|---|---|---|---|
| Sharper cards | +0.5 damage a card | a darker edge on the card | common | none | exists |
| Quicker hands | throws more often | a shorter gap, a brighter flick | common | none | exists |
| A longer arm | +1.5 range | the card flies further, a longer trail | common | none | exists |
| One more card | one more card a throw, at the next nearest | two trails | common | none | exists |
| The pierce | the card goes on through the critic it strikes, losing a quarter of its damage each time | the card does not stop; a line of bursts | uncommon | none | T25 |
| The ricochet | the card turns to the next critic within four when it strikes, up to two turns | a bent trail | uncommon | none | T25 |
| The burst | the card bursts where it strikes: a half-damage ring of two units | a ring of suits | uncommon | none | T25 |
| A heavy card | a struck critic is pushed one unit back | a thud, a skid | uncommon | none | knockback on critics |
| The boomerang | the card comes back to the hand and strikes again on the way | a curved return trail | uncommon | none | a returning projectile |
| The split | on its first strike the card becomes two, at the two next nearest | a fork in the trail | uncommon | none | spawn cards on hit |
| The mark | a struck critic is marked for three seconds; the next card at a marked critic does double | a chalk mark over the critic | uncommon | none | a status with a timer |
| Ink | a struck critic drips ink: 0.5 damage a second for three seconds | black drops, a dark stain | uncommon | none | a status with a timer; the first damage over time |
| Confetti | a struck critic is slowed by 40 % for two seconds | a burst of confetti that settles | uncommon | none | a status with a timer |
| The fan | with three or more critics in range, the throw is a fan of cards across them instead of a series | a spread of cards | uncommon | none | a second throw pattern |

### 6.2 The Feet

| Card | Does | Shows | Rarity | Guard | Needs |
|---|---|---|---|---|---|
| Quick feet | +1 speed | a faster gait, dust at the heels | common | none | a speed count |
| A long reach | +0.4 pick-up reach | applause glows when within reach | common | watch: T21 found the guard jumpy here | exists as a number |
| Lasting applause | pieces lie two seconds longer | a slower dim | common | none | a count on `applauseTime` |
| The magnet | applause within three drifts to the magician | pieces slide across the boards | uncommon | **flagged**: it makes the orbit's few pieces easier; in only with the kiting player in the guard | applause motion |
| A cheaper encore | the first encore of the act costs one piece less | the bar's mark moves | uncommon | none | a count on the encore's cost |
| Second wind | a piece picked up heals one, up to two a act | a green glint on pick-up | uncommon | none | healing on `ApplausePickedUp` |
| Thick skin | +2 life | a padded coat | common | none | a count on the magician's life |
| The curtain call | once a performance, the fallen magician rises after three seconds with half its life | the curtain twitches, a spotlight | rare | **flagged**: a fall is the kiting player's main loss; in only with the kiter in the guard | a one-off state |

### 6.3 The Smoke

| Card | Does | Shows | Rarity | Guard | Needs |
|---|---|---|---|---|---|
| A quicker Vanish | a shorter cooldown | the bar fills faster | common | none | exists |
| A longer leap | +2 units | a longer puff | common | none | a count on the distance |
| Two puffs | a second charge | two marks on the bar | uncommon | none | a charge count; the recording keeps both |
| Thicker smoke | +1 cloud radius | a bigger cloud | common | none | a count |
| Lingering smoke | +1 s cloud | a slower thinning | common | none | a count |
| Choking smoke | +1 s stun | critics stay pale longer | common | none | a count |
| Burning smoke | critics in the cloud drip ink | dark smoke | uncommon | none | ink, as above |
| The vortex | the cloud pulls critics within four toward its middle for its whole life | a swirl, critics skidding inward | uncommon | none | a pull force; the Herder proposal arrives as a card |
| The decoy | the Vanish leaves a cardboard stand-up of the magician where it stood; for three seconds critics that have already turned on the magician walk at the decoy instead, and strike it for nothing; then it falls flat | a flat magician on a stand, critics punching cardboard | rare | **resolved by the rule**: only critics already turned are taken, so the pile at the box office is not protected by it | a decoy entity; the critics' turn rule reads it |
| Departure burst | six cards in a ring where the Vanish starts | a ring of cards | uncommon | none | a throw pattern on the Vanish's tick |
| Arrival burst | six cards in a ring where it lands | a ring of cards | uncommon | none | the same |
| Phase | the Vanish passes through critics and pushes them aside | critics knocked off the line | uncommon | none | knockback along the dash |

## 7. Chorus cards

After every act, one card, for every understudy there is and will be.

| Card | Does | Shows | Rarity | Guard | Needs |
|---|---|---|---|---|---|
| A louder chorus | +0.5 damage for understudies | brighter cards from the chorus | common | none | exists |
| Longer arms | +1.5 range for understudies | longer trails | common | none | a count |
| A quicker chorus | understudies throw more often | | common | none | a count |
| Ink chorus | understudies' cards drip ink | | uncommon | none | ink |
| Choking chorus | understudies' clouds stun a second longer | | common | none | a count |
| Duller critics | critics turned on the magician within an understudy's range lose 20 % speed | critics slow near the ghosts | uncommon | **flagged**: it rewards standing near understudies; the review cut Resonance for the same reason | an aura read in the walk |
| The understudy learns the part | the newest understudy gets the self cards the magician holds now, not those it began with | the ghost brightens | rare | none | one assignment at `GoOn` |
| An encore for the chorus | every understudy takes one more encore in its act, drawn from the cards the magician holds, at a tick of its own | | rare | none | T24's machinery, one more entry |

Under decision 26 the chorus card is free after every act. The review (§3.8 there) argues it needs a price: offered only after an act with at least one encore or one piece of applause. This list assumes that price.

## 8. Evolutions

Two of the same uncommon card make a visibly different trick. The second copy is the trigger, so an evolution costs two encores and reads as a reward for commitment. Each evolution is the "carnage" the owner asked for: the card's look changes outright.

| Two of | Becomes | What it is | Guard |
|---|---|---|---|
| Quicker hands | The card storm | a continuous stream of cards while a critic is in range | none |
| One more card | The fan | the fan of §6.1, always | none |
| The ricochet | The pinball | no limit on turns for two seconds after the first strike | none |
| The pierce | The guillotine | a card the width of a critic that stops for nothing | none |
| The burst | The confetti cannon | the ring is four units and slows | none |
| Ink | The printing press | ink spreads between critics that touch | none |
| The decoy | The cardboard army | the decoy throws cards too, for its three seconds | the decoy's own rule holds |
| The vortex | The trapdoor | the cloud swallows critics under two life | none |
| The boomerang | The juggler | three cards in the air at once | none |
| The magnet | The usherette | applause from the whole stage walks to the magician | **flagged**: in only if the guard with the kiter allows it; likely not |

## 9. The pace of the offer, and the carnage curve

- About one encore in an early act and two or three in a late one gives fifteen to twenty-five self cards in a performance, and nine chorus cards.
- With about twenty-six self cards in the pool, the third encore of a kind is common by act five, so evolutions fire by themselves in the middle of a performance. That is the engine: the screen gets louder because of what the player chose twice, not because the acts got bigger.
- Every understudy carries its act's cards, so a card storm taken in act four is a card storm on the stage in every act after, from the understudy of act four. By act eight the stage has whatever the player built, nine times.
- Rarity weights are a hypothesis: common 60 %, uncommon 35 %, rare 5 %, with no rare before act three.

## 10. What to build first

In the order a ticket each, after the encore (T23–T25) lands and with the kiting player in the guard (review ticket R1) before the flagged ones:

1. **Enemies:** the scalper (teaches the applause rule), the rival's understudy (makes lanes matter), the heckler (punishes standing), the prompter (pulls to the doors). Each is one new field. Then the Diva and the Reviewer, which share the ring.
2. **Self cards:** the three of T25, then ink, confetti, a heavy card, two puffs, the vortex, the decoy. With the existing five that is fourteen.
3. **Chorus cards:** longer arms, a quicker chorus, ink chorus, the understudy learns the part. With the existing one that is five.
4. **Evolutions:** the card storm, the fan, the confetti cannon, the printing press.

Every ticket re-runs the guard and re-pins the hashes, as the repository's rules say.

## 11. Kits

The vision's kits (doves that home, a wand that beams, a hat that drops rabbits as mines, a saw that sweeps) are unlocks that replace the Hands family's base throw, not cards. The Feet, the Smoke and the chorus cards are kit-agnostic; the Hands cards are written for thrown cards and each kit would reinterpret them (a pierce for the wand is a longer beam, a ricochet for the doves is a second target). Kits are out of this list and out of the prototype (plan §8).

## 12. Open questions for the owner

1. The three flagged cards (the magnet, the curtain call, duller critics) and the usherette: in with the kiter in the guard, or out?
2. The intern: does spawn-on-death stay under the cap on live enemies of review ticket R2, or does it wait for the cap?
3. Names in the fiction for the Diva and the Reviewer, if they are to have any.
4. The chorus card's price under decision 26: at least one encore, at least one piece, or free as written.
5. Whether the heckler's tomato should hurt the box office when it misses the magician and flies on. The list says no.
