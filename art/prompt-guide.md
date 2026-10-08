# How a figure is made

The look is the owner's pick of plan decision 25: pixel art with a thick dark outline, of figures that are not plain people. Everything here was made with ludo.ai through its CLI (`ludo`); every prompt and setting that was run is in the `manifest.json` files under `art/ludo/`.

## The idea of a figure

A figure is an ordinary theatre person or thing with one part that is something else: the magician's head is a dove in an upturned top hat, the critic's head is an eye under a bowler, the stagehand's head is a spotlight, the box office's window is a mouth. Say the odd part first and say how large it is, or it is lost at this size. No artist is named in a prompt.

## The still

`ludo image create --image-type sprite --art-style "16-Bit" --perspective "High Angle" --aspect-ratio ar_1_1 --n 2`, the subject and then:

> Pixel art game sprite with a thick dark outline and a small limited palette, standing upright and facing the viewer, seen from the front and slightly from above as characters are in a top-down action RPG. Full body from head to feet, single subject, centred, no text, no ground, no shadow.

- Two tries and pick one. The picture is 768 pixels with twelve to a sprite pixel.
- A building comes back isometric with `High Angle`: ask for "a flat front elevation, no side walls visible" with `--perspective "Any perspective"`.
- `Top-Down` gives a view straight down onto the hat: not used.
- A wide prop (the rival's understudy, a cut-out on a board) comes back smaller than a person unless it is asked to "fill the whole frame from the top edge to the bottom edge": `art/ludo/rival/manifest.json` has both tries.

- "Where his face should be a fan of tickets, with a flat cap on top" came back as a man in a cap with a pale ruff, which reads as a fur collar: a part that replaces the head is asked for with "no head" first, then what stands "straight up out of his collar", with a thing it is held like ("a hand of playing cards") and what it is not ("no face, no hat, no hair, no fur"). `art/ludo/scalper/manifest.json` has both tries.
- A figure with a wide part is not made smaller by `--frame-size`: 32 is the smallest, and the scalper's sheet came back in frames of 50 by 62 for it (the stagehand's are 26 by 46).

## The other views

`ludo sprite rotate --image <still> --camera-rotation 180` (the back) and `90` (the side, facing right; the game mirrors it for the left). Half a credit each, and the figure holds.

## The walk

`ludo sprite animate --initial-image <view> --motion-prompt "walking towards the viewer" --model forge-pixel --duration 1 --frames 9 --frame-size <n>` ("walking away from the viewer", "walking to the right" for the other views). One and a half credits each.

- `forge-pixel` keeps the pixel grid; the default model does not, and costs six times as much.
- The sheet must come back with one file pixel to a sprite pixel, and every figure to one measure: the magician is 64 sprite pixels for its three units, so a figure of two units is about 43. (Those are units of the sheets' own measure: since plan T42 the game draws a walking figure 1.6 times that against the stage.) `--frame-size 64` gives the magician that, and `--frame-size 32` a figure of two units (the size names the sprite's width more than its height). A sheet that came back larger is exported again for nothing with `ludo sprite adjust --spritesheet-url <its url> --frame-size 32`.
- The sheet is three rows of three frames, each frame cropped to the same size within a sheet.

## The set

- The floor: `--image-type texture --perspective "Top-Down" --art-style "16-Bit"`, asked to be calm, dark and low in contrast so that figures stand out on it. It comes back 1024 pixels, sixteen to a sprite pixel, and goes round at every edge.
- The curtain: `--image-type sprite-tiling-horizontal`, "seen flat from the front ... repeating seamlessly left to right". It goes round left to right; its own pixel is twelve file pixels and it is drawn three times that size to be as tall as the back wall.
- A door or a lamp is a still like a building (see above). The lit door is `ludo image edit` of the shut one ("keep the exact same door frame, size, position and pixel art style ..."): the edit keeps the shape and loses the pixel grid inside the doorway, which is seen only close up.

## An effect

`ludo image create --image-type sprite-vfx --art-style "16-Bit" --aspect-ratio ar_1_1 --n 2`, the effect "captured at its peak, centred, seen flat, no character, no text, no ground", then `ludo sprite animate --image-type sprite-vfx --model forge-pixel --duration 1 --frames 9 --no-loop --frame-size <32 or 64>` with a motion like "bursts outward ... and everything fades away to nothing". A burst that flies apart comes back well; a still shape (a star) hardly moves and does not fade, so the game fades it.

## A card's picture

A card has a picture of what it does, not an emblem (plan T47): the pierce is a line through two critics with the card out the far side, the ricochet a V that bounces off one critic to the next.

`ludo image create --image-type item-icon --art-style "16-Bit" --perspective "Any perspective" --aspect-ratio ar_1_1 --n 2`, the scene and then:

> Pixel art game icon with a thick dark outline and a small limited palette of black, white, red, blue-grey and gold, big bold simple shapes that read at a very small size, seen flat, centred and filling the frame, on a plain empty background, no text, no letters, no numbers, no border, no frame.

- It comes back as a figure does: 768 pixels, exactly twelve to a sprite pixel from the corner, on a transparent background.
- Say where each thing is in the frame, not what it has done. "The card has already passed through the first critic" came back with the card short of both; "one long straight line runs across the whole picture, through the chest of the first critic and of the second, and at its end on the right a card flies on" came back right. A path is asked for by its shape ("a V-shaped path, like a ball bouncing ... diagonal lines only, one sharp corner"): "a zigzag" came back as right angles.
- The critic is described in full every time ("a short stout man in a blue-grey overcoat whose whole head is one huge white eyeball with a blue iris under a small black bowler hat"), and "complete from bowler hat to shoes" when there are three, or the outer two come back as loose eyeballs.
- A thing is long or large only against something small: the long arm reads once a tiny critic stands at its end.
- A scene with figures fills a third of its frame and a hand or a hat fills it, whatever "filling the frame" and "from edge to edge" ask. All nine are kept at one scale, so the scenes are small on a card: the impact on one critic is 32 by 34 sprite pixels beside an arm of 60 by 62.
- Afterimages and motion blur come back half see-through, and one of them came back smoothed, with no pixel grid at all: look before picking.
- Into the game: one pixel from each twelve-pixel cell (no smoothing; first check that every cell is one colour), cut to what is drawn, set in the middle of a transparent square of 62 pixels, and saved as `art/ludo/cards/<the card's name in the Card enum, lower case>.png`. `art/ludo/cards/manifest.json` says which candidate each is.

## Into the game

The file goes to `art/ludo/sprites/` under the name `ReadSheet` is given in `UnderstudiesGame.LoadContent`, and the stills that were returned to `art/ludo/sprites/raw/` (the set's to `art/ludo/set/raw/`, an effect's to `art/ludo/effects/raw/`).
