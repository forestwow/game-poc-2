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

## The other views

`ludo sprite rotate --image <still> --camera-rotation 180` (the back) and `90` (the side, facing right; the game mirrors it for the left). Half a credit each, and the figure holds.

## The walk

`ludo sprite animate --initial-image <view> --motion-prompt "walking towards the viewer" --model forge-pixel --duration 1 --frames 9 --frame-size <n>` ("walking away from the viewer", "walking to the right" for the other views). One and a half credits each.

- `forge-pixel` keeps the pixel grid; the default model does not, and costs six times as much.
- The sheet must come back with one file pixel to a sprite pixel, and every figure to one measure: the magician is 64 sprite pixels for its three units, so a figure of two units is about 43. `--frame-size 64` gives the magician that, and `--frame-size 32` a figure of two units (the size names the sprite's width more than its height). A sheet that came back larger is exported again for nothing with `ludo sprite adjust --spritesheet-url <its url> --frame-size 32`.
- The sheet is three rows of three frames, each frame cropped to the same size within a sheet.

## Into the game

The file goes to `art/ludo/sprites/` under the name `ReadSheet` is given in `UnderstudiesGame.LoadContent`, and the stills that were returned to `art/ludo/sprites/raw/`.
