# Chorus synergies and the notices: ticket-shaped specs

- **Status:** written 2026-10-08 at the owner's word ("the two for now sound great, go"), after the owner floated a loose idea of borrowing from Balatro. Of the four borrowings weighed in conversation (synergies between understudies, a house rule per act, a scoring ceremony, the deck as ammunition), the owner took the two cheap ones: **synergies between understudies as chorus cards** (Balatro's jokers, which play with each other) and **a ceremony after the act** (Balatro's scoring, with the count-up and the rising pitch). Both leave the core verb alone.
- **Numbers in the plan:** this document's T26, T27, T28, T29 and T30 are the plan's T28, T32, T33 to T36, T30 and T31 (`docs/plan-prototype.md`, "The road from here"); the plan's T26 is another ticket.
- **What this is:** five tickets in the plan's own shape (`docs/plan-prototype.md` §5), with their tests, their `tuning.json` keys and what each needs in Core, so they can be lifted into the plan when the owner names them. Nothing here is agreed until then. No code.
- **Facts from the code that shaped them** (`main` at a7098df): `ChorusCards` is one count, so there is one kind of chorus card; `TickEvent` carries a kind and a position and nothing else, so the view cannot tell a magician's kill from an understudy's without comparing hit points (`Juice.cs:132–141`); Resonance was cut from the vision because it rewarded the live magician for standing near a ghost; the guard of decision 22 and the two pinned hashes apply after every change of rule.

## 0. Five rules the specs keep

1. **A synergy is between understudies only.** The live magician's position, range or cards never enter a condition. That closes Resonance's flaw and leaves the guard alone: the orbit player earns no chorus card (with the price of review ticket R4), and the doors player earns from a synergy only where its routes overlap.
2. **A synergy replays.** Understudies are on rails; a condition reads their places, ranges and cards on the tick, never an input. The understudy of act four does in act nine what it did in act five.
3. **A synergy is seen between the ghosts**, not only counted: a thread, a baton, a gold strike.
4. **The ceremony is view only.** No rule, no state in Core, no change to the hash, no reward in v1 (the stars are cosmetic). One press skips it. About five seconds.
5. **Every ticket:** the test before the rule in Core, the guard after, the hashes pinned again, the smallest thing.

## 1. The tickets

### T26 Events say who. *Needed by T28 and T29.*

`Throw`, `Hit` and `Kill` carry who threw (`Thrower`: the magician, or an understudy by its index in `Simulation.Understudies`) and the critic's id (`CriticId`); `ApplauseDropped` carries the critic's id too. Events are not in the state hash, so the hash does not move.

- Tests: a throw by the magician says so, and one by an understudy says which; a hit and a kill name the critic; the two pinned performances end in the same hashes as before.
- The view stops comparing hit points to find a hit (`Juice.Feed`) and reads the event. An understudy's throw gets a quieter, duller flick and sound than the magician's, so a past self is heard apart from the present one (the skeleton review, §3.3).
- The `TickEventKind.Throw` doc says the magician or an understudy.

### T27 Chorus cards as kinds. *After T23 (the encore).*

`ChorusCard` is an enum with counts, as `SelfCards` is for self cards; the chorus card there is today (a louder chorus, +0.5 damage for every understudy) becomes its first kind. After every act that has another, the program offers one kind of chorus card, drawn from the stream `RngStream.Program`. Under decision 26 the chorus card is free after every act; this ticket takes the price of review ticket R4 (a chorus card only after an act with at least one encore or at least one piece of applause). If the owner keeps it free, the four synergies still hold and the guard is run with that in the pull request.

- Tests: counts per kind; one kind offered; the louder chorus is the kind it was (the card tests of `CardTests` hold on the renamed kind); the hash covers every count; the hashes pinned again.
- The view: `Describe` names each kind and what it changes; the program's panel shows the one chorus card.

### T28 Four synergies. *After T26 and T27. One ticket each, in this order. The kiter of review ticket R1 in the guard before the first, or a note in each pull request that it was not.*

Each is a chorus kind: taken once, it is on for every understudy there is and will be, in every act after. A second copy adds again where the table says.

| Kind | Condition (understudies only) | Effect | Seen | `tuning.json` | Needs in Core |
|---|---|---|---|---|---|
| **Crossfire** | a card of understudy A strikes a critic that a card of another understudy B struck within `crossfireWindow` (1 s) | that strike does double; a second copy triple | a gold burst in place of the usual, and a thread from A to B for half a second | `crossfireWindow` | on a critic: which understudy last struck it and on which tick |
| **Hand-off** | a critic leaves the range of understudy A and within `handOffWindow` (2 s) enters the range of understudy B | B's first card at that critic does triple; a second copy makes it the first two cards | a baton over the critic, carried from A's side to B's; a fanfare on the strike | `handOffWindow` | on a critic: whose range it left and when; "in range" is the throw range the understudy has now |
| **Chorus line** | two understudies stand within `chorusLineDistance` (2 units) of each other on the tick of a throw | each of the two throws one more card in that throw; a second copy, two more | their cards fly as pairs; a thin thread between the two while they are close | `chorusLineDistance` | the pairs of understudies, at most 36 for nine, once a tick |
| **Duet** | a critic is in the range of two understudies at once | both throw `duetSpeed` (25 %) more often while it is; a second copy, 50 % | their bursts in the colours of both acts | `duetSpeed` | the same pairs; a critic's count of understudies in range |

What each rewards in the route: Crossfire, overlapping ranges on one lane; Hand-off, a relay along a path (the route of act six picks up where the route of act three left off); the Chorus line, running a past route again to double it, which is a real decision against covering a new door; the Duet, standing two ghosts a throw apart on a door. None of them rewards the live magician for being anywhere.

- Tests, each kind: the condition met gives the effect; the condition not met (the window passed, the distance exceeded, one understudy only) gives nothing; **the live magician in the condition's place never counts** (the magician's card struck first, the critic left the magician's range, the magician stands beside the understudy: no effect), which is the guard in miniature; two copies add as the table says; the hash covers the new fields on a critic; the guard holds on the tuned numbers and the two hashes are pinned again. If no tuning holds the guard with a synergy on, that is the finding, as T20 says.
- Later, a rare kind to weigh after these four: **the standing ovation**, where a critic that falls to an understudy's card within the range of two other understudies gives every understudy +0.25 damage to the end of the act. A snowball, flagged for the guard.

### T29 The notices. *After T26. View only.*

The ceremony after an act, between the act's last tick and the chorus card's program, in `UnderstudiesGame` and `Juice.cs`, driven by the events of the act tallied in the view (nothing is added to Core).

- **The curtain falls:** one picture from ludo.ai (the first visible curtain), slid down over the stage in half a second; the stage dims under it. The same picture rises at the next act's curtain, so the rewind of the understudies happens behind a real curtain and the signature moment of the vision exists at last.
- **The notices:** a newspaper column slides in from the side, headed with the act's number. A row for each understudy, in the colour of its act, named by its act ("Act 3's you"), with its kills this act counted up from zero, a tick of sound a kill with the pitch rising, the whole count scaled so that it never takes more than about three seconds however many kills there were. Then the magician's row, bright, the same way. Then the applause picked up, in pink. Then the box office's row, red if it was struck, with what it lost. Last, **the house**: one to five stars from a formula that is cosmetic and says so in its comment (the share of the act's entrants that fell, the box office untouched, the applause picked up), and the audience: a murmur for one star up to a roar for five.
- **A press** of any key, button or stick skips to the column complete and the stars shown. Then the program of the chorus card opens over it (T27), or, with no card, the stage waits between two acts as today.
- **Sound:** two new recipes in `Sound.cs`, a count-up tick with a pitch that rises with each, and a crowd (shaped noise) that is a murmur or a roar by the stars. Named constants at the top, as the others.
- **Capture:** `--capture` on an act's last tick shows the column half counted; a capture a hundred ticks later shows it complete with its stars.
- No test. The owner judges by eye and ear.
- **The rule that protects it:** the ceremony gives nothing in v1 (no card, no applause, no score kept), and it takes at most five seconds plus the chorus pick. If it ever pays, it is a rule and goes to Core with a test.

Why it is more than juice: it names every understudy after every act and shows what it did. A player who reads "Act 3's you: 14 critics" has been told what an understudy is without a caption.

### T30 Stop 2 asked. *After T29.*

A capture of act five with four understudies on the stage and the notices of act four beside it, and the owner's written answer to stop 2's question, which has none on record: does the figure read as "past me"? One pull request with the frames and the answer in the plan's §6.

## 2. Order and dependencies

T26 → T27 → T28 (four pull requests), and in parallel T26 → T29 → T30. The kiting player of review ticket R1 belongs in the guard before T28; without it each T28 pull request says so.

## 3. What was weighed and not taken (from the same conversation)

- **A house rule per act** (Balatro's boss blinds): a rule twist drawn from the seed each act, such as a blackout or a door that moves. Cheap, and the direct answer to the owner's "is the stage always the same?". Not now; a candidate after G1.
- **The deck as ammunition**: the magician's throw draws from a real deck whose suits have effects, and cards add, remove and foil cards in it. The one borrowing that would change the upgrade space from the inside, and the most faithful to a magician. A second system, so it waits for G1; a one-page concept is owed so it is not lost, with a prior-art check first.
- **Multipliers without a cap** and **banking applause** between acts: not taken; the first breaks balance, the second breaks the guard.
