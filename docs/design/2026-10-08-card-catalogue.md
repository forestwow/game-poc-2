# The card catalogue

- **Status:** written 2026-10-08 at the owner's request, after `docs/design/2026-10-08-enemies-and-cards.md` (the cards as tables of effects) and `docs/design/2026-10-08-chorus-synergies-and-notices.md` (the four synergies). This is the same set of cards as a catalogue: a stage name, the text on the card, a line of flavour, the rule, what a second copy adds, what it shows, and the ticket that brings it. Nothing is agreed until the owner names a ticket. No code.
- **Why a catalogue:** the owner took from Balatro the idea that a card is readable at a glance and has a character. Today the six cards exist as an enum and a sentence assembled from numbers (`Describe` in `ProgramScreen.cs`), and the fifty others exist as table rows. A card the player reads in the twelve seconds of an encore needs a name, one sentence and a joke.
- **Numbers in the plan:** the plan (`docs/plan-prototype.md`, "The road from here") numbers a ticket when it is taken, so this catalogue labels what has no number yet **C1 to C9**; each gets the plan's next free number when taken. Where the plan has a number it is used: T25 (the pierce, the ricochet, the burst), T32 (chorus cards as kinds), T33 to T36 (the four synergies). The plan's T26 (the waves' shape), T27 (a capture that plays), T28 (events say who) and T37 (an honest guard) are built and are not this document's.
- **Read against `main` after T26 to T28 and T37:** no applause drops within `applauseBoxOfficeRadius` (8) of the box office (decision 27); the chorus card comes only after an act with an encore (decision 26 as the owner changed it; "or one piece of applause" was not taken); an encore costs 3 and one more for each taken, so a player who goes out takes some forty in a performance and one who stays on the ring some twenty, most of them in act one and few in act two (T26, T37); events already say who threw and which critic (T28); the guard has a third player, the kiter, which walks the ring just outside the radius, never loses and earns half (T37), and the owner's open question is whether the ring must lose outright.
- **Facts that shape the texts:** every number lives in `tuning.json` and is reloaded with F5, so a text on a card names its number by its key and the view fills the value in; the text is never wrong after a retune. Cards are counts, so every entry says what a second copy does. Everything is in English. The tone is the vision's: absurd, warm, a little mean.

## 0. Rules for a card's text

1. **The sentence on the card names its number by its tuning key**, written here as `key`; the view prints the value. "Thrown cards hurt for `cardDamage` more" prints as "Thrown cards hurt for 0.5 more".
2. **Present tense, second person, at most fourteen words.** The player reads it while the act stands.
3. **The flavour line never explains the rule.** If the rule needs the flavour, the sentence is wrong.
4. **A second copy is said in the sentence** when it does something other than add again ("Two copies: twice"); "Each copy again" is the default and may be left out.
5. **An understudy replays everything here without an input.** A card that would need one is not a card.

The entry format: the name (and today's name where one exists), then family, rarity, where it is taken (an encore in the act, or the program after it), the guard flag, the ticket; then the sentence, the flavour, the rule, the second copy, and what it shows. Numbers in rules are sketches; the keys are proposals in the style of the existing ones (`cardDamage`, `cardAttackSpeed`, `cardRange`, `cardVanishCooldown`, `cardChorusDamage`).

## 1. The Hands (the throw)

**Card Sharp** (today: sharper cards) · Hands · common · encore · guard none · exists
- On the card: "Thrown cards hurt for `cardDamage` more. Each copy again."
- Flavour: "Honed on the edge of a bad review."
- Rule: a thrown card's damage is the base plus `cardDamage` for each copy.
- Second copy: another `cardDamage`.
- Shows: a darker edge on the card; the burst a little bigger.

**Sleight of Hand** (today: quicker hands) · Hands · common · encore · guard none · exists
- On the card: "Throws come `cardAttackSpeed` more often. Each copy again."
- Flavour: "The hand is quicker than the critic."
- Rule: the throw's wait is `throwCooldown` divided by one plus `cardAttackSpeed` times the copies.
- Second copy: the divisor grows again.
- Shows: a shorter gap, a brighter flick at the hand.

**Long Arm** (today: a longer arm) · Hands · common · encore · guard none · exists
- On the card: "Cards reach `cardRange` further. Each copy again."
- Flavour: "Reach the back row. They paid less; they deserve a card too."
- Rule: throw range and the card's flight grow by `cardRange` per copy.
- Second copy: again.
- Shows: a longer trail.

**One More for the Lady** (today: one more card) · Hands · common · encore · guard none · exists
- On the card: "Each throw sends one more card, at the next nearest critic."
- Flavour: "Pick a card. No, you. Yes, you too."
- Rule: cards a throw are one plus the copies, each at its own nearest critic in range; with one critic, one card.
- Second copy: one more again.
- Shows: parallel trails.

**The Pierce** · Hands · uncommon · encore · guard none · T25
- On the card: "A card goes on through the critic it strikes, losing `cardPierceLoss` each time."
- Flavour: "Through the critic, through the review, through the paper it is printed on."
- Rule: on a strike the card flies on with its damage reduced by `cardPierceLoss`, until it is spent or out of range.
- Second copy: the loss halves.
- Shows: the card does not stop; a line of bursts.

**The Ricochet** · Hands · uncommon · encore · guard none · T25
- On the card: "A card that strikes turns to the next critic within `cardRicochetReach`. Two copies: twice."
- Flavour: "No review goes unanswered."
- Rule: on a strike the card retargets the nearest other critic within reach, once per copy.
- Second copy: a second turn.
- Shows: a bent trail.

**The Burst** · Hands · uncommon · encore · guard none · T25
- On the card: "Where a card strikes, a ring of `cardBurstRadius` hurts for half the card."
- Flavour: "Applause, but sharp."
- Rule: on a strike every critic within the radius takes half the card's damage.
- Second copy: the ring does the whole card.
- Shows: a ring of suits.

**Heavy Stock** · Hands · uncommon · encore · guard none · C2
- On the card: "A struck critic is shoved `cardPush` back."
- Flavour: "Printed on something that could stop a door."
- Rule: on a strike the critic is displaced along the card's heading.
- Second copy: twice the shove.
- Shows: a thud, a skid in the boards.

**The Boomerang** · Hands · uncommon · encore · guard none · C2
- On the card: "A card comes back to your hand and strikes again on the way."
- Flavour: "Everything you throw at a critic comes back to you. Here, usefully."
- Rule: at the end of its flight the card returns along its line to the thrower and strikes what it crosses once more.
- Second copy: it goes out a second time.
- Shows: a curved return trail.

**The Split** · Hands · uncommon · encore · guard none · C2
- On the card: "On its first strike a card becomes two, at the two next nearest critics."
- Flavour: "Tear it in half. Now there are two bad reviews."
- Rule: on the first strike two cards of half damage leave for the two nearest other critics in range.
- Second copy: three.
- Shows: a fork in the trail.

**The Mark** · Hands · uncommon · encore · guard none · C1
- On the card: "A struck critic is marked for `cardMarkTime`. The next card at it does double."
- Flavour: "A chalk cross on the back. The usher knows what it means."
- Rule: a status with a timer; the next strike spends it for double damage.
- Second copy: triple.
- Shows: a chalk mark over the critic.

**Spilled Ink** · Hands · uncommon · encore · guard none · C1
- On the card: "A struck critic drips ink: `cardInkDamage` a second for `cardInkTime`."
- Flavour: "Their own medium, used against them."
- Rule: a status with a timer that hurts every tick; a new strike restarts the timer.
- Second copy: the drip doubles.
- Shows: black drops, a stain that spreads where it walks.

**Confetti** · Hands · uncommon · encore · guard none · C1
- On the card: "A struck critic is slowed by `cardConfettiSlow` for `cardConfettiTime`."
- Flavour: "They cannot walk and pick it out of their hair at the same time."
- Rule: a status with a timer that scales the critic's speed.
- Second copy: slower still.
- Shows: a burst of confetti that settles on the floor.

**The Fan** · Hands · uncommon · encore · guard none · C2
- On the card: "With `cardFanThreshold` or more critics in range, the throw fans across them all."
- Flavour: "For the matinee crowd: everyone gets one."
- Rule: when the critics in range reach the threshold, the throw is one card at each of them instead of a series.
- Second copy: the threshold drops by one.
- Shows: a spread of cards.

## 2. The Feet (movement and applause)

**Quick Feet** · Feet · common · encore · guard none · C3
- On the card: "You walk `cardSpeed` faster. Each copy again."
- Flavour: "The stage is wide. The critics are slow. Be the other thing."
- Rule: the magician's speed grows by `cardSpeed` per copy; an understudy keeps the speed it ran at, since places are recorded.
- Second copy: again.
- Shows: dust at the heels, a faster gait.

**Long Reach** · Feet · common · encore · guard watch (T21 found the guard jumpy here, and since T37 the kiter's reach is the guard's own geometry) · C3
- On the card: "Applause `cardReach` further off is yours."
- Flavour: "Bow lower. It is all down there."
- Rule: the pick-up reach grows by `cardReach` per copy.
- Second copy: again.
- Shows: a piece glows when within reach.

**Lingering Applause** · Feet · common · encore · guard none · C3
- On the card: "Applause lies `cardApplauseTime` longer."
- Flavour: "A slow clap, in the good sense."
- Rule: pieces dropped from now lie longer by `cardApplauseTime` per copy.
- Second copy: again.
- Shows: a slower dimming.

**The Magnet** · Feet · uncommon · encore · **guard flagged** · C7
- On the card: "Applause within `cardMagnetReach` slides to you."
- Flavour: "They came to you. That is the whole trick."
- Rule: pieces within reach move toward the magician at `cardMagnetSpeed`.
- Second copy: the reach doubles.
- Shows: pieces sliding across the boards.
- Why flagged: since decision 27 nothing drops within the radius, so the magnet pays only outside the ring; what it changes is how far the kiter of T37 must step off its circle for a piece. In only if the guard with the kiter still holds: the doors player to the ovation at least as often, with half as many encores again.

**Cheap Seats** · Feet · uncommon · encore · guard none · C3
- On the card: "Your first encore of each act costs `cardEncoreDiscount` fewer pieces."
- Flavour: "A discount for the regulars."
- Rule: the first encore's cost in an act drops by the discount per copy, never below one.
- Second copy: again.
- Shows: the bar's mark moves.

**Second Wind** · Feet · uncommon · encore · guard none · C3
- On the card: "A piece picked up heals `cardSecondWindHeal`, up to `cardSecondWindCap` an act."
- Flavour: "Nothing heals like being liked."
- Rule: on a pick-up the magician heals, to a cap per act.
- Second copy: the cap doubles.
- Shows: a green glint on pick-up.

**Padded Coat** · Feet · common · encore · guard none · C3
- On the card: "You have `cardThickSkin` more life from the next act."
- Flavour: "Tailored for tomatoes."
- Rule: the magician's life at the next curtain grows by `cardThickSkin` per copy.
- Second copy: again.
- Shows: a thicker coat (a tint now, a sprite later).

**Curtain Call** · Feet · rare · encore · **guard flagged** · C7
- On the card: "Once a performance, you rise `cardCurtainCallDelay` after a fall, with half your life."
- Flavour: "They are still clapping. Get up."
- Rule: one rise per performance; the recording goes on after it, so that act's understudy rises too.
- Second copy: twice a performance.
- Shows: the curtain twitches, a spotlight finds you.
- Why flagged: the orbit loses by being felled in act three, not starved (T37), so a rise is the one card that could carry it past the guard's act six. In only if the orbit still loses by act six with it on.

## 3. The Smoke (the Vanish)

**Quick Smoke** (today: a quicker Vanish) · Smoke · common · encore · guard none · exists
- On the card: "The Vanish comes back `cardVanishCooldown` sooner. Each copy again."
- Flavour: "The smoke clears. You do not."
- Rule: the cooldown is divided by one plus `cardVanishCooldown` times the copies.
- Second copy: again.
- Shows: the bar fills faster.

**Long Leap** · Smoke · common · encore · guard none · C4
- On the card: "The Vanish carries you `cardLeap` further."
- Flavour: "From here to the exit, in one puff."
- Rule: the distance grows by `cardLeap` per copy.
- Second copy: again.
- Shows: a longer puff.

**Two Puffs** · Smoke · uncommon · encore · guard none · C4
- On the card: "The Vanish has a second charge."
- Flavour: "Smoke, mirrors, more smoke."
- Rule: charges are one plus the copies; every Vanish tick is recorded as today.
- Second copy: a third charge.
- Shows: two marks on the bar.

**Thick Smoke** · Smoke · common · encore · guard none · C4
- On the card: "The cloud is `cardCloudRadius` wider."
- Flavour: "The front row cannot see. Good."
- Rule: the cloud's radius grows per copy.
- Second copy: again.
- Shows: a bigger cloud.

**Slow Smoke** · Smoke · common · encore · guard none · C4
- On the card: "The cloud lasts `cardCloudTime` longer."
- Flavour: "It hangs in the air like a bad notice."
- Rule: the cloud's life grows per copy.
- Second copy: again.
- Shows: a slower thinning.

**Choking Smoke** · Smoke · common · encore · guard none · C4
- On the card: "Critics in the cloud stand stunned `cardStunTime` longer."
- Flavour: "Coughing counts as a review."
- Rule: the stun grows per copy.
- Second copy: again.
- Shows: they stay pale longer.

**Burning Smoke** · Smoke · uncommon · encore · guard none · C5 (after C1)
- On the card: "Critics in the cloud drip ink."
- Flavour: "Printers' smoke. It gets into everything."
- Rule: a critic the cloud touches gets the ink status.
- Second copy: the drip doubles.
- Shows: dark smoke.

**The Vortex** · Smoke · uncommon · encore · guard none · C5
- On the card: "The cloud pulls critics within `cardVortexReach` toward its middle."
- Flavour: "Everyone leans in for the trick. Everyone."
- Rule: critics within reach move toward the cloud's centre at `cardVortexPull` a second for the cloud's life.
- Second copy: the reach doubles.
- Shows: a swirl, critics skidding inward. The Herder proposal, as a card.

**Vanishing Cabinet** · Smoke · rare · encore · guard resolved by the rule · C6
- On the card: "The Vanish leaves a cardboard you. Critics already chasing you chase it for `cardDecoyTime`."
- Flavour: "Audiences never notice the difference. Neither do critics."
- Rule: a decoy stands where the Vanish began; critics that have turned on the magician and are within `cardDecoyReach` walk at it instead and strike it for nothing; critics walking to the box office ignore it; it falls flat when its time is up.
- Second copy: it stands twice as long.
- Shows: a flat magician on a stand, critics punching cardboard.
- Why it passes the guard: only critics already chasing are taken, so a pile at the box office is not protected by it.

**Grand Exit** · Smoke · uncommon · encore · guard none · C5
- On the card: "Where the Vanish starts, `cardRingCards` cards fly out in a ring."
- Flavour: "Leave them wanting less of you."
- Rule: on the Vanish tick a ring of cards leaves the departure point, each with the throw's damage.
- Second copy: a second ring.
- Shows: a ring of cards.

**Grand Entrance** · Smoke · uncommon · encore · guard none · C5
- On the card: "Where the Vanish lands, `cardRingCards` cards fly out in a ring."
- Flavour: "Ta-da, with knives."
- Rule: the same, at the arrival point.
- Second copy: a second ring.
- Shows: a ring on landing.

**Walk Through** · Smoke · uncommon · encore · guard none · C5
- On the card: "The Vanish passes through critics and shoves them `cardPhasePush` aside."
- Flavour: "Excuse me. Excuse me. Excuse me."
- Rule: critics along the dash's line are displaced sideways.
- Second copy: twice the shove.
- Shows: critics knocked off the line.

## 4. The Chorus (after the act, for every understudy)

**Louder Chorus** (today: the chorus card) · Chorus · common · program · guard none · exists
- On the card: "Every understudy's cards hurt for `cardChorusDamage` more. Each copy again."
- Flavour: "Nine of you, and all of you have opinions."
- Rule: as today.
- Second copy: again.
- Shows: brighter cards from the chorus.

**Longer Arms** · Chorus · common · program · guard none · C8
- On the card: "Every understudy's cards reach `chorusRange` further."
- Flavour: "The chorus reaches the balcony."
- Rule: understudies' range grows per copy.
- Second copy: again.
- Shows: longer trails from the ghosts.

**Quicker Chorus** · Chorus · common · program · guard none · C8
- On the card: "Every understudy throws `chorusAttackSpeed` more often."
- Flavour: "Tempo, please. Tempo."
- Rule: understudies' throw wait divided as Sleight of Hand's is.
- Second copy: again.
- Shows: a denser stream from the ghosts.

**Inky Chorus** · Chorus · uncommon · program · guard none · C8 (after C1)
- On the card: "Every understudy's cards drip ink."
- Flavour: "Nine pens. One newspaper."
- Rule: understudies' strikes apply the ink status.
- Second copy: the drip doubles.
- Shows: dark trails.

**Choking Chorus** · Chorus · common · program · guard none · C8
- On the card: "Every understudy's cloud stuns `chorusStun` longer."
- Flavour: "A chorus of coughs."
- Rule: the recorded Vanishes' clouds stun longer.
- Second copy: again.
- Shows: pale critics around the ghosts' clouds.

**Learns the Part** · Chorus · rare · program · guard none · C8
- On the card: "Your newest understudy takes the cards you hold now, not the ones it was born with."
- Flavour: "It was watching the whole time."
- Rule: at the next `GoOn`, the understudy made from the act just over gets the magician's current cards instead of the act's opening ones.
- Second copy: the two newest.
- Shows: the ghost brightens for a moment at the curtain.

**Duller Critics** · Chorus · uncommon · program · **guard flagged** · C7
- On the card: "Critics chasing you move `chorusDullerSlow` slower inside any understudy's range."
- Flavour: "They came to see you. They cannot take their eyes off the copies."
- Rule: turned critics within an understudy's range have their speed scaled down.
- Second copy: slower still.
- Shows: critics dragging their feet near the ghosts.
- Why flagged: it rewards the live magician for staying near understudies, which is Resonance's flaw with another name.

**Encore for the Chorus** · Chorus · rare · program · guard none · C8 (after T24)
- On the card: "Each understudy takes one more encore in its act, from the cards you hold."
- Flavour: "They get a bow too."
- Rule: every understudy gains one extra self card per act, drawn from the magician's held kinds by the program's stream, at a tick of its own.
- Second copy: two.
- Shows: a small flourish over the ghost when it takes it.

**Crossfire** · Chorus · uncommon · program · guard none · T33
- On the card: "An understudy's card striking within `crossfireWindow` of another's does double."
- Flavour: "Two of you had the same idea. It was a good one."
- Rule: the synergies document (the plan's T33).
- Second copy: triple.
- Shows: a gold burst, a thread between the two ghosts for half a second.

**Hand-off** · Chorus · uncommon · program · guard none · T34
- On the card: "A critic passed between two understudies' ranges within `handOffWindow` takes a triple first card."
- Flavour: "Stage left hands it to stage right. Nobody drops it."
- Rule: the synergies document (the plan's T34).
- Second copy: the first two cards.
- Shows: a baton over the critic, a fanfare on the strike.

**Chorus Line** · Chorus · uncommon · program · guard none · T35
- On the card: "Two understudies within `chorusLineDistance` of each other each throw one more card."
- Flavour: "Kick, kick, card."
- Rule: the synergies document (the plan's T35).
- Second copy: two more.
- Shows: cards in pairs, a thread between the two.

**Duet** · Chorus · uncommon · program · guard none · T36
- On the card: "A critic in two understudies' range makes both throw `duetSpeed` more often."
- Flavour: "They have rehearsed this. Several times, in fact."
- Rule: the synergies document (the plan's T36).
- Second copy: twice the gain.
- Shows: bursts in both acts' colours.

## 5. Evolutions

The second copy of an uncommon card from the Hands or the Smoke is not a count: it replaces the card with its evolution, revealed on the encore's panel as the card is taken. The evolution keeps the first copy's effect and adds the trick. A third copy adds to the evolution as the table says.

**Card Storm** (two of Sleight of Hand) · guard none · C9
- On the card: "While a critic is in range, cards without pause."
- Flavour: "The deck is bottomless. The critic is not."
- Rule: the throw's wait becomes `evoStormCooldown`, a few ticks.
- Third copy: two streams.
- Shows: a continuous stream from the hand.

**The Fan** (two of One More for the Lady) · guard none · C9
- On the card: "Every throw is a fan across everyone in range."
- Flavour: "Everyone gets one, and the back row gets two."
- Rule: the Fan's pattern, always, with no threshold.
- Third copy: two fans.
- Shows: a spread of cards every throw.

**The Pinball** (two of The Ricochet) · guard none · C9
- On the card: "For `evoPinballTime` after its first strike, a card never stops turning."
- Flavour: "Tilt."
- Rule: no limit on turns within the time.
- Third copy: twice the time.
- Shows: a trail that zigzags across the stage.

**The Guillotine** (two of The Pierce) · guard none · C9
- On the card: "A card as wide as a critic that stops for nothing."
- Flavour: "Cuts the review short."
- Rule: the card's width is a critic's diameter and it loses no damage.
- Third copy: twice the width.
- Shows: a wide blade of a card.

**Confetti Cannon** (two of The Burst) · guard none · C9
- On the card: "The ring is `evoCannonRadius` wide and slows."
- Flavour: "Celebrate. Loudly. At them."
- Rule: the burst's radius doubles and applies the slow of Confetti.
- Third copy: the ring hurts for the whole card.
- Shows: a cloud of confetti where the card lands.

**Printing Press** (two of Spilled Ink) · guard none · C9
- On the card: "Ink spreads between critics that touch."
- Flavour: "Tomorrow's edition, printed on the critics."
- Rule: a critic with ink gives it to every critic it touches, once a second.
- Third copy: the drip doubles.
- Shows: stains that join across a crowd.

**Cardboard Army** (two of Vanishing Cabinet) · guard as the decoy's · C9 (after C6)
- On the card: "The cardboard you throws cards too."
- Flavour: "Cheaper than a chorus, and it never asks for notes."
- Rule: the decoy throws like an understudy with the magician's cards for its life.
- Third copy: it stands twice as long.
- Shows: cards from a flat figure.

**The Trapdoor** (two of The Vortex) · guard none · C9
- On the card: "The cloud swallows critics under `evoTrapdoorLife` life."
- Flavour: "Exit, pursued by nothing."
- Rule: a critic in the cloud with less life than the threshold falls at once; the kill is the cloud owner's, so the magician's own cloud drops applause (outside `applauseBoxOfficeRadius`, as any kill) and an understudy's does not.
- Third copy: twice the threshold.
- Shows: a critic dropping through the boards.

**The Juggler** (two of The Boomerang) · guard none · C9
- On the card: "Three cards in the air at once, always returning."
- Flavour: "He only drops them on critics."
- Rule: up to three boomerang cards aloft; the throw goes on while they fly.
- Third copy: five.
- Shows: cards circling the magician.

**The Usherette** (two of The Magnet) · **guard flagged** · C7
- On the card: "Applause walks to you from anywhere on the stage."
- Flavour: "Row K, your clap has been delivered."
- Rule: every piece drifts to the magician.
- Third copy: faster.
- Shows: pieces walking the boards.
- Why flagged: likely out. Applause from the whole stage walking to a player on the ring is the kiter with no reason to step off it; in only if the guard with the kiter allows it.

## 6. The tickets

Each is one pull request, tests first in Core, the guard run after (with the kiter of T37), the hashes pinned again, as the repository's rules say. T25, T32 and T33 to T36 are in other documents and in the plan; C1 to C9 are new and get the plan's next free numbers when taken.

| Ticket | Cards | Needs in Core | Tests (the rule lines) |
|---|---|---|---|
| T25 (planned) | The Pierce, The Ricochet, The Burst | as the plan says | as the plan says |
| C1 Statuses | The Mark, Spilled Ink, Confetti | one status mechanism on a critic: a kind, a timer, a stack; ink hurts in the tick's first step; slow scales the walk; the mark is spent by the next strike | applied on a strike; gone when its time is up; refreshed by another strike; ink hurts per tick; a slowed critic walks slower; the mark doubles exactly once; the hash covers statuses |
| C2 Throw patterns | Heavy Stock, The Boomerang, The Split, The Fan | a push on a critic; a card that returns; cards spawned on a strike; a second throw pattern | the shove's distance; the return strikes once more; the split's two targets; the fan at the threshold and not below it |
| C3 Feet | Quick Feet, Long Reach, Lingering Applause, Cheap Seats, Second Wind, Padded Coat | counts only; the encore's cost reads a count; healing on pick-up | each count applied; an understudy's speed is the recorded one; the cap on healing; life at the next curtain only |
| C4 Smoke counts | Long Leap, Two Puffs, Thick Smoke, Slow Smoke, Choking Smoke | a charge count; the rest counts | each applied; two charges recorded and replayed; a stun's length |
| C5 Smoke behaviours | Burning Smoke, The Vortex, Grand Exit, Grand Entrance, Walk Through | the cloud applies a status; a pull force; rings of cards on the Vanish tick; displacement along the dash | each applied; an understudy's recorded Vanish does the same; the pull never moves an understudy |
| C6 The decoy | Vanishing Cabinet | a decoy entity; the critics' turn rule reads it | only critics already turned walk at it; critics walking to the box office ignore it; it falls when its time is up; it protects nothing at the box office (the guard in miniature) |
| C7 Flagged | The Magnet, Curtain Call, Duller Critics, The Usherette | the kiter of T37 is in the guard already; each card is measured against it, one at a time | each applied; the guard holds with the card on, or the finding is written and the card stays out |
| C8 Chorus kinds | Longer Arms, Quicker Chorus, Inky Chorus, Choking Chorus, Learns the Part, Encore for the Chorus | after T32; Learns the Part at `GoOn`; Encore for the Chorus after T24 | each applied to every understudy and never to the magician; the newest understudy's cards; the extra encore's tick |
| C9 Evolutions | the ten above, one a pull request | a rule: the second copy of an evolving card replaces its count with the evolution; `Describe` shows the reveal | one copy behaves as the card; two as the evolution; three adds; the hash covers it |

### Where the texts live

Today a card's sentence is assembled in the view from the tuning's numbers. With fifty-six cards that is a long `switch`. The recommendation: a `cards.json` beside `tuning.json`, parsed as strictly (an unknown or a missing key refused), with the name, the sentence with its keys, the flavour, the family, the rarity and where it is taken, one entry per kind; the view fills the keys from the tuning and draws the rest. One test: every kind of card has an entry and every entry names a kind. The alternative, the texts in code, is the owner's to choose; it is smaller now and larger every ticket after.

## 7. The offer, in short

An encore offers three self cards, never two of a kind; the program after an act offers one chorus card, only after an act with an encore (decision 26 as the owner changed it). Rarity weights are a hypothesis: common 60 %, uncommon 35 %, rare 5 %, no rare before act three. The pace of the encores as measured (T26, T37), not as the enemies-and-cards document guessed: four or five in act one, next to none in act two, three to seven in each act after for a player who goes out; some forty a performance. With about thirty-four self cards in the pool, the third encore of a kind is common by the middle of a performance, so evolutions fire by themselves; a cap on copies (§8) decides whether they fire too often.

## 8. Open questions for the owner

1. The six existing cards: rename to the stage names here, or keep today's names and use the stage names only for the new ones?
2. Is the flavour line printed on the card, or does it live in the catalogue only? The card's panel is small; the flavour may fit under the name in a smaller face.
3. A limit on copies: none (as today), or three a kind, with the third the evolution's extra?
4. `cards.json` or the texts in code?
