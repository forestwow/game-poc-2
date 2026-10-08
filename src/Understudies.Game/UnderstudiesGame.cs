using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Understudies.Core;

// World positions are Core's vectors; MonoGame's own Vector2 appears only where SpriteBatch asks for one.
using Vector2 = System.Numerics.Vector2;

namespace Understudies.Game;

// The base class is spelled out because `Game` alone means this namespace here.
internal sealed partial class UnderstudiesGame : Microsoft.Xna.Framework.Game
{
    // The window the game opens in, and the size of a captured frame (plan T42).
    private const int WindowWidth = 1280;
    private const int WindowHeight = 720;

    // The sprites are pixel art, and there are two measures (plan T42). The set's (the curtain, the doors, the
    // box office, the footlights) is in units whatever the window: 64 sprite pixels are three units.
    private const float SetPixelsPerUnit = 64f / 3f;

    // The walking figures' own measure is this many times the set's, in units whatever the window as well. At 1
    // a figure is as large against the stage as before plan T42: the magician's 66 pixels are 3.09 units, one
    // screen pixel to a sprite pixel in a window 1024 wide and one and a quarter, uneven, in the 1280 the game
    // opens in. At 1.6 it is the design's: 4.95 units, two screen pixels to a sprite pixel at 1280 (the frames of
    // art/frames/s2-design). What is drawn of a figure, at its chest or over its head is this many times as
    // large; no footprint, range or radius is. Which of the two stays is the owner's to say (plan T42).
    internal const float FiguresMeasure = 1f;

    /// <summary>How far below the line its critics enter on a door's picture has its foot, in units.</summary>
    private const float DoorSill = 0.3f;

    // A figure's sprite pixel is made a whole number of screen pixels where its measure asks for one to within
    // this much of a screen pixel, and is the measure's own otherwise.
    private const float WholeWithin = 0.05f;

    // The boards have no footprint to keep: they are laid at a whole number of screen pixels to a sprite pixel,
    // the nearest to this many times the set's measure (two in a window 1280 wide, as the design has them).
    private const float FloorMeasure = 1.6f;

    // The box office's bar is this far above its roof: clear of the critics that stand behind it.
    private const float BoxOfficeBarLift = 2f * FiguresMeasure;

    // The line under the curtain, along the top of the boards: four screen pixels in a window 1280 wide.
    private const float GoldLine = 0.15f;

    // The boards are dimmed with this, so that what stands on them stands out.
    private static readonly Color BoardsDim = new Color(10, 6, 14) * 0.35f;

    // How strong the footlights are: whole in an act, and less while the stage stands for an encore, and for the
    // program or between two acts.
    private const float FootlightsInAnEncore = 0.5f;
    private const float FootlightsBetweenActs = 0.35f;

    // A walk goes through its frames at WalkFramesPerSecond for a figure that goes WalkReferenceSpeed units a
    // second, and faster or slower as the figure does: a stagehand's feet run and a critic's plod. Never slower
    // than WalkSlowest or faster than WalkFastest, where a walk stands or blurs.
    private const float WalkFramesPerSecond = 12f;
    private const float WalkReferenceSpeed = 4f;
    private const float WalkSlowest = 4f;
    private const float WalkFastest = 20f;
    private const float ShadowOpacity = 0.3f;
    private const float FootlightGap = 4f;

    // A thrown card spins as it flies, this many turns for a unit flown, with a dark edge this wide about its face.
    private const float ThrownCardWidth = 0.7f;
    private const float ThrownCardHeight = 0.5f;
    private const float ThrownCardSpin = 0.11f;
    private const float ThrownCardEdge = 0.1f;

    // The flick of light at a throwing hand grows to this wide as it goes out.
    private const float FlickSize = 1.1f * FiguresMeasure;

    // A card flies at the height of a critic's chest.
    private const float ThrownCardLift = 1f * FiguresMeasure;

    // A circle is laid of this many strips: there is no texture but the one pixel.
    private const int DiscStrips = 48;

    // The same in every capture, so that a frame can be compared with the one before.
    private const ulong CaptureSeed = 1;

    // How tall the words on the back wall are and the number at the box office's bar, in world units: the font is
    // asked for at that many screen pixels, whatever the window's size.
    private const float WordsHeight = 1.2f;
    private const float NumberHeight = 0.8f;

    // The one caption of a performance, over the head of the first understudy and going where it goes: through
    // the curtain of the second act and for the first seconds of that act, long enough to be read. Where the
    // understudy stands beside the magician the words are clear of the magician's head: their middle is
    // CaptionLift above the head.
    private const string Caption = "Your understudy. It repeats your act one, every act.";
    private const float CaptionHeight = 1f;
    private const float CaptionLift = 1.4f;
    private const float CaptionTimeInTheAct = 3f;

    // An understudy is a coloured cardboard figure (plan T43, the design's numbers): the magician's shape filled
    // with its act's tint, the magician's own picture over that at UnderstudyPicture, and the whole this much
    // there. One that is on the magician's spot is there less, since the magician is the one to be found there:
    // UnderstudyOpacityOnTheSpot within UnderstudyDimmedWithin of the magician's feet, and more with every step
    // away, itself again UnderstudyDimmedOver further out. It is read off the two places, and is no state.
    private const float UnderstudyOpacity = 0.85f;
    private const float UnderstudyPicture = 0.45f;
    private const float UnderstudyOpacityOnTheSpot = 0.55f;
    private const float UnderstudyDimmedWithin = 0.5f;
    private const float UnderstudyDimmedOver = 1f;

    // The magician is drawn as if it stood this far nearer the viewer, so an understudy on its very line is
    // behind it, and a critic that close as well. The smallest that does it: nine understudies' own hairs
    // (UnderstudyAhead) are 0.036, and a picture's hair over its fill is a little more.
    private const float MagicianAhead = 0.05f;

    // Understudies that stand on one line are drawn one whole figure over the other, the newest act's in front:
    // each is this far nearer the viewer than the act's before. Two at one depth would have their fills laid
    // first and their pictures both over those, and be nearly the magician.
    private const float UnderstudyAhead = 0.004f;

    // Where an understudy is about to go: the next RouteAhead seconds of its route as a broken line on the floor
    // in its tint, RouteOpacity at its feet and fading to nothing at the far end. The design's line: three screen
    // pixels wide in a window 1280 wide, four on and ten off. It is measured along every sixth place of the
    // route: a tenth of a second, under a unit at the magician's speed. While the stage stands between two acts,
    // and under the curtain, it is the first RouteAhead seconds of the route: where it will go when the
    // next act begins.
    private const float RouteAhead = 2.5f;
    private const float RouteOpacity = 0.4f;
    private const float RouteWidth = 3f / 26.667f;
    private const float RouteDash = 4f / 26.667f;
    private const float RouteGap = 10f / 26.667f;
    private const int RouteStride = 6;

    // The act an understudy is of, as a number in its tint over its head. Understudies that stand on one spot
    // have theirs side by side, MarkApart from one to the next.
    private const float MarkHeight = 0.9f;
    private const float MarkLift = 0.45f;

    // Where there is no floor over the head for a mark (at the back door), it is this far under the feet: under
    // the magician's pips and the Vanish's bar under those.
    private const float MarkUnderTheFeet = PipsDrop + PipSize + VanishBarGap + VanishBarHeight + 0.55f;
    private const float MarkApart = 0.6f;
    private const float MarkSameSpot = 0.5f;

    // The moment after a Vanish in which nothing hurts the magician is seen: the magician is this much there and
    // washed with the smoke of its cloud, and is itself again when a touch counts again.
    private const float InvulnerableOpacity = 0.7f;

    // A piece of applause is a diamond with a pale heart: this wide, and never fainter than ApplauseFaintest, so
    // that a piece about to go is still seen to be there. Its lower tip is on the place it lies, which is the
    // place the magician's feet must come near.
    private const float ApplauseSize = 0.75f;
    private const float ApplauseLift = ApplauseSize * 0.7f;
    private const float ApplauseFaintest = 0.25f;

    // The game's words are in two faces, read from the repository's own files so that every machine shows the
    // same (plan decision 30, T40): Pixelify Sans Bold for a card's name, on the main menu the title and the
    // entries' names (plan T53) and on the poster the night's name and a new kind's (plan T55), and
    // Atkinson Hyperlegible for a sentence, a label and every number that is read in a glance (the act, the clock, the
    // counts, what is held): at the sizes the game has, Pixelify's 5 is read as an S and its 2 as an 8. The files
    // are in the order of Face, under the fonts' folder; the game does not start without every one of them.
    // ponytail: Pixelify Sans is smoothed at every size. Its static weights are not drawn on one grid of whole
    // pixels: a weight is made by fattening each square over its neighbours (Bold's squares are 127 by 136 font
    // units at a pitch of 88 across and 85 up; Regular's 101 at 90.5), so no size puts every edge on a whole
    // screen pixel, and the text library's rasteriser has no setting that draws without smoothing. A face cut as
    // a bitmap at the sizes the screens use would end it.
    internal static readonly string[] FaceFiles =
    [
        Path.Combine("pixelify-sans", "PixelifySans-Bold.ttf"),
        Path.Combine("atkinson-hyperlegible", "AtkinsonHyperlegible-Regular.ttf"),
    ];

    // A card's picture (plan T47) is the file named by the lower case of the card's name in the Card enum, under
    // the cards' folder: a card added to the enum is asked for its picture by that name, and the game does not
    // start without it.
    internal static readonly string[] CardFiles =
        [.. Enum.GetValues<Card>().Select(card => card.ToString().ToLowerInvariant() + ".png")];

    // Words on the stage carry an outline in ink, this wide in world units: two screen pixels in the window of
    // 1280 the game opens in, and two as well in one 1024 wide, where 1.6 is rounded; never less than one, and
    // never more than one part in SmallestOutlined of the words' height.
    private static readonly Color OutlineInk = new(30, 22, 30);
    private const float OutlineWidth = 0.075f;
    private const float SmallestOutlined = 12f;

    // The floor about the box office where a fall earns no applause: a shade over the boards and a broken line,
    // both quiet. The rows are an eighth of a unit tall, and the line is cut into this many stretches, every
    // other one drawn.
    private const float QuietFloorRow = 0.125f;
    private const int QuietFloorStretches = 72;
    private const float QuietFloorLine = 0.1f;
    private static readonly Color QuietFloorShade = Color.Black * 0.12f;
    private static readonly Color QuietFloorEdge = new Color(236, 228, 210) * 0.24f;

