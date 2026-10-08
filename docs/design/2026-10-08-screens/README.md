# The screens: the owner's direction

- **Status:** brought in by the owner on 2026-10-08 as a Claude design canvas (https://claude.ai/artifact/WZwvzTxGPM3DoNZP5U7mHQ, private to the owner), with the words: a simple design of a few screens to implement, to be treated as a loose way and a direction, not the final look. The four artboards are kept here as the canvas held them (`Main.dc.html`, `HUD.dc.html`, `Encore.dc.html`, `Program.dc.html`, `canvas.json`). They are records, not pages that open: their pictures are addresses inside the canvas and stand for the game's own sprites (`art/ludo/sprites/`).
- **What this file is:** the four screens in words, for whoever builds a ticket from them, and what in them is new against the game as it is. The plan (`docs/plan-prototype.md`, "The screens") has the tickets and their order.
- **The canvas's own note:** main menu → act (HUD) → the encore opens in the fight when the applause bar fills → the program between acts, paid for by an encore. The stage is 48 × 27 units on 1280 × 720, sprites at two screen pixels a sprite pixel. The numbers are one mid-show state: act 6 of 10, three encores taken, the back door just opened.

## What all four share

- **1280 × 720**, the stage filling it; the curtain along the back wall 80 pixels high (three units) with a thin gold line under it; the boards dimmed a little; footlights along the front edge.
- **Two faces:** Pixelify Sans (600 and 700) for names, numbers and headings; Atkinson Hyperlegible for sentences. Both are under the Open Font License.
- **Words on the stage carry a dark outline** (ink `#1e161e`, two pixels; four on the title).
- **The palette:** ground `#18121c`; ink `#1e161e`; gold `#fae278` (headings, the act); cream `#ece4d2` for words and `#faf6ec` for paper; applause pink `#ff48c4`, lighter `#ff8ad8` and `#ffe2f6`; box-office green `#84cc6e`; the Vanish's blue `#96d6ec`; an understudy's tint by its act: `#78c8aa`, `#d68478`, `#b094d6`, `#a0c46e`, `#dc96be`, `#6ebece`.
- **Paper and ink:** a card, a chip and a menu entry are cream paper with an ink border and a hard ink shadow down and to the right, no blur.
- **Sprites are drawn at two screen pixels to a sprite pixel** on this window: the magician 64 × 132, a critic 64 × 92, the rival 120 × 136, the box office 108 × 108. That is larger against the stage than the game draws them now (one screen pixel a sprite pixel at 1024 × 576).
- **An understudy** is the magician's shape filled with its act's tint, with the magician's own picture showing through at under half strength, the whole a little see-through: a coloured cardboard figure, clearly not the magician. Its route is a dashed line in its tint.

## 1. The main menu

- Left, on the boards under a taller curtain: a small pink line "Tonight, at the street corner"; the title "The Understudies" in gold, large and outlined; one sentence: "Where you run is what you build. Every act you play leaves a cardboard you that plays it again."
- Under it a column of four entries, each with a name and a small note at its right: **Perform** ("ten acts of 75 s", the gold one, larger); **Perform the same seed** ("last night's show"); **Matinee or gala** ("easier or harder"); **Quit** ("Esc").
- Right: the magician with five understudies in their tints lined up behind it, shadows under them.
- At the foot, one line of the controls: "Move: WASD or the stick · Vanish: Space or A · Go on: Enter or Start · R starts a new show".
- New against the game: the screen itself (the game starts in a show today); playing a seed again; a choice of difficulty.

## 2. The HUD in an act

- **Top left:** "Act 6 of 10" large in gold; under it "86 critics still to come".
- **Top middle:** a small pink label "APPLAUSE TO THE NEXT ENCORE"; the bar, pink with a light upper edge and a faint glow; beside it "23 / 30" large.
- **Top right:** the clock large; under it "3 encores tonight".
- **Under the curtain, centred:** one line that announces ("Act 6. The back door opens.").
- **On the stage:** the quiet floor's dashed circle round the box office; the box office with its bar and its number above it; the magician with its hit points as eight small square pips (lost ones dark) and the Vanish's bar under them; the understudies as above with their dashed routes; applause as pink diamonds with a glow; thrown cards as small cream cards with a fading trail.
- **Bottom left:** "YOU HOLD" and a row of paper chips, a card's name and "×2" each; a chorus card's chip is pale green.
- **Bottom right:** "THE CAST ON STAGE" and a row of small squares, one an understudy, numbered by its act and filled with its tint; the act being recorded is a dark square with a dashed border.
- New against the game: the pips, the chips, the cast's squares, the label and the count on the bar, the encores' count, the announcing line; an understudy's look and its dashed route.

## 3. The encore

- The act as it stood, under a dark wash of six tenths; the top row stays: the bar full and glowing ("6 / 6"), the clock greyed with "the clock stands" under it.
- "Encore!" large in pink; under it one sentence: "Take a card, and the act goes on. 3 encores so far; this one was paid with 6 pieces of applause."
- **Three cards** in a row, 280 × 330: a gold head strip with the card's key ("1") and "For the magician"; the name large; what it does in two lines; at the foot, small, "you have 1" or "you have none yet". The chosen one is lifted and ringed in pink with a glow.
- Under them: "4 s left: then the leftmost card is taken for you", a pink bar running down, and the keys drawn as small key caps: ← → choose · Enter or Space takes · or press 1 2 3 · gamepad: the stick and A.
- Bottom left: "YOU HOLD" and the chips. The footlights dimmed.
- New against the game: the cards' size and look, the lift and the ring, the key caps, the greyed clock. The flavour line of decision 29 has no place in the design and needs one (under the name).

## 4. The program

- The stage under the same wash; "Act 6 of 10 / is over" top left, the clock greyed at 0:00 with "the stage stands".
- "The program" large in teal; under it: "Two encores tonight paid for a card for the whole chorus."
- **Left, two panels** (dark, a cream border): "ACT 6 ON THE BOOKS" with four rows (applause picked up 23; encores taken 2; box office left 352 / 400; the magician: stood to the end); and "UNDERSTUDY 6 JOINS THE CAST": its figure in its tint, "It will run tonight's route, every act from now on, with the cards you had:" and their chips.
- **Middle:** the chorus card, larger (320 × 380), its head strip teal with "1" and "For the chorus", ringed in teal; under it "9 s left: then it is taken for you" and a teal bar.
- **Right, two panels:** "NEXT: ACT 7 OF 10" with a sentence on what is coming ("All three doors are open. The rival's understudy comes tougher by the act. The encore costs 7 pieces now."); and "THE CAST AT THE CURTAIN": the squares, and "Every understudy snaps back to its mark when the curtain rises."
- At the foot: Enter or Space takes the card · gamepad: A · then Enter or Start raises the curtain.
- New against the game: the four panels (what the act did, who joins, what comes next, the cast). The first overlaps the notices of T30, which count the same things up as a ceremony: the two are one screen when T30 is taken.