    private static readonly Color Surround = new(24, 18, 28);
    private static readonly Color BackWall = new(52, 40, 62);
    private static readonly Color HitPoints = new(132, 204, 110);
    private static readonly Color HitPointsLost = new(30, 22, 30);
    private static readonly Color Magician = new(250, 226, 120);
    private static readonly Color VanishBar = new(150, 214, 236);
    private static readonly Color CloudPuff = new(236, 232, 244);
    private static readonly Color CriticStunnedBody = new(168, 180, 212);

    // The headliner (plan T41) is the rival's cut-out washed with this, so that the one of three hundred hit
    // points and more is told from the one of thirty.
    private static readonly Color HeadlinerWash = new(226, 58, 72);
    private static readonly Color ThrownCardFace = new(250, 246, 236);
    private static readonly Color ScrapOfPaper = new(244, 238, 222);
    private static readonly Color Words = new(236, 228, 210);

    // Applause is the one thing on the stage in this colour: the pieces on the floor and the bar they fill.
    private static readonly Color ApplauseGlow = new(255, 72, 196);
    private static readonly Color ApplauseHeart = new(255, 226, 246);

    // An understudy's card is a card, a little duller than the magician's own.
    private static readonly Color UnderstudysCardFace = new(190, 184, 172);
    private static readonly Color CardEdge = new(28, 20, 30);

    // What an understudy is tinted by, the first for the first act's: nine dusty colours told apart at a glance,
    // none of them the magician's yellow or a critic's blue, and neighbours far apart.
    private static readonly Color[] UnderstudyTints =
    [
        new(120, 200, 170),
        new(214, 132, 120),
        new(176, 148, 214),
        new(160, 196, 110),
        new(220, 150, 190),
        new(110, 190, 206),
        new(206, 166, 110),
        new(150, 160, 150),
        new(196, 120, 150),
    ];

    // What the view keeps for one show is set back in StartAgain, every field of it: one added here, or to the
    // program's screen, is added there.
    private SimulationClock _clock = new();
    private readonly string? _capturePath;
    private readonly int _captureTicks;

    private readonly string _spritesFolder;
    private readonly string _fontsFolder;
    private readonly string _cardsFolder;

    // By Figure and then by Facing; a figure with one view has that one alone.
    private readonly Sheet[][] _sheets = new Sheet[Enum.GetValues<Figure>().Length][];

    // The set's two pictures that are laid side by side: the boards of the floor and the curtain of the back wall.
    private Sheet _floor = null!;
    private Sheet _curtain = null!;

    // What a card is seen to do where it strikes and where its critic falls: each a sheet played through once.
    private Sheet _hitBurst = null!;
    private Sheet _killBurst = null!;

    // The view's own time, in seconds: what a walk's frames are counted by.
    private float _walkClock;

    // Where the magician last went: it keeps facing there while it stands.
    private Vector2 _magicianToward = Vector2.UnitY;
    private readonly Sound _sound;
    private Simulation _simulation;
    private Juice _juice;
    private bool _captured;

    // The keys and the gamepad as this frame found them and as the frame before did: a press is the one down and
    // the other up (Pressed, PadPressed, StepAcross, StepDown), on every screen.
    private KeyboardState _keys;
    private GamePadState _pad;
    private KeyboardState _keysBefore;
    private GamePadState _padBefore;
    private bool _vanishAsked;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;

    // Where the frame being drawn has the stage: the screen pixels of a world unit, and of the stage's corner,
    // and the two as what a batch in world units is begun with.
    private float _scale;
    private Vector2 _corner;
    private Matrix _worldToScreen;

    // A card's picture (plan T47), by the card's number: read unsmoothed, a file pixel to a sprite pixel.
    private readonly Texture2D[] _cardPictures = new Texture2D[CardFiles.Length];

    // One for each Face, in its order.
    private readonly FontSystem[] _faces = new FontSystem[FaceFiles.Length];

    /// <summary>With a <paramref name="capturePath"/> the game does not play: it saves one frame there and exits.</summary>
    /// <param name="tuning">The plain tuning, which a night is composed from.</param>
    /// <param name="nights">The nights, at least one, every one of which composes with the tuning.</param>
    /// <param name="night">The one night to play whatever the progress says, or null.</param>
    /// <param name="rules">The house rules to play whatever night is played with, by their names.</param>
    /// <param name="progressPath">The file of the player's progress, or null for a game that keeps none.</param>
    /// <param name="captureTheMenu">The frame is the main menu's, and no tick is played for it.</param>
    /// <param name="captureThePoster">The frame is the poster's page of that night, and no tick is played for it.</param>
    /// <param name="spritesFolder">Where the figures' images are.</param>
    /// <param name="fontsFolder">Where the two faces' files are.</param>
    /// <param name="cardsFolder">Where the cards' pictures are.</param>
    public UnderstudiesGame(
        Tuning tuning,
        IReadOnlyList<Night> nights,
        int? night,
        IReadOnlyList<string> rules,
        string? progressPath,
        string? capturePath,
        int captureTicks,
        bool captureTheMenu,
        int? captureThePoster,
        string spritesFolder,
        string fontsFolder,
        string cardsFolder)
    {
        _spritesFolder = spritesFolder;
        _fontsFolder = fontsFolder;
        _cardsFolder = cardsFolder;
        _plain = tuning;
        _nights = nights;
        _askedNight = night;
        _askedRules = rules;
        _progressPath = progressPath;
        _progress = LoadTheProgress();

        // A capture of a show that asks for no night plays the plain tuning as it is, as before the nights; the
        // game and the menu's frame have a night, the newest that is open.
        _night = captureThePoster ?? night ?? (capturePath is not null && !captureTheMenu ? null : Unlocked[^1]);
        Tuning played = _night is { } number ? Night.Compose(tuning, nights, number) : tuning;
        _simulation = new Simulation(played, capturePath is null ? (ulong)DateTime.UtcNow.Ticks : CaptureSeed);
        _boxOfficeAtTheActsStart = _simulation.BoxOfficeHitPoints;
        _juice = new Juice(capturePath is null ? Random.Shared : new Random((int)CaptureSeed));
        _sound = new Sound(silent: capturePath is not null);
        _capturePath = capturePath;
        _captureTicks = captureTicks;

        // The game opens on its menu (plan T53). A capture is of a show from its first tick, unless it asks for
        // the menu. The show made above stands behind the menu for its numbers and is never played: "Perform"
        // makes another.
        _onTheMenu = capturePath is null || captureTheMenu;
        _onThePoster = captureThePoster is not null;
        _guardLeft = MenuGuardTime;
        _ = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = WindowWidth,
            PreferredBackBufferHeight = WindowHeight,
        };
        Window.Title = "The Understudies";
        Window.AllowUserResizing = true;
        IsMouseVisible = true;
        // One Update per drawn frame with the real time since the last one; the clock makes the fixed ticks of it.
        IsFixedTimeStep = false;
    }

    /// <summary>The numbers the simulation runs on now: the view keeps no copy, so a reload reaches the drawing too.</summary>
    private Tuning Tuning => _simulation.Tuning;

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);

        // A walk is a sheet of three rows of three frames, a file pixel to a sprite pixel. The side view faces
        // right. The stagehand, the rival's understudy and the scalper have one walk each, toward the viewer (a
        // cut-out has no back and no side, so it is not mirrored), and the box office is one picture as the tool
        // returned it: twelve file pixels to one of its own, drawn six to a sprite pixel so that it is as wide as
        // its four units.
        _sheets[(int)Figure.Magician] =
            [ReadSheet("magician-down.png", 3), ReadSheet("magician-up.png", 3), ReadSheet("magician-side.png", 3)];
        _sheets[(int)Figure.Critic] =
            [ReadSheet("critic-down.png", 3), ReadSheet("critic-up.png", 3), ReadSheet("critic-side.png", 3)];
        _sheets[(int)Figure.Stagehand] = [ReadSheet("stagehand-down.png", 3)];
        _sheets[(int)Figure.Rival] = [ReadSheet("rival-down.png", 3)];
        _sheets[(int)Figure.Scalper] = [ReadSheet("scalper-down.png", 3)];
        _sheets[(int)Figure.BoxOffice] = [ReadSheet("box-office.png", 1, block: 6f)];

        // The set (plan T07d). The doors are stills like the box office and drawn at its pixel size, a footlight at
        // the figures'. The floor's picture is sixteen file pixels to a sprite pixel. The curtain's own pixel is
        // twelve, and it is drawn four to a sprite pixel, which makes it as tall as the back wall.
        // ponytail: the box office's and the doors' pixels are so twice the figures' and the curtain's three times.
        // A still comes back from the tool at one size whatever it shows; pictures made to the figures' measure
        // (through the tool's animation export, as the walks are) would end it.
        _sheets[(int)Figure.ShutDoor] = [ReadSheet("door-shut.png", 1, block: 6f)];
        _sheets[(int)Figure.OpenDoor] = [ReadSheet("door-open.png", 1, block: 6f)];
        _sheets[(int)Figure.Footlight] = [ReadSheet("footlight.png", 1, block: 12f)];
        _floor = ReadSheet("floor.png", 1, block: 16f);
        _curtain = ReadSheet("curtain.png", 1, block: 4f);
        _hitBurst = ReadSheet("hit-burst.png", 3);
        _killBurst = ReadSheet("kill-burst.png", 3);

        // The batch blends colours that are already multiplied by their alpha.
        for (int card = 0; card < CardFiles.Length; card++)
        {
            _cardPictures[card] = Texture2D.FromFile(
                GraphicsDevice, Path.Combine(_cardsFolder, CardFiles[card]), DefaultColorProcessors.PremultiplyAlpha);
        }

        for (int face = 0; face < FaceFiles.Length; face++)
        {
            _faces[face] = new FontSystem();
            _faces[face].AddFont(File.ReadAllBytes(Path.Combine(_fontsFolder, FaceFiles[face])));
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if (_capturePath is not null)
        {
            // Run calls Update once before its loop and Exit only ends the loop after a frame: Update comes again.
            if (!_captured)
            {
                _captured = true;
                Capture(_capturePath);
                Exit();
            }

            return;
        }

        _keys = Keyboard.GetState();
        _pad = GamePad.GetState(PlayerIndex.One);

        // F5 reads tuning.json and nights.json again and the next tick runs on the new numbers, composed for the
        // night that is played or chosen. A file that does not parse, or nights without that night, leave the
        // numbers as they were. In a show the acts' entries stay as they were planned (a new scale of the budget
        // or new kinds count from the next show) and everything else counts at once, the number of acts too.
        if (Pressed(Keys.F5) && TuningFile.Read() is var (plain, nights))
        {
            if (nights.Any(night => night.Number == _night))
            {
                _plain = plain;
                _nights = nights;
                ChooseTheNight(_night!.Value);

                // On other numbers the last show's seed is another show: there is no "same show again" of it.
                _played = null;
            }
            else
            {
                Console.Error.WriteLine($"Night {_night} is not in nights.json any more: the numbers stay as they were");
            }
        }

        // M mutes the sound, and M again brings it back.
        if (Pressed(Keys.M))
        {
            _sound.Muted = !_sound.Muted;
        }

        // A press does one thing, by the screen and the phase this frame began in: the Space that takes "Perform"
        // finds a show when it is done, and must not be a Vanish as well; the Enter that takes a card finds the
        // stage between two acts, and must not go on as well, and the Space that takes an encore's card finds the
        // act going on, and must not be a Vanish either.
        if (_onTheMenu)
        {
            ChooseOnTheMenu();
        }
        else if (_onThePoster)
        {
            ChooseOnThePoster();
        }
        else if (Pressed(Keys.Escape) || PadPressed(Buttons.Back))
        {
            // Esc, or the gamepad's Back, is the way back to the menu, from an act as from a show that is over: the show is given up.
            GiveUp();
            ShowTheMenu();
        }
        else if ((Pressed(Keys.R) || PadPressed(Buttons.Y)) && _simulation.Phase is Phase.Ovation or Phase.Closed)
        {
            // R, or the gamepad's Y, when the show is over is the way back to the poster (plan T55): of the night
            // this show has just opened, or else of the night that was played. A pad has no key for R in a show.
            ShowThePoster(_opened ?? _night!.Value);
        }
        else if (Pressed(Keys.R))
        {
            // R in a show starts the same night again at once, on the numbers of now.
            GiveUp();
            RaiseTheCurtain();
        }
        else if (IsOffered)
        {
            ChooseInTheProgram();
        }
        else
        {
            // Space or the gamepad's A is one Vanish for each press. The press waits for a tick to take it: a
            // frame may run no tick, and it must not be lost, or several, and it must not be asked of each.
            if (Pressed(Keys.Space) || PadPressed(Buttons.A))
            {
                _vanishAsked = true;
            }

            // Enter or the gamepad's Start goes on to the next act. The simulation takes it between two acts only,
            // and the view not while a card just taken is still shown: a press meant for the program that came a
            // moment after its time ran out must not begin the next act.
            if ((Pressed(Keys.Enter) || PadPressed(Buttons.Start)) && _takenLeft <= 0f)
            {
                GoOn();
            }
        }

        _keysBefore = _keys;
        _padBefore = _pad;

        // The hit-stop: while the moment of a Vanish holds the world still, a frame's time is dropped. The clock
        // gets none of it and no tick is run, so the frame is drawn as the one before it was.
        // Nothing is played under the menu or the poster: no tick, no juice, no sound of the stage.
        double frameSeconds = gameTime.ElapsedGameTime.TotalSeconds;
        if (!_onTheMenu && !_onThePoster && !_juice.Holds((float)frameSeconds))
        {
            // Between two acts the stage stands until the player goes on, and a performance that is over stands as
            // it ended until R: there Step changes nothing. In an encore and in the program it counts the offer's
            // time.
            Vector2 move = ReadMove(_keys, _pad);
            int ticks = _clock.Advance(frameSeconds);
            for (int i = 0; i < ticks; i++)
            {
                Tick(new MagicianInput(move, _vanishAsked));
                _vanishAsked = false;
            }

            // The juice goes on whatever the phase: between two acts, as after the show, what flew still settles,
            // a flash ends and a shaken stage comes to rest, while the simulation stands.
            _juice.Advance((float)frameSeconds);
            _walkClock += (float)frameSeconds;
        }

        _takenLeft = MathF.Max(0f, _takenLeft - (float)frameSeconds);
        _guardLeft = MathF.Max(0f, _guardLeft - (float)frameSeconds);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Only an act has a next tick to draw towards, and the curtain, whose rewind goes on between its ticks.
        DrawTheFrame(_simulation.Phase is Phase.Act or Phase.Curtain ? _clock.Alpha : 1f);
        base.Draw(gameTime);
    }

    /// <summary>The frame of now: the menu, or the stage <paramref name="alpha"/> between its last two ticks.</summary>
    private void DrawTheFrame(float alpha)
    {
        if (_onTheMenu)
        {
            DrawTheMenu();
        }
        else if (_onThePoster)
        {
            DrawThePoster();
        }
        else
        {
            DrawStage(alpha);
        }
    }

    /// <summary>Goes on to the next act, where the simulation lets it: the last act's offer is the view's no more.</summary>
    private void GoOn()
    {
        if (_simulation.Phase == Phase.BetweenActs)
        {
            _offered = [];
            _simulation.GoOn();
            _boxOfficeAtTheActsStart = _simulation.BoxOfficeHitPoints;
            _actKills.Clear();
        }
    }

    /// <summary>
    /// A new show, and nothing of the last one left in the view: no part of a tick owed, no Vanish asked for, the
    /// magician facing the viewer, no card lit or shown, and no menu and no poster. What is the player's and not
    /// the show's stays: the keys that are down, and the sound's mute. The one way a show is made: the one that
    /// is played (<see cref="RaiseTheCurtain"/>) and the one that stands behind the menu and the poster and is
    /// never played. With no <paramref name="seed"/> it is a show nobody has seen: its seed is the time, which
    /// Core never reads.
    /// </summary>
    private void StartAgain(ulong? seed = null)
    {
        _onTheMenu = false;
        _onThePoster = false;
        _simulation = new Simulation(Tuning, seed ?? (ulong)DateTime.UtcNow.Ticks);
        _juice = new Juice(Random.Shared);
        _clock = new SimulationClock();
        _walkClock = 0f;
        _magicianToward = Vector2.UnitY;
        _vanishAsked = false;
        _offered = [];
        _highlighted = 0;
        _taken = 0;
        _takenLeft = 0f;
        _guardLeft = 0f;
        _opened = null;
        _boxOfficeAtTheActsStart = _simulation.BoxOfficeHitPoints;
        _actKills.Clear();
    }

    /// <summary>A key that went down in this frame: one held down counts once.</summary>
    private bool Pressed(Keys key) => _keys.IsKeyDown(key) && !_keysBefore.IsKeyDown(key);

    /// <summary>A button of the gamepad that went down in this frame.</summary>
    private bool PadPressed(Buttons button) => _pad.IsButtonDown(button) && _padBefore.IsButtonUp(button);

    /// <summary>
    /// A frame's step to the side, one to the right or one to the left: the arrows, A and D, the d-pad, or the
    /// left stick, which counts when it comes to a side as a key does when it goes down.
    /// </summary>
    private int StepAcross()
    {
        static int Lean(GamePadState pad) =>
            pad.ThumbSticks.Left.X > StickLean ? 1 : pad.ThumbSticks.Left.X < -StickLean ? -1 : 0;

        int lean = Lean(_pad);
        return (lean != Lean(_padBefore) ? lean : 0)
            + (Pressed(Keys.Right) || Pressed(Keys.D) || PadPressed(Buttons.DPadRight) ? 1 : 0)
            - (Pressed(Keys.Left) || Pressed(Keys.A) || PadPressed(Buttons.DPadLeft) ? 1 : 0);
    }

    /// <summary>A frame's step down or up, as <see cref="StepAcross"/>: W and S. A stick pushed up reports +Y.</summary>
    private int StepDown()
    {
        static int Lean(GamePadState pad) =>
            pad.ThumbSticks.Left.Y > StickLean ? -1 : pad.ThumbSticks.Left.Y < -StickLean ? 1 : 0;

        int lean = Lean(_pad);
        return (lean != Lean(_padBefore) ? lean : 0)
            + (Pressed(Keys.Down) || Pressed(Keys.S) || PadPressed(Buttons.DPadDown) ? 1 : 0)
            - (Pressed(Keys.Up) || Pressed(Keys.W) || PadPressed(Buttons.DPadUp) ? 1 : 0);
    }

    private static Vector2 ReadMove(KeyboardState keys, GamePadState pad)
    {
        var stick = pad.ThumbSticks.Left;

        float Held(Keys letter, Keys arrow) => keys.IsKeyDown(letter) || keys.IsKeyDown(arrow) ? 1f : 0f;

        // A stick pushed up reports +Y; the stage's y grows downward.
        return new Vector2(
            stick.X + Held(Keys.D, Keys.Right) - Held(Keys.A, Keys.Left),
            -stick.Y + Held(Keys.S, Keys.Down) - Held(Keys.W, Keys.Up));
    }

    /// <summary>
    /// One tick, and what a tick's events drive is fed them at once: the next tick starts the list afresh, and a
    /// frame may run several.
    /// </summary>
    private void Tick(MagicianInput input)
    {
        bool wasOffered = IsOffered;
        Phase before = _simulation.Phase;
        _simulation.Step(input);
        if (_simulation.Phase != before && _simulation.Phase is Phase.Ovation or Phase.Closed)
        {
            // The show has ended, in its ovation or by the box office's fall: the night is played.
            Remember();
        }

        _juice.Feed(_simulation);
        _sound.Feed(_simulation);
        foreach (TickEvent happened in _simulation.Events)
        {
            // The act's books (plan T50): whose card felled a critic, as the event says it.
            if (happened.Kind == TickEventKind.Kill)
            {
                int who = happened.Thrower - TickEvent.TheMagician;
                while (_actKills.Count <= who)
                {
                    _actKills.Add(0);
                }

                _actKills[who]++;
            }
        }

        if (IsOffered && !wasOffered)
        {
            // An encore or a program opens: the view keeps its offer, which the simulation empties with the pick.
            // A tick only counts an offer's time, so one never opens out of another.
            _offered = [.. _simulation.Offer];
            _highlighted = 0;
            _guardLeft = _simulation.Phase == Phase.Encore ? EncoreGuardTime : 0f;
        }
        else if (wasOffered && !IsOffered)
        {
            // The offer's time ran out in this tick, and the simulation took the leftmost card.
            Acknowledge(0);
        }
    }

    /// <summary>Plays the doors player for the capture's ticks, draws the frame it ends on and saves it as a PNG.</summary>
    private void Capture(string path)
    {
        // Nobody is here to press a key, so the capture is played by the doors player of the guard (plan decision
        // 22; `ScriptedPlayers.cs`, the tests' own file, compiled into the game as well): it goes out to the
        // newest door, picks up its applause and takes its cards by the players' one order. The capture's seed is one
        // the guard plays, and on the committed numbers that performance reaches the ovation. The guard lets four
        // seeds of twenty lose: if a retuning makes this one lose, CaptureSeed gets one that wins.
        // Every act opens with its curtain, whose ticks are counted here with the rest.
        const int second = Simulation.TicksPerSecond;
        Phase before = _simulation.Phase;
        int eatenToldOf = 0;
        for (int i = 0; i < _captureTicks; i++)
        {
            // A card is taken and the next act gone on to before the tick, and not after it, so neither takes any
            // of a capture's ticks, and a capture that ends on the tick an encore or a program opens shows that
            // screen. The pick is the simulation's own and not the view's, so no capture shows a card just taken.
            _simulation.Pick(Core.Tests.ScriptedPlayers.Choose(_simulation.Offer));
            GoOn();

            // Read again after the pick, so that an encore that opens on the tick after another is told as well.
            before = _simulation.Phase;
            Tick(Core.Tests.ScriptedPlayers.Doors(_simulation));

            // Where a screen is, for whoever wants a frame of it: the console is told the tick each opens on.
            if (_simulation.Phase != before && _simulation.Phase is Phase.Encore or Phase.Program or Phase.Ovation or Phase.Closed)
            {
                Console.WriteLine($"Capture: {_simulation.Phase} in act {_simulation.Act} on tick {i + 1}");
            }

            // And the tick a scalper first eats a piece on in each act (plan T57): a frame a few ticks after it
            // has the scalper and its puff of tickets.
            if (_simulation.Act > eatenToldOf && _simulation.Events.Any(happened => happened.Kind == TickEventKind.ApplauseEaten))
            {
                eatenToldOf = _simulation.Act;
                Console.WriteLine($"Capture: a piece eaten in act {_simulation.Act} on tick {i + 1}");
            }

            // Each tick is a sixtieth of a second to the juice, as in a game that runs a tick a frame: the frame
            // shows the scraps and the flashes that would be on the screen at that moment. Nothing holds a capture
            // still: it is counted in ticks.
            _juice.Advance(1f / second);
            _walkClock += 1f / second;
        }

        using var frame = new RenderTarget2D(GraphicsDevice, WindowWidth, WindowHeight);
        GraphicsDevice.SetRenderTarget(frame);
        DrawTheFrame(alpha: 1f);
        GraphicsDevice.SetRenderTarget(null);

        using FileStream file = File.Create(path);
        frame.SaveAsPng(file, frame.Width, frame.Height);
    }

    /// <param name="alpha">How far between the last two ticks to draw what moves: 1 is the last tick itself.</param>
    private void DrawStage(float alpha)
    {
        // A shake moves the whole picture, bars and all. The wall and the floor are laid as far past the stage's
        // edge as the stage is moved, so the strip of the window a shake uncovers is stage and not the surround.
        FitTheStage(_juice.Shake);
        Vector2 past = Vector2.Abs(_juice.Shake);
        Matrix worldToScreen = _worldToScreen;
        Vector2 magicianFeet = Vector2.Lerp(_simulation.MagicianPreviousPosition, _simulation.MagicianPosition, alpha);

        // The spotlight night (plan T56): the dark is made first, and laid over the figures below.
        // Read once for the frame: Lit asks it for every enemy.
        bool dark = _lightsDown = LightsAreDown;
        if (dark)
        {
            MakeTheDark(magicianFeet, past);
        }

        GraphicsDevice.Clear(Surround);
        LayTheSet(past, Tuning.StageFloorTop, curtainMeasure: 1f, GoldLine);

        // What lies flat on the floor, and the doors, which are of the set and hide nobody: whoever stands at a
        // door is drawn over it.
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: worldToScreen);
        DrawTheQuietFloor();
        for (int i = 0; i < Tuning.StageDoors.Count; i++)
        {
            // A door's foot is on the line its critics enter on, a little below it (plan T49): whoever enters
            // stands in the lit frame with its feet on the sill, and the door at the back wall stands up in the
            // curtain. `Tuning.Parse` keeps a door's width on the stage. It is lit while it is open.
            DrawFigure(
                _simulation.DoorIsOpen(i) ? Figure.OpenDoor : Figure.ShutDoor,
                Tuning.StageDoors[i].Position + new Vector2(0f, DoorSill));
        }

        foreach (Understudy understudy in _simulation.Understudies)
        {
            DrawRouteAhead(understudy);
        }

        foreach (Cloud cloud in _simulation.Clouds)
        {
            // A pale patch the size of the cloud's circle, the fainter the less of its time it has left.
            float thick = Math.Clamp(cloud.TicksLeft / (Tuning.VanishCloudTime * Simulation.TicksPerSecond), 0f, 1f);
            FillDisc(cloud.Position, Tuning.VanishCloudRadius, CloudPuff * (0.5f * thick));
        }

        foreach (var body in _juice.Bodies)
        {
            // A critic that fell lies where it fell and fades away, under the feet of whoever stands there.
            // Whatever it was: the juice kept its kind from when it stood.
            DrawFigure(
                FigureOf(body.Kind), body.Position, white: body.White, fallen: true, opacity: body.Opacity * Lit(body.Position), tint: WashOf(body.Kind));
        }

        _spriteBatch.End();

        // What stands on the floor: the lower on the screen, the later it is drawn (Depth says how late).
        _spriteBatch.Begin(SpriteSortMode.FrontToBack, samplerState: SamplerState.PointClamp, transformMatrix: worldToScreen);

        // The box office is drawn from halfway between the middle of its circle and the circle's front. A critic
        // that touches the circle from in front then overlaps the foot of the box, one at a side stands against its
        // wall, and one in front of a corner is not hidden behind it.
        Vector2 boxOfficeFeet = Tuning.BoxOfficePosition + new Vector2(0f, Tuning.BoxOfficeSize / 4f);

        // The batch keeps no order between equal depths, and the magician's mark is on this very line: the box
        // office stands a hair behind its foot line, so whoever stands exactly on that line is in front.
        const float hair = 0.001f;
        DrawFigure(Figure.BoxOffice, boxOfficeFeet - new Vector2(0f, hair), white: _juice.BoxOfficeWhite);
        Vector2 magicianStep = _simulation.MagicianPosition - _simulation.MagicianPreviousPosition;
        if (magicianStep != Vector2.Zero)
        {
            _magicianToward = magicianStep;
        }

        DrawFigure(
            Figure.Magician,
            magicianFeet,
            white: _juice.MagicianWhite,
            fallen: _simulation.MagicianHasFallen,
            opacity: _simulation.MagicianIsInvulnerable ? InvulnerableOpacity : 1f,
            tint: _simulation.MagicianIsInvulnerable ? CloudPuff : null,
            toward: _magicianToward,
            walking: magicianStep != Vector2.Zero,
            speed: Tuning.MagicianSpeed,
            ahead: MagicianAhead);
        foreach (Understudy understudy in _simulation.Understudies)
        {
            // The magician's own figure through a treatment, and never a figure of its own: cardboard in the
            // colour of the act it came from. It walks as its route goes, out of step with the understudy of the
            // act before.
            if (understudy.IsOnStage)
            {
                Vector2 step = understudy.Position - understudy.PreviousPosition;
                Vector2 feet = Feet(understudy, alpha);

                // The one that is being told what it is (the caption's) is neither dimmed nor behind the
                // magician: it is what the words point at, and it must be seen where the two stand together.
                bool captioned = CaptionIsUp && understudy.Act == 1;
                float away = (Vector2.Distance(feet, magicianFeet) - UnderstudyDimmedWithin) / UnderstudyDimmedOver;
                DrawFigure(
                    Figure.Magician,
                    feet,
                    opacity: captioned
                        ? UnderstudyOpacity
                        : MathHelper.Lerp(UnderstudyOpacityOnTheSpot, UnderstudyOpacity, Math.Clamp(away, 0f, 1f)),
                    cardboard: TintOf(understudy),
                    ahead: (understudy.Act * UnderstudyAhead) + (captioned ? 2f * MagicianAhead : 0f),
                    toward: step,
                    walking: step != Vector2.Zero,
                    speed: Tuning.MagicianSpeed,
                    beat: understudy.Act * 4);
            }
        }

        foreach (Critic critic in _simulation.Critics)
        {
            Vector2 feet = Vector2.Lerp(critic.PreviousPosition, critic.Position, alpha);
            Figure figure = FigureOf(critic.Kind);

            // What is dark is not drawn, and is still there.
            float seen = Lit(feet);
            if (seen <= 0f)
            {
                continue;
            }

            // A critic that stands is at its work: it faces the box office.
            Vector2 step = critic.Position - critic.PreviousPosition;
            DrawFigure(
                figure,
                feet,
                pale: critic.IsStunned,
                white: _juice.CriticWhite(critic.Id),
                opacity: seen,
                tint: WashOf(critic.Kind),
                toward: step != Vector2.Zero ? step : Tuning.BoxOfficePosition - critic.Position,
                walking: step != Vector2.Zero,
                speed: Tuning.EnemyKinds[Math.Min(critic.Kind, Tuning.EnemyKinds.Count - 1)].Speed,
                beat: critic.Id);
        }

        // In the light a card is among the figures: one behind a figure is behind it.
        if (!dark)
        {
            TheThrownCards();
        }

        _spriteBatch.End();

        // Over everything: the scraps in the air, then a performance that is over goes dark, and the box office's
        // hit points are a bar above it, a critic's height above, clear of the heads of the critics who stand
        // behind the box.
        if (dark)
        {
            LayTheDark();
        }

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: worldToScreen);

        // On the spotlight night every card is drawn over the dark, as its effects below are: the understudies'
        // cards are what shows where the dark is not empty. Nothing is sorted here, so a card is over every figure.
        if (dark)
        {
            TheThrownCards();
        }

        // The cards' effects go under the applause: a splash of ink must not hide the piece its kill drops.
        foreach (var effect in _juice.Effects)
        {
            // The flick is a diamond of light that grows and goes out. A burst is a sheet played through once, and
            // the burst of a hit goes out as it plays: its picture does not fade by itself.
            // An understudy's is smaller and dimmer, in the face of its card: the past throws beside the present.
            if (effect.Kind is Juice.Effect.Flick or Juice.Effect.UnderstudyFlick)
            {
                bool understudys = effect.Kind == Juice.Effect.UnderstudyFlick;
                float wide = FlickSize * (0.4f + (0.6f * effect.Through)) * (understudys ? Juice.UnderstudyFlickSize : 1f);
                Color light = understudys ? UnderstudysCardFace * Juice.UnderstudyFlickOpacity : Color.White;
                FillTurned(effect.Middle, new Vector2(wide), MathF.PI / 4f, light * (1f - effect.Through));
                continue;
            }

            // A card's burst is a ring that opens to the radius of the rule and goes out.
            if (effect.Kind is Juice.Effect.Ring or Juice.Effect.UnderstudyRing)
            {
                float radius = Tuning.CardBurstRadius * (Juice.RingFirstShare + ((1f - Juice.RingFirstShare) * effect.Through));
                Color line = (effect.Kind == Juice.Effect.Ring ? ThrownCardFace : UnderstudysCardFace)
                    * (Juice.RingOpacity * (1f - effect.Through));
                float stretch = MathF.Tau / Juice.RingStretches;
                for (int i = 0; i < Juice.RingStretches; i++)
                {
                    float turn = (i + 0.5f) * stretch;
                    FillTurned(
                        effect.Middle + (new Vector2(MathF.Cos(turn), MathF.Sin(turn)) * radius),
                        new Vector2(radius * stretch, Juice.RingLine),
                        turn + (MathF.PI / 2f),
                        line);
                }

                continue;
            }

            bool hit = effect.Kind == Juice.Effect.HitBurst;
            if (!hit && effect.Through < Juice.KillFlashShare)
            {
                // Black ink on dark boards is not seen: the splash has a pale flash under it as it opens.
                float left = 1f - (effect.Through / Juice.KillFlashShare);
                FillDisc(effect.Middle, Juice.KillFlashRadius, ScrapOfPaper * (Juice.KillFlashOpacity * left));
            }

            Sheet sheet = hit ? _hitBurst : _killBurst;
            int frames = sheet.Columns * sheet.Columns;
            int frame = Math.Min(frames - 1, (int)(effect.Through * frames));
            var source = new Rectangle(
                sheet.First.X + (frame % sheet.Columns * sheet.First.Width),
                sheet.First.Y + (frame / sheet.Columns * sheet.First.Height),
                sheet.First.Width,
                sheet.First.Height);
            _spriteBatch.Draw(
                sheet.Image,
                effect.Middle,
                source,
                Color.White * (hit ? 1f - effect.Through : 1f),
                0f,
                new Microsoft.Xna.Framework.Vector2(source.Width / 2f, source.Height / 2f),
                FigurePixel,
                SpriteEffects.None,
                0f);
        }

        foreach (Applause piece in _simulation.ApplauseOnTheFloor)
        {
            // Over every figure, so that a crowd does not hide what is there to be fetched, and the dimmer the
            // less of its time it has left.
            float left = Math.Clamp(piece.TicksLeft / (Tuning.ApplauseTime * Simulation.TicksPerSecond), 0f, 1f);
            float seen = ApplauseFaintest + ((1f - ApplauseFaintest) * left);
            Vector2 middle = piece.Position - new Vector2(0f, ApplauseLift);
            FillTurned(middle, new Vector2(ApplauseSize), MathF.PI / 4f, ApplauseGlow * seen);
            FillTurned(middle, new Vector2(ApplauseSize / 2.5f), MathF.PI / 4f, ApplauseHeart * seen);
        }

        foreach (var scrap in _juice.Scraps)
        {
            FillTurned(scrap.Middle, Juice.ScrapSize, scrap.Turn, ScrapOfPaper * scrap.Opacity);
        }

        // A closed show goes dark, and an ovation does not: the two are told apart at a glance. Under an offer's
        // cards and the program's panels the stage is as dark (the design's six tenths): where a crowd stands is
        // still seen, and not what it is.
        if (_simulation.Phase == Phase.Closed)
        {
            Fill(Vector2.Zero, Tuning.StageSize, Color.Black * 0.6f);
        }
        else if (StageStands)
        {
            // What is at a figure's feet is under the wash with its figure while the stage stands: the cards and
            // the panels lie over the stage, and a bar that was bright beside one would be read with it.
            TheBarsAtTheFeet();
            Fill(Vector2.Zero, Tuning.StageSize, Color.Black * ProgramDim);
        }

        // The footlights, along the stage's front edge, in front of all that stands on it and of the wash: dimmer
        // while the stage stands.
        float footlights = !StageStands ? 1f : _simulation.Phase == Phase.Encore ? FootlightsInAnEncore : FootlightsBetweenActs;
        for (float x = FootlightGap / 2f; x < Tuning.StageSize.X; x += FootlightGap)
        {
            DrawFigure(Figure.Footlight, new Vector2(x, Tuning.StageSize.Y), opacity: footlights);
        }

        // The box office's bar is not drawn while the stage stands, nor its number: the row of what is held, the
        // cards and the panels are where they would be (plan T48, T50), and the act's books have the number.
        var bar = new Vector2(Tuning.BoxOfficeSize, 0.4f);
        Vector2 barTopLeft = boxOfficeFeet
            - new Vector2(bar.X / 2f, Tuning.BoxOfficeSize + BoxOfficeBarLift + bar.Y);
        if (!StageStands)
        {
            FillBar(barTopLeft, bar, _simulation.BoxOfficeHitPoints / Tuning.BoxOfficeHitPoints, HitPoints);
            TheBarsAtTheFeet();
        }

        // On the curtain, the way to the next encore.
        DrawTheApplauseBar();
        _spriteBatch.End();

        void TheThrownCards()
        {
            foreach (ThrownCard card in _simulation.ThrownCards)
            {
                // The card's position is the point of the floor it is over.
                Vector2 below = Vector2.Lerp(card.PreviousPosition, card.Position, alpha);
                Color face = card.ThrownByMagician ? ThrownCardFace : UnderstudysCardFace;
                Vector2 heart = below - new Vector2(0f, ThrownCardLift + (ThrownCardHeight / 2f));

                // Its turn is told by how far it has flown, so a card left in the air when the world stands hangs still.
                float turn = Vector2.Distance(below, card.ThrownFrom) * ThrownCardSpin * MathF.Tau;
                var size = new Vector2(ThrownCardWidth, ThrownCardHeight);
                FillTurned(heart, size + new Vector2(2f * ThrownCardEdge), turn, CardEdge, Depth(below));
                FillTurned(heart, size, turn, face, MathF.Min(1f, Depth(below) + 0.0001f));

                // A card thrown on this tick has not flown yet and has no way to trail along.
                Vector2 flown = card.Position - card.PreviousPosition;
                if (flown == Vector2.Zero)
                {
                    continue;
                }

                // The trail: a streak from the middle of the card back along its flight, so that a card that is in the
                // air for an eighth of a second is seen. It never reaches back past where the card was thrown from,
                // or where it last turned (plan T25): the trail of a card that turns bends there with it.
                Vector2 behind = card.ThrownFrom - below;
                if (behind == Vector2.Zero)
                {
                    continue;
                }

                Vector2 back = Vector2.Normalize(behind) * MathF.Min(Juice.TrailLength, behind.Length());
                FillTurned(
                    heart + (back / 2f),
                    new Vector2(back.Length(), Juice.TrailWidth),
                    MathF.Atan2(back.Y, back.X),
                    face * Juice.TrailOpacity,
                    Depth(below));
            }
        }

        // The HUD's shapes at a figure's feet (plan T45): the magician's pips and its Vanish, and a headliner's
        // hit points (plan T46), the one enemy whose hit points the player counts: a small bar in its own red.
        // ponytail: full is what one enters with in this act, so one left from the act before is never shown
        // full. Keep what it entered with on the critic when that is seen.
        void TheBarsAtTheFeet()
        {
            DrawTheMagiciansBars(magicianFeet);
            var headlinerBar = new Vector2(1.6f, 0.25f) * FiguresMeasure;
            foreach (Critic critic in _simulation.Critics.Where(critic => critic.Kind == 3))
            {
                // As everywhere a critic's kind is read: a tuning read again (F5) may have fewer kinds than the stage.
                EnemyKind headliner = Tuning.EnemyKinds[Math.Min(critic.Kind, Tuning.EnemyKinds.Count - 1)];

                // A headliner that the dark hides has no bar to give it away.
                Vector2 feet = Vector2.Lerp(critic.PreviousPosition, critic.Position, alpha);
                if (Lit(feet) < 0.5f)
                {
                    continue;
                }

                FillBar(
                    feet + new Vector2(-headlinerBar.X / 2f, 0.3f),
                    headlinerBar,
                    critic.HitPoints / (headliner.HitPoints + (headliner.HitPointsPerAct * (_simulation.Act - headliner.FromAct))),
                    HeadlinerWash);
            }
        }

        DrawWords(alpha, besideTheBar: barTopLeft + new Vector2(bar.X + 0.3f, bar.Y / 2f));
    }

    /// <summary>
    /// Where the frame being drawn has the stage, moved by a <paramref name="shake"/>: the stage keeps its shape,
    /// fitted to the window, and the rest is left dark.
    /// </summary>
    private void FitTheStage(Vector2 shake)
    {
        Viewport viewport = GraphicsDevice.Viewport;
        float scale = MathF.Min(viewport.Width / Tuning.StageSize.X, viewport.Height / Tuning.StageSize.Y);
        Vector2 corner = (new Vector2(viewport.Width, viewport.Height) - (Tuning.StageSize * scale)) / 2f;

        // To a whole screen pixel: a part of one would change which of the set's uneven pixels are the wide ones,
        // and a shaken set would shimmer.
        corner += shake * scale;
        _scale = scale;
        _corner = new Vector2(MathF.Round(corner.X), MathF.Round(corner.Y));
        _worldToScreen = Matrix.CreateScale(scale, scale, 1f) * Matrix.CreateTranslation(_corner.X, _corner.Y, 0f);
    }

    /// <summary>
    /// The set, in a batch of its own: the back wall with its curtain hung to the floor's top, a gold line under
    /// it, and the boards of the floor below, laid <paramref name="past"/> the stage's edges. The curtain is
    /// <paramref name="curtainMeasure"/> times its size in an act: the menu's is taller.
    /// </summary>
    private void LayTheSet(Vector2 past, float floorTop, float curtainMeasure, float goldLine)
    {
        // The two pictures are laid side by side, so this batch lets a picture go round at its edges. Every batch
        // that draws a sprite takes its pixels as they are: a sprite is never smoothed.
        _spriteBatch.Begin(samplerState: SamplerState.PointWrap, transformMatrix: _worldToScreen);
        var across = new Vector2(Tuning.StageSize.X + (2f * past.X), 0f);
        Fill(-past, across with { Y = floorTop + past.Y }, BackWall);
        Lay(
            _floor,
            new Vector2(-past.X, floorTop),
            across with { Y = Tuning.StageSize.Y - floorTop + past.Y },
            MathF.Max(1f, MathF.Round((_scale * FloorMeasure / SetPixelsPerUnit) - 0.01f)) / _scale);
        Fill(
            new Vector2(-past.X, floorTop),
            across with { Y = Tuning.StageSize.Y - floorTop + past.Y },
            BoardsDim);
        float curtainHeight = curtainMeasure * _curtain.First.Height / (_curtain.Block * SetPixelsPerUnit);
        Lay(
            _curtain,
            new Vector2(-past.X, floorTop - curtainHeight),
            across with { Y = curtainHeight },
            curtainMeasure / SetPixelsPerUnit);
        Fill(new Vector2(-past.X, floorTop), across with { Y = goldLine }, Magician);
        _spriteBatch.End();
    }

    /// <summary>The colour of the act an understudy came from.</summary>
    private static Color TintOf(Understudy understudy) => TintOfAct(understudy.Act);

    /// <summary>The colour of an act's understudy, the one that is still to join among them.</summary>
    private static Color TintOfAct(int act) => UnderstudyTints[(act - 1) % UnderstudyTints.Length];

    /// <summary>
    /// The first understudy is told once what it is, as the act it first appears in begins (vision 13): through
    /// the curtain of the second act and the first seconds of that act. How long is read off the act's time
    /// left: it is no rule and no state.
    /// </summary>
    private bool CaptionIsUp =>
        _simulation is { Act: 2, Phase: Phase.Curtain or Phase.Act, Understudies: [{ IsOnStage: true }, ..] }
        && _simulation.ActTicksLeft > (Tuning.ActLength - CaptionTimeInTheAct) * Simulation.TicksPerSecond;

    /// <summary>
    /// Where an understudy is about to go, a broken line on the floor that fades with how far ahead it is. In
    /// an act, the stretch of its route it walks in the next <see cref="RouteAhead"/> seconds. While the stage
    /// stands between two acts, in the program and under the curtain, the first of the route as long, from the mark, which is where every understudy
    /// begins the next act: the line is there before the understudy is back on it. At the ovation and on a
    /// closed stage there is no next act and no line. A stretch it stands through has no length and no line, and
    /// a Vanish has none either: the dashes stop where it vanishes and start again where it comes out.
    /// </summary>
    // ponytail: the route is measured from its start every frame, so that the dashes lie still on the floor
    // while the understudy walks over them: a square root for every sixth place up to the far end of what is
    // shown, 750 an understudy at most and nine understudies. Keep each route's lengths when a frame feels it.
    // ponytail: a Vanish is told by its length, since the recording's Vanishes are not public: a stretch more
    // than twice what six ticks' walk can be (1.8 units on the committed numbers, against a Vanish of 6). No card
    // makes the walk faster today (the simulation's step is magicianSpeed alone); one that does must be counted
    // here. A Vanish that a wall cut shorter than that is drawn as a walk.
    private void DrawRouteAhead(Understudy understudy)
    {
        bool fromTheMark = _simulation.Phase is Phase.BetweenActs or Phase.Program or Phase.Curtain;
        if (!fromTheMark && !(_simulation.Phase is Phase.Act or Phase.Encore && understudy.IsOnStage))
        {
            return;
        }

        // The route's place of this tick: what the act has played. Read off the act's time left, like the
        // caption's time: an act whose length F5 changed while it was played is out by the change.
        int now = fromTheMark
            ? 0
            : (int)MathF.Round(Tuning.ActLength * Simulation.TicksPerSecond) - _simulation.ActTicksLeft;
        float shown = RouteAhead * Simulation.TicksPerSecond;
        float longestWalk = 2f * RouteStride * Tuning.MagicianSpeed / Simulation.TicksPerSecond;
        IReadOnlyList<Vector2> route = understudy.Route;
        int last = Math.Min(route.Count - 1, now + (int)shown);
        Color tint = TintOf(understudy);
        float gone = 0f;
        for (int i = RouteStride; i <= last; i += RouteStride)
        {
            Vector2 from = route[i - RouteStride];
            Vector2 along = route[i] - from;
            float length = along.Length();
            if (length == 0f)
            {
                continue;
            }

            if (i > now && length <= longestWalk)
            {
                // The dashes that begin on this stretch, each laid along it; of the stretch the understudy is
                // on, those ahead of its feet.
                float behind = length * Math.Max(0, now - (i - RouteStride)) / RouteStride;
                Color color = tint * (RouteOpacity * (1f - ((i - now) / shown)));
                float turn = MathF.Atan2(along.Y, along.X);
                const float pitch = RouteDash + RouteGap;
                for (float at = (pitch - (gone % pitch)) % pitch; at < length; at += pitch)
                {
                    if (at >= behind)
                    {
                        FillTurned(
                            from + (along * ((at + (RouteDash / 2f)) / length)),
                            new Vector2(RouteDash, RouteWidth),
                            turn,
                            color);
                    }
                }
            }

            gone += length;
        }
    }

    /// <summary>Where an understudy is drawn: in the curtain's rewind, or between its last two ticks.</summary>
    private Vector2 Feet(Understudy understudy, float alpha) =>
        _simulation.Phase == Phase.Curtain
            ? Rewound(understudy, alpha)
            : Vector2.Lerp(understudy.PreviousPosition, understudy.Position, alpha);

    /// <summary>
    /// The curtain's rewind: where an understudy is drawn while the curtain is up. It slides back along its own
    /// route, from where it stood when the last act ended to the first place of the route, where the simulation
    /// has had it since going on: nothing in the simulation moves for this.
    /// </summary>
    // ponytail: every stretch of the route is measured twice here, and this is asked twice a frame of every
    // understudy (for its figure and for its mark): 18,000 square roots an understudy and nine understudies at
    // most, for the curtain's one second. Keep each route's lengths, or the frame's feet, when a frame feels it.
    private Vector2 Rewound(Understudy understudy, float alpha)
    {
        // From its route's last place: where the act's last tick left it, or where it left the stage when its
        // own act was cut short by the magician's fall.
        // ponytail: an act played shorter than an older route (F5 with a smaller actLength) left that understudy
        // part-way along it, and the rewind starts from the route's end all the same. The simulation would have
        // to say how long the last act was played for.
        IReadOnlyList<Vector2> route = understudy.Route;
        int end = route.Count - 1;

        // The frame is drawn a part of a tick behind the last tick, like everything else.
        float curtainTicks = MathF.Max(1f, Tuning.CurtainTime * Simulation.TicksPerSecond);
        float left = MathF.Min(1f, _simulation.CurtainLeft + ((1f - alpha) / curtainTicks));

        // The route is run back by its length and not by its ticks, so an understudy that stood for most of its
        // act slides all through the curtain as one that ran does.
        float whole = 0f;
        for (int i = 1; i <= end; i++)
        {
            whole += Vector2.Distance(route[i - 1], route[i]);
        }

        // Fast at first and settling: with half of the curtain's time left, an eighth of the way is.
        float toGo = whole * left * left * left;
        for (int i = 1; i <= end; i++)
        {
            float stretch = Vector2.Distance(route[i - 1], route[i]);
            if (stretch > 0f && toGo <= stretch)
            {
                return Vector2.Lerp(route[i - 1], route[i], toGo / stretch);
            }

            toGo -= stretch;
        }

        return route[end];
    }

    /// <summary>
    /// The words of the screen: the HUD's, with the line that announces when no act is played or the magician has
    /// fallen in the one that is; as the second act begins the one caption, over the understudy that is drawn
    /// <paramref name="alpha"/> between its last two ticks; and the box office's hit points as a number at
    /// <paramref name="besideTheBar"/>, the point just to the right of the middle of its bar's end. They are
    /// drawn in screen pixels, so they stay sharp: the font is asked for at the size the window makes of it (see
    /// <see cref="Write"/>).
    /// </summary>
    private void DrawWords(float alpha, Vector2 besideTheBar)
    {
        string? said = _simulation.Phase switch
        {
            Phase.Encore => "Encore! Take a card, and the act goes on.",
            Phase.Program => $"Act {_simulation.Act} is over. The program has a card for the chorus.",
            Phase.BetweenActs when CardsAreShown => $"{Describe(_offered[_taken]).Name} it is.",
            Phase.BetweenActs when _simulation.ActEncores == 0 =>
                $"Act {_simulation.Act} is over. No encore, no card for the chorus.",
            Phase.BetweenActs => $"Act {_simulation.Act} is over. The chorus has its card.",
            Phase.Ovation => $"A standing ovation! {TheWayOn}",
            Phase.Closed => $"The box office fell. {TheWayOn}",
            Phase.Act when _simulation.MagicianHasFallen =>
                "The magician has fallen. The understudies carry on to the end of the act.",
            _ => null,
        };

        // Unsmoothed, for a card's picture: the words and the paper are drawn a pixel to a pixel and are the same
        // either way.
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        if (_simulation.Phase is Phase.Encore or Phase.Program or Phase.BetweenActs)
        {
            DrawTheOffer();
        }

        // Which act's understudy this is, on the figure itself: its act's number over its head, in its tint.
        // Those that stand on one spot have theirs in a row, the oldest first, and not on top of each other.
        // A mark is kept on the floor: where its place over the head is in the curtain (at the back door), it
        // is written under the feet.
        // Not while the stage stands for an offer or between two acts: the words are drawn over the cards and the
        // panels, so the number of an understudy that stands behind one would be written on it.
        // ponytail: one spot is a hard line (a mark jumps aside as two figures cross it),
        // each mark counts the older ones near itself (three in a chain, each near the next alone, are not one
        // row) and the row starts over the head and is not centred on it. A row laid out from groups, if it shows.
        IReadOnlyList<Understudy> cast = _simulation.Understudies;
        Span<Vector2> stands = stackalloc Vector2[cast.Count];
        for (int i = 0; i < cast.Count; i++)
        {
            if (StageStands || !cast[i].IsOnStage)
            {
                // Nowhere: no mark of its own, and beside nobody's.
                stands[i] = new Vector2(float.NaN);
                continue;
            }

            stands[i] = Feet(cast[i], alpha);

            // The first understudy has the caption for its label while that is up.
            if (i == 0 && CaptionIsUp)
            {
                continue;
            }

            int before = 0;
            for (int j = 0; j < i; j++)
            {
                if (Vector2.Distance(stands[j], stands[i]) < MarkSameSpot)
                {
                    before++;
                }
            }

            float over = stands[i].Y - MagicianTall - MarkLift;
            Write(
                Face.Sentence,
                MarkHeight,
                $"{cast[i].Act}",
                new Vector2(
                    stands[i].X + (before * MarkApart),
                    over < Tuning.StageFloorTop + GoldLine + (MarkHeight / 2f) ? stands[i].Y + MarkUnderTheFeet : over),
                0.5f,
                TintOf(cast[i]),
                keptOnTheStage: true);
        }

        // The first understudy is told once what it is (CaptionIsUp): the words are in its colour and over its
        // head, wherever it is drawn, so they are its label and nobody else's.
        if (CaptionIsUp)
        {
            Write(
                Face.Sentence,
                CaptionHeight,
                Caption,
                stands[0] - new Vector2(0f, MagicianTall + CaptionLift),
                0.5f,
                UnderstudyTints[0],
                keptOnTheStage: true);
        }

        if (!StageStands)
        {
            Write(Face.Sentence, NumberHeight, $"{MathF.Ceiling(_simulation.BoxOfficeHitPoints)}", besideTheBar, 0f, Words);
        }

        // The HUD last: its paper lies over whatever word stands where it is.
        DrawTheHud(said);
        _spriteBatch.End();
    }

    /// <summary>
    /// Words in screen pixels, in a <paramref name="face"/>: the line is centred on the height of
    /// <paramref name="at"/>, a point of the stage, with its left end there, its middle (<paramref name="anchor"/>
    /// 0.5) or its right end (1). Words on the stage carry the outline; words <paramref name="onPaper"/>, dark on
    /// a light ground, have none; the menu's title has one of an <paramref name="outlineWidth"/> of its own, in
    /// world units. Words <paramref name="keptOnTheStage"/> are moved by as much as it takes to have
    /// the whole line on the stage, and a label's letters are <paramref name="spacing"/> apart, in world units.
    /// Called between the Begin and the End of a batch with no transform.
    /// </summary>
    private void Write(
        Face face,
        float height,
        string text,
        Vector2 at,
        float anchor,
        Color color,
        bool onPaper = false,
        bool keptOnTheStage = false,
        float spacing = 0f,
        float outlineWidth = OutlineWidth)
    {
        SpriteFontBase font = Font(face, height);
        int outline = onPaper ? 0 : Outline(font, outlineWidth);
        float apart = MathF.Round(spacing * _scale);
        var size = new Vector2(font.MeasureString(text, characterSpacing: apart).X, font.LineHeight);
        Vector2 topLeft = _corner + (at * _scale) - new Vector2(size.X * anchor, size.Y / 2f);
        if (keptOnTheStage)
        {
            // The outline is kept on the stage with the words. The far corner first: a line wider than the stage
            // starts at the stage's left edge.
            var rim = new Vector2(outline);
            topLeft = Vector2.Max(_corner + rim, Vector2.Min(topLeft, _corner + (Tuning.StageSize * _scale) - size - rim));
        }

        // The words in ink at every place within the outline's width, and then themselves over that: at the eight
        // places of its rim alone a stroke thinner than the outline would stand clear of it. The middle is left
        // out, which the words cover, and the four corners, so the outline is round. The text library's own
        // stroke (FontSystemEffect.Stroked) is not used: it is black, it eats into the letters and it moves the
        // line by its width.
        // ponytail: twenty draws of a line for an outline of two pixels, a dozen lines a frame. A line drawn once
        // to a texture of its own and kept while its words stand, if the HUD grows to where that is felt.
        var place = new Vector2(MathF.Round(topLeft.X), MathF.Round(topLeft.Y));
        for (int x = -outline; x <= outline; x++)
        {
            for (int y = -outline; y <= outline; y++)
            {
                if ((x != 0 || y != 0) && (Math.Abs(x) < outline || Math.Abs(y) < outline))
                {
                    _spriteBatch.DrawString(font, text, place + new Vector2(x, y), OutlineInk, characterSpacing: apart);
                }
            }
        }

        _spriteBatch.DrawString(font, text, place, color, characterSpacing: apart);
    }

    /// <summary>
    /// How wide the outline of words on the stage is in that font, in screen pixels: small words have a thinner
    /// one, for the whole width shuts the eyes of letters under some eighteen pixels tall.
    /// </summary>
    private int Outline(SpriteFontBase font, float width = OutlineWidth) =>
        Math.Max(1, (int)MathF.Round(MathF.Min(width * _scale, font.FontSize / SmallestOutlined)));

    /// <summary>
    /// How wide <see cref="Write"/> draws <paramref name="text"/> in that face at that height, its letters
    /// <paramref name="spacing"/> apart, in world units.
    /// </summary>
    private float Wide(Face face, float height, string text, float spacing = 0f) =>
        Font(face, height).MeasureString(text, characterSpacing: MathF.Round(spacing * _scale)).X / _scale;

    /// <summary>
    /// A face at the size the window makes of a height in world units. A whole number of pixels tall, and drawn
    /// on whole pixels: a glyph drawn between two pixels is smeared over both. A window too small for words still
    /// has a pixel's worth.
    /// </summary>
    private SpriteFontBase Font(Face face, float height) =>
        _faces[(int)face].GetFont(MathF.Max(1f, MathF.Round(height * _scale)));

    /// <summary>The faces of the game's words, in the order of <see cref="FaceFiles"/>.</summary>
    private enum Face
    {
        /// <summary>Pixelify Sans Bold: a card's name, the menu's title and entries, and on the poster the night's name and a new kind's.</summary>
        Heading,

        /// <summary>Atkinson Hyperlegible: a sentence, and a number that is read in a glance.</summary>
        Sentence,
    }

    /// <summary>A bar that is <paramref name="share"/> full, from its left end.</summary>
    private void FillBar(Vector2 topLeft, Vector2 size, float share, Color color)
    {
        Fill(topLeft, size, HitPointsLost);
        Fill(topLeft, size with { X = size.X * Math.Clamp(share, 0f, 1f) }, color);
    }

    /// <summary>
    /// The one call that draws a figure. Whatever is asked of a figure's look is asked of this call, and nothing
    /// outside it knows how a figure is drawn: no flash, no body and no fade paints over a figure with a shape of
    /// its own. A figure is a frame of a sprite sheet, drawn at a whole number of screen pixels to a sprite pixel
    /// and from a whole screen pixel, with a shadow on the floor under it while it stands.
    /// </summary>
    /// <param name="pale">A stunned critic: it has gone pale all over.</param>
    /// <param name="white">The flash of a figure that was hurt a moment ago, from 0 to 1: a white copy of the
    /// sprite drawn over it that thick, since a tint can only darken.</param>
    /// <param name="fallen">Lying flat where <paramref name="feet"/> is, and not standing on it.</param>
    /// <param name="opacity">All that is drawn of the figure is that much see-through.</param>
    /// <param name="tint">A colour washed over the figure: the magician's, in the moment nothing hurts it, is the
    /// smoke's.</param>
    /// <param name="cardboard">An understudy: the figure's shape filled with this colour, the colour of the act it
    /// came from, and the figure's own picture over that at <see cref="UnderstudyPicture"/>.</param>
    /// <param name="ahead">It is drawn over whoever stands less than this far in front of it.</param>
    /// <param name="toward">Where the figure faces: toward the viewer when this is nothing.</param>
    /// <param name="walking">Its walk goes through its frames, while an act is played.</param>
    /// <param name="speed">How fast it walks when it does, in units a second: its kind's number and not what it
    /// made of it in the last tick, so that its walk does not skip when it is pushed. The faster, the quicker
    /// its frames.</param>
    /// <param name="beat">Which frame of the walk it is on when the clock is at nothing: two figures with
    /// different beats are out of step.</param>
    /// <param name="measure">A measure of its own, in place of its family's: the menu's figures are larger than
    /// the stage's.</param>
    private void DrawFigure(
        Figure figure,
        Vector2 feet,
        bool pale = false,
        float white = 0f,
        bool fallen = false,
        float opacity = 1f,
        Color? tint = null,
        Vector2 toward = default,
        bool walking = false,
        float speed = WalkReferenceSpeed,
        int beat = 0,
        Color? cardboard = null,
        float ahead = 0f,
        float? measure = null)
    {
        // Sideways when it goes more across than up or down. The side view faces right and is mirrored for left.
        bool sideways = MathF.Abs(toward.X) > MathF.Abs(toward.Y);
        Facing facing = sideways ? Facing.Side : toward.Y < 0f ? Facing.Up : Facing.Down;
        Sheet[] views = _sheets[(int)figure];
        Sheet sheet = views[fallen ? 0 : Math.Min((int)facing, views.Length - 1)];
        bool mirrored = sideways && toward.X < 0f && !fallen && views.Length > 1;
        float rate = Math.Clamp(WalkFramesPerSecond * speed / WalkReferenceSpeed, WalkSlowest, WalkFastest);
        int frame = walking && !fallen && _simulation.Phase == Phase.Act
            ? (int)(((_walkClock * rate) + beat) % (sheet.Columns * sheet.Columns))
            : 0;
        var source = new Rectangle(
            sheet.First.X + (frame % sheet.Columns * sheet.First.Width),
            sheet.First.Y + (frame / sheet.Columns * sheet.First.Height),
            sheet.First.Width,
            sheet.First.Height);

        // A sprite is in units whatever the window, by the measure of its family, and unsmoothed.
        // ponytail: its own pixels are so uneven wherever the window does not make a whole number of them: the
        // set's and, at a measure of 1, a figure's are one and a quarter screen pixels at 1280 wide (every
        // fourth is two), and whole at 1024 and its multiples. A set and figures drawn for 1280, or a stage drawn
        // to a target of its own and scaled whole, would end it.
        float unit = (measure is { } own ? PixelAt(own) : figure <= Figure.Scalper ? FigurePixel : 1f / SetPixelsPerUnit)
            / sheet.Block;
        float width = source.Width * unit;
        var onAPixel = new Vector2(MathF.Round(feet.X * _scale), MathF.Round(feet.Y * _scale)) / _scale;

        if (!fallen)
        {
            FillDisc(onAPixel, width * 0.3f, Color.Black * (ShadowOpacity * opacity), flat: 0.4f);
        }

        // Standing: the bottom middle of the frame is on the feet. Fallen: turned onto its side, lying along the
        // floor with its middle where the feet were.
        void Copy(Texture2D image, Color color, float depth) =>
            _spriteBatch.Draw(
                image,
                fallen ? onAPixel - new Vector2(0f, width / 2f) : onAPixel,
                source,
                color,
                fallen ? MathF.PI / 2f : 0f,
                new Microsoft.Xna.Framework.Vector2(source.Width / 2f, fallen ? source.Height / 2f : source.Height),
                unit,
                mirrored ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                depth);

        // A sorted batch keeps no order between two textures at one depth: what is drawn over a figure stands a
        // hair in front of it. ponytail: a neighbour whose feet are within that hair below comes between a figure
        // and its wash for a frame, and between an understudy's fill and its picture; a wash made in a shader, or
        // an understudy drawn to a target of its own, would be the figure's own.
        float depth = Depth(feet + new Vector2(0f, ahead));
        float over = MathF.Min(1f, depth + 0.0001f);
        if (cardboard is { } fill)
        {
            // The two are laid one over the other with no target of their own, so each is as thick as leaves
            // the pair what it would be if it were made whole and then drawn at the opacity: the picture
            // UnderstudyPicture of what is there, the fill the rest, and the floor showing through by what the
            // opacity leaves.
            float picture = opacity * UnderstudyPicture;
            Copy(sheet.White, fill * ((opacity - picture) / (1f - picture)), depth);
            Copy(sheet.Image, Color.White * picture, over);
        }
        else
        {
            Copy(sheet.Image, Color.White * opacity, depth);
        }

        // A tint can only darken a sprite, so the smoke on the magician, a headliner's red and a stunned critic's
        // pallor are washed over it as the flash is: the pallor over the red, so a stunned headliner is still one.
        if (tint is { } wash)
        {
            Copy(sheet.White, wash * (0.5f * opacity), over);
        }

        if (pale)
        {
            Copy(sheet.White, CriticStunnedBody * (0.5f * opacity), over);
        }

        if (white > 0f)
        {
            Copy(sheet.White, Color.White * (white * opacity), over);
        }
    }

    /// <summary>The figure an enemy of a kind is drawn as.</summary>
    // ponytail: the view knows the kinds by their places in enemyKinds, the critic first, the stagehand second,
    // the rival's understudy third, the headliner fourth, which is the rival's cut-out with a wash
    // (`WashOf`) until it has a picture of its own, and the scalper fifth (plan T57). A sixth kind is drawn as a
    // stagehand; the kinds need names to be looked up by when the list is reordered.
    private static Figure FigureOf(int kind) => kind switch
    {
        0 => Figure.Critic,
        2 or 3 => Figure.Rival,
        4 => Figure.Scalper,
        _ => Figure.Stagehand,
    };

    /// <summary>The colour washed over an enemy of a kind: the headliner's, and none for the others.</summary>
    private static Color? WashOf(int kind) => kind == 3 ? HeadlinerWash : null;

    /// <summary>
    /// How long a walking figure's sprite pixel is in world units in the frame being drawn: the figures' measure,
    /// and a whole number of screen pixels where the measure asks for one to within <see cref="WholeWithin"/>.
    /// </summary>
    private float FigurePixel => PixelAt(FiguresMeasure);

    /// <summary>A sprite pixel at a measure, as <see cref="FigurePixel"/> is at the figures'.</summary>
    private float PixelAt(float measure)
    {
        float asked = _scale * measure / SetPixelsPerUnit;
        float whole = MathF.Max(1f, MathF.Round(asked));
        return (MathF.Abs(asked - whole) <= WholeWithin ? whole : asked) / _scale;
    }

    /// <summary>How tall the magician is drawn in the frame being drawn, in world units: what is over its head is over this.</summary>
    private float MagicianTall => _sheets[(int)Figure.Magician][0].First.Height * FigurePixel;

    /// <summary>
    /// A picture laid side by side over a rectangle of the stage, its first copy's corner on the stage's own corner
    /// across, so that a shaken stage carries its boards with it. For a batch that lets a picture go round, and for
    /// a picture that fills its file from side to side.
    /// </summary>
    /// <param name="pixel">How long a sprite pixel of the picture is, in world units.</param>
    private void Lay(Sheet sheet, Vector2 topLeft, Vector2 size, float pixel)
    {
        float unit = pixel / sheet.Block;
        var source = new Rectangle(
            sheet.First.X + (int)MathF.Round(topLeft.X / unit),
            sheet.First.Y,
            (int)MathF.Ceiling(size.X / unit),
            (int)MathF.Ceiling(size.Y / unit));
        _spriteBatch.Draw(
            sheet.Image,
            topLeft,
            source,
            Color.White,
            0f,
            Microsoft.Xna.Framework.Vector2.Zero,
            unit,
            SpriteEffects.None,
            0f);
    }

    /// <summary>A sheet of the sprites' folder: its picture, a white copy of it, and where its first frame is.</summary>
    /// <param name="columns">How many frames a row has, and how many rows there are; 1 is one picture, of which the
    /// part that is not empty is the frame.</param>
    /// <param name="block">How many of the file's pixels are drawn to one sprite pixel.</param>
    private Sheet ReadSheet(string name, int columns, float block = 1f)
    {
        // The batch blends colours that are already multiplied by their alpha.
        Texture2D image = Texture2D.FromFile(
            GraphicsDevice, Path.Combine(_spritesFolder, name), DefaultColorProcessors.PremultiplyAlpha);
        var pixels = new Color[image.Width * image.Height];
        image.GetData(pixels);
        int left = image.Width, top = image.Height, right = -1, bottom = -1;
        for (int i = 0; i < pixels.Length; i++)
        {
            // A tool's cut-out leaves a faint haze in its margin: under this it does not count.
            if (pixels[i].A > 16)
            {
                (int x, int y) = (i % image.Width, i / image.Width);
                (left, top, right, bottom) = (Math.Min(left, x), Math.Min(top, y), Math.Max(right, x), Math.Max(bottom, y));
            }

            // The white copy: the same shape, as see-through, in white.
            pixels[i] = new Color(pixels[i].A, pixels[i].A, pixels[i].A, pixels[i].A);
        }

        if (right < 0)
        {
            throw new InvalidDataException($"{name} is empty");
        }

        var white = new Texture2D(GraphicsDevice, image.Width, image.Height);
        white.SetData(pixels);
        Rectangle first = columns == 1
            ? new Rectangle(left, top, right - left + 1, bottom - top + 1)
            : new Rectangle(0, 0, image.Width / columns, image.Height / columns);
        return new Sheet(image, white, first, columns, block);
    }

    /// <summary>Where in a sorted batch what stands on <paramref name="feet"/> is drawn: the lower, the later.</summary>
    private float Depth(Vector2 feet) => Math.Clamp(feet.Y / Tuning.StageSize.Y, 0f, 1f);

    /// <summary>
    /// Where a fall earns no applause (plan decision 27): the floor within the tuning's radius of the box office's
    /// middle a shade darker, and a thin broken line round it. Only what is on the floor is drawn, and with no
    /// radius nothing is.
    /// </summary>
    private void DrawTheQuietFloor()
    {
        float radius = Tuning.ApplauseBoxOfficeRadius;
        if (radius <= 0f)
        {
            return;
        }

        // Rows that lie one below another and never on one another, each as wide as the circle is at its middle
        // and no wider than the floor.
        Vector2 middle = Tuning.BoxOfficePosition;
        for (float top = MathF.Max(middle.Y - radius, Tuning.StageFloorTop);
            top < MathF.Min(middle.Y + radius, Tuning.StageSize.Y);
            top += QuietFloorRow)
        {
            float y = top + (QuietFloorRow / 2f) - middle.Y;
            float halfWidth = MathF.Sqrt(MathF.Max(0f, (radius * radius) - (y * y)));
            float left = MathF.Max(0f, middle.X - halfWidth);
            float right = MathF.Min(Tuning.StageSize.X, middle.X + halfWidth);
            if (right > left)
            {
                Fill(new Vector2(left, top), new Vector2(right - left, QuietFloorRow), QuietFloorShade);
            }
        }

        // The line: every other of the short straight stretches the circle is cut into.
        float stretch = MathF.Tau / QuietFloorStretches;
        for (int i = 0; i < QuietFloorStretches; i += 2)
        {
            float turn = (i + 0.5f) * stretch;
            Vector2 at = middle + (new Vector2(MathF.Cos(turn), MathF.Sin(turn)) * radius);
            if (at.X >= 0f && at.X <= Tuning.StageSize.X && at.Y >= Tuning.StageFloorTop && at.Y <= Tuning.StageSize.Y)
            {
                FillTurned(at, new Vector2(radius * stretch, QuietFloorLine), turn + (MathF.PI / 2f), QuietFloorEdge);
            }
        }
    }

    /// <summary>A filled circle in world units, flat on the floor.</summary>
    /// <param name="flat">How tall it is for its width: under 1 it is a shadow's oval.</param>
    private void FillDisc(Vector2 middle, float radius, Color color, float flat = 1f)
    {
        // Strips that lie side by side and never on one another: where two overlapped, a see-through colour would
        // show twice as thick.
        float height = 2f * radius / DiscStrips;
        for (int i = 0; i < DiscStrips; i++)
        {
            // Each strip is as wide as the circle is at the strip's own middle.
            float y = ((i + 0.5f) * height) - radius;
            float halfWidth = MathF.Sqrt((radius * radius) - (y * y));
            Fill(
                middle + new Vector2(-halfWidth, (y - (height / 2f)) * flat),
                new Vector2(2f * halfWidth, height * flat),
                color);
        }
    }

    /// <summary>A filled rectangle in world units.</summary>
    /// <param name="depth">Counts only in a sorted batch, from 0 to 1: the greater depth is drawn later.</param>
    private void Fill(Vector2 topLeft, Vector2 size, Color color, float depth = 0f) =>
        _spriteBatch.Draw(
            _pixel, topLeft, null, color, 0f, Microsoft.Xna.Framework.Vector2.Zero, size, SpriteEffects.None, depth);

    /// <summary>A filled rectangle in world units, turned about its middle by <paramref name="turn"/> radians.</summary>
    private void FillTurned(Vector2 middle, Vector2 size, float turn, Color color, float depth = 0f) =>
        _spriteBatch.Draw(
            _pixel, middle, null, color, turn, new Microsoft.Xna.Framework.Vector2(0.5f), size, SpriteEffects.None, depth);

    /// <summary>The views a figure has a sheet for, in the order the sheets are kept.</summary>
    private enum Facing
    {
        Down,
        Up,
        Side,
    }

    /// <param name="First">The first frame; the others follow it across and then down.</param>
    /// <param name="Block">How many of the file's pixels are one sprite pixel.</param>
    /// <param name="Columns">How many frames a row has; there are as many rows.</param>
    private sealed record Sheet(Texture2D Image, Texture2D White, Rectangle First, int Columns, float Block);

    /// <summary>What <see cref="DrawFigure"/> can draw.</summary>
    private enum Figure
    {
        // The walking figures first, to the scalper: DrawFigure tells them from the set by that.
        Magician,
        Critic,
        Stagehand,
        Rival,
        Scalper,
        BoxOffice,
        ShutDoor,
        OpenDoor,
        Footlight,
    }
}
