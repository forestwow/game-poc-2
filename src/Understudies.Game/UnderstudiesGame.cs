using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Understudies.Core;

// World positions are Core's vectors; MonoGame's own Vector2 appears only where SpriteBatch asks for one.
using Vector2 = System.Numerics.Vector2;

namespace Understudies.Game;

// The base class is spelled out because `Game` alone means this namespace here.
internal sealed class UnderstudiesGame : Microsoft.Xna.Framework.Game
{
    private const int WindowWidth = 1280;
    private const int WindowHeight = 720;
    private const float MagicianHeight = 3f;
    private const float CriticBodyHeight = 1.4f;
    private const float CriticHeadSize = 0.6f;
    private const float ThrownCardWidth = 0.5f;
    private const float ThrownCardHeight = 0.35f;

    // A card flies at the height of a critic's chest.
    private const float ThrownCardLift = 1f;

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
    // understudy stands beside the magician the words are clear of the bar over the magician's head.
    private const string Caption = "Your understudy. It repeats your act one, every act.";
    private const float CaptionHeight = 1f;
    private const float CaptionLift = MagicianHeight + 1.2f;
    private const float CaptionTimeInTheAct = 3f;

    // An understudy is half there, and the line of its route on the floor is fainter still. The line is laid from
    // every sixth place of the route to the next: a tenth of a second, under a unit at the magician's speed.
    private const float UnderstudyOpacity = 0.5f;
    private const float RouteOpacity = 0.22f;
    private const float RouteWidth = 0.12f;
    private const int RouteStride = 6;

    // A piece of applause is a diamond that hovers at a critic's chest, where the scraps of its fall burst from,
    // with a pale heart: this wide, and never fainter than ApplauseFaintest, so that a piece about to go is still
    // seen to be there.
    private const float ApplauseSize = 0.75f;
    private const float ApplauseLift = 0.8f;
    private const float ApplauseFaintest = 0.25f;

    // The act's applause is a bar in the middle of the back wall, ApplauseBarGap above its foot. Its second notch
    // is this far along it: the bar is full a little past the share that earns the most.
    private static readonly Vector2 ApplauseBar = new(14f, 0.45f);
    private const float ApplauseBarSecondNotchAt = 0.7f;
    private const float ApplauseBarGap = 0.2f;
    private const float ApplauseNotchWidth = 0.12f;
    private const float ApplauseNotchPast = 0.18f;

    // ponytail: a system font, the first of these files that this machine has: one for macOS, one for Windows and
    // two for Linux. A font file is shipped with the game when a build leaves the owner's machine.
    private static readonly string[] FontFiles =
    [
        "/System/Library/Fonts/Supplemental/Arial.ttf",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf"),
    ];

    private static readonly Color Surround = new(24, 18, 28);
    private static readonly Color BackWall = new(52, 40, 62);
    private static readonly Color Floor = new(96, 74, 58);
    private static readonly Color OpenDoor = new(222, 180, 104);
    private static readonly Color ShutDoor = new(66, 50, 42);
    private static readonly Color BoxOffice = new(150, 44, 52);
    private static readonly Color HitPoints = new(132, 204, 110);
    private static readonly Color HitPointsLost = new(30, 22, 30);
    private static readonly Color Magician = new(250, 226, 120);
    private static readonly Color VanishBar = new(150, 214, 236);
    private static readonly Color CloudPuff = new(236, 232, 244);
    private static readonly Color CriticBody = new(62, 88, 156);
    private static readonly Color CriticStunnedBody = new(168, 180, 212);
    private static readonly Color CriticHead = new(226, 216, 200);
    private static readonly Color ThrownCardFace = new(250, 246, 236);
    private static readonly Color ScrapOfPaper = new(244, 238, 222);
    private static readonly Color Words = new(236, 228, 210);

    // Applause is the one thing on the stage in this colour: the pieces on the floor and the bar they fill.
    private static readonly Color ApplauseGlow = new(255, 72, 196);
    private static readonly Color ApplauseHeart = new(255, 226, 246);

    // An understudy's card is a card, a little duller than the magician's own.
    private static readonly Color UnderstudysCardFace = new(190, 184, 172);

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

    private readonly SimulationClock _clock = new();
    private readonly string? _capturePath;
    private readonly int _captureTicks;
    private readonly Sound _sound;
    private Simulation _simulation;
    private Juice _juice;
    private bool _captured;
    private KeyboardState _keysBefore;
    private GamePadState _padBefore;
    private bool _vanishAsked;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;

    // Null on a machine that has none of the font files: the game then runs without its words.
    private FontSystem? _fonts;

    /// <summary>With a <paramref name="capturePath"/> the game does not play: it saves one frame there and exits.</summary>
    public UnderstudiesGame(Tuning tuning, string? capturePath, int captureTicks)
    {
        _simulation = capturePath is null ? NewShow(tuning) : new Simulation(tuning, CaptureSeed);
        _juice = new Juice(capturePath is null ? Random.Shared : new Random((int)CaptureSeed));
        _sound = new Sound(silent: capturePath is not null);
        _capturePath = capturePath;
        _captureTicks = captureTicks;
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

        if (FontFiles.FirstOrDefault(File.Exists) is { } fontFile)
        {
            _fonts = new FontSystem();
            _fonts.AddFont(File.ReadAllBytes(fontFile));
            Console.WriteLine($"Font read from {fontFile}");
        }
        else
        {
            Console.Error.WriteLine("No font found: the game has no text.");
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

        KeyboardState keys = Keyboard.GetState();
        if (keys.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        // A key held down counts once.
        bool Pressed(Keys key) => keys.IsKeyDown(key) && !_keysBefore.IsKeyDown(key);

        // F5 reads tuning.json again and the next tick runs on the new numbers. A file that does not parse leaves
        // the numbers as they were.
        if (Pressed(Keys.F5) && TuningFile.Read() is { } tuning)
        {
            _simulation.Tuning = tuning;
        }

        // R starts the show again, on the numbers of now.
        if (Pressed(Keys.R))
        {
            _simulation = NewShow(Tuning);
            _juice = new Juice(Random.Shared);
        }

        // M mutes the sound, and M again brings it back.
        if (Pressed(Keys.M))
        {
            _sound.Muted = !_sound.Muted;
        }

        // Space or the gamepad's A is one Vanish for each press. The press waits for a tick to take it: a frame may
        // run no tick, and it must not be lost, or several, and it must not be asked of each.
        GamePadState pad = GamePad.GetState(PlayerIndex.One);
        if (Pressed(Keys.Space) || (pad.IsButtonDown(Buttons.A) && _padBefore.IsButtonUp(Buttons.A)))
        {
            _vanishAsked = true;
        }

        // Enter or the gamepad's Start goes on to the next act. The simulation takes it between two acts only.
        if (Pressed(Keys.Enter) || (pad.IsButtonDown(Buttons.Start) && _padBefore.IsButtonUp(Buttons.Start)))
        {
            _simulation.GoOn();
        }

        _keysBefore = keys;
        _padBefore = pad;

        // The hit-stop: while the moment of a Vanish holds the world still, a frame's time is dropped. The clock
        // gets none of it and no tick is run, so the frame is drawn as the one before it was.
        double frameSeconds = gameTime.ElapsedGameTime.TotalSeconds;
        if (!_juice.Holds((float)frameSeconds))
        {
            // Between two acts the stage stands until the player goes on, and a performance that is over stands as
            // it ended until R: there Step changes nothing.
            Vector2 move = ReadMove(keys, pad);
            int ticks = _clock.Advance(frameSeconds);
            for (int i = 0; i < ticks; i++)
            {
                Tick(new MagicianInput(move, _vanishAsked));
                _vanishAsked = false;
            }

            // The juice goes on whatever the phase: between two acts, as after the show, what flew still settles,
            // a flash ends and a shaken stage comes to rest, while the simulation stands.
            _juice.Advance((float)frameSeconds);
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Only an act has a next tick to draw towards, and the curtain, whose rewind goes on between its ticks.
        DrawStage(_simulation.Phase is Phase.Act or Phase.Curtain ? _clock.Alpha : 1f);
        base.Draw(gameTime);
    }

    /// <summary>A show nobody has seen: its seed is the time, which Core never reads.</summary>
    private static Simulation NewShow(Tuning tuning) => new(tuning, (ulong)DateTime.UtcNow.Ticks);

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
        _simulation.Step(input);
        _juice.Feed(_simulation);
        _sound.Feed(_simulation);
    }

    /// <summary>Walks a fixed script, draws the frame it ends on and saves it as a PNG.</summary>
    private void Capture(string path)
    {
        // The first act: two seconds right and down, out of every critic's range, and still there while critics
        // gather at the box office: more of them than the magician's cards can fell in time. Then two seconds back
        // to the mark, which is at the edge of the crowd by now, and three seconds still: the crowd turns on the
        // magician and its hit points go. On the first tick after thirty-one seconds a Vanish to the left: the
        // cloud lies on the crowd, and the frames of the next second show it, the stunned in it and the Vanish's
        // bar part full. There the magician stands for the rest of the act, out of the crowd's reach and with the
        // crowd in its own: it throws until few are left.
        // The three acts go three ways, so that their understudies do not stand in one pile. The second act: out
        // to the first door, the only one open, to a place below the critics' way that has the door in range and
        // is out of their reach, and every critic that enters falls there. The third: up, across behind the box
        // office and down its far side. In every later act the magician stands on its mark, and the performance
        // is played to its ovation.
        // Every act opens with its curtain, whose ticks are counted here with the rest: the simulation takes no
        // input in them, and the script's own count, of an act's ticks, starts when the curtain is over.
        const int second = Simulation.TicksPerSecond;
        for (int i = 0; i < _captureTicks; i++)
        {
            // Nobody is here to press a key between two acts. Going on before the tick, and not after it, leaves a
            // capture that ends on an act's last tick between the two acts.
            _simulation.GoOn();
            int length = (int)MathF.Round(Tuning.ActLength * second);
            Tick((_simulation.Act, length - _simulation.ActTicksLeft) switch
            {
                (1, < 2 * second) => new MagicianInput(new Vector2(1f, 1f)),
                (1, < 26 * second) => default,
                (1, < 28 * second) => new MagicianInput(new Vector2(-1f, -1f)),
                (1, < 31 * second) => default,
                (1, 31 * second) => new MagicianInput(new Vector2(-1f, 0f), Vanish: true),
                (2, < 140) => new MagicianInput(new Vector2(-1f, 0.3f)),
                (3, < 40) => new MagicianInput(new Vector2(0f, -1f)),
                (3, < 100) => new MagicianInput(new Vector2(1f, 0f)),
                (3, < 134) => new MagicianInput(new Vector2(0f, 1f)),
                _ => default,
            });

            // Each tick is a sixtieth of a second to the juice, as in a game that runs a tick a frame: the frame
            // shows the scraps and the flashes that would be on the screen at that moment. Nothing holds a capture
            // still: it is counted in ticks.
            _juice.Advance(1f / second);
        }

        using var frame = new RenderTarget2D(GraphicsDevice, WindowWidth, WindowHeight);
        GraphicsDevice.SetRenderTarget(frame);
        DrawStage(alpha: 1f);
        GraphicsDevice.SetRenderTarget(null);

        using FileStream file = File.Create(path);
        frame.SaveAsPng(file, frame.Width, frame.Height);
    }

    /// <param name="alpha">How far between the last two ticks to draw what moves: 1 is the last tick itself.</param>
    private void DrawStage(float alpha)
    {
        // The stage keeps its shape: it is fitted to the window and the rest is left dark.
        Viewport viewport = GraphicsDevice.Viewport;
        float scale = MathF.Min(viewport.Width / Tuning.StageSize.X, viewport.Height / Tuning.StageSize.Y);
        Vector2 corner = (new Vector2(viewport.Width, viewport.Height) - (Tuning.StageSize * scale)) / 2f;

        // A shake moves the whole picture, bars and all. The wall and the floor are laid as far past the stage's
        // edge as the stage is moved, so the strip of the window a shake uncovers is stage and not the surround.
        corner += _juice.Shake * scale;
        Vector2 past = Vector2.Abs(_juice.Shake);
        Matrix worldToScreen = Matrix.CreateScale(scale, scale, 1f) * Matrix.CreateTranslation(corner.X, corner.Y, 0f);

        GraphicsDevice.Clear(Surround);

        // The back wall, the floor below it and what lies flat on the floor.
        _spriteBatch.Begin(transformMatrix: worldToScreen);
        var floorTopLeft = new Vector2(0f, Tuning.StageFloorTop);
        var across = new Vector2(Tuning.StageSize.X + (2f * past.X), 0f);
        Fill(-past, across with { Y = Tuning.StageFloorTop + past.Y }, BackWall);
        Fill(floorTopLeft with { X = -past.X }, across with { Y = Tuning.StageSize.Y - Tuning.StageFloorTop + past.Y }, Floor);
        for (int i = 0; i < Tuning.StageDoors.Count; i++)
        {
            // A door is a mat as wide as the door, the half of it that is on the floor: lit when the door is open
            // in this act, and dim while it is shut.
            var half = new Vector2(Tuning.StageDoorWidth / 2f);
            Vector2 topLeft = Vector2.Max(Tuning.StageDoors[i].Position - half, floorTopLeft);
            Vector2 bottomRight = Vector2.Min(Tuning.StageDoors[i].Position + half, Tuning.StageSize);
            Fill(topLeft, bottomRight - topLeft, _simulation.DoorIsOpen(i) ? OpenDoor : ShutDoor);
        }

        foreach (Understudy understudy in _simulation.Understudies)
        {
            // The whole route of each understudy, a faint line on the floor in its act's colour. A stretch it
            // stood through has no length and is not drawn.
            Color color = UnderstudyTints[(understudy.Act - 1) % UnderstudyTints.Length] * RouteOpacity;
            for (int i = RouteStride; i < understudy.Route.Count; i += RouteStride)
            {
                Vector2 from = understudy.Route[i - RouteStride];
                Vector2 along = understudy.Route[i] - from;
                if (along != Vector2.Zero)
                {
                    FillTurned(
                        from + (along / 2f),
                        new Vector2(along.Length(), RouteWidth),
                        MathF.Atan2(along.Y, along.X),
                        color);
                }
            }
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
            DrawFigure(Figure.Critic, body.Position, white: body.White, fallen: true, opacity: body.Opacity);
        }

        _spriteBatch.End();

        // What stands on the floor: the lower on the screen, the later it is drawn (Depth says how late).
        _spriteBatch.Begin(SpriteSortMode.FrontToBack, transformMatrix: worldToScreen);

        // The box office is drawn from halfway between the middle of its circle and the circle's front. A critic
        // that touches the circle from in front then overlaps the foot of the box, one at a side stands against its
        // wall, and one in front of a corner is not hidden behind it.
        Vector2 boxOfficeFeet = Tuning.BoxOfficePosition + new Vector2(0f, Tuning.BoxOfficeSize / 4f);

        // The batch keeps no order between equal depths, and the magician's mark is on this very line: the box
        // office stands a hair behind its foot line, so whoever stands exactly on that line is in front.
        const float hair = 0.001f;
        DrawFigure(Figure.BoxOffice, boxOfficeFeet - new Vector2(0f, hair), white: _juice.BoxOfficeWhite);
        Vector2 magicianFeet = Vector2.Lerp(_simulation.MagicianPreviousPosition, _simulation.MagicianPosition, alpha);
        DrawFigure(
            Figure.Magician, magicianFeet, white: _juice.MagicianWhite, fallen: _simulation.MagicianHasFallen);
        foreach (Understudy understudy in _simulation.Understudies)
        {
            // The magician's own figure through a treatment, and never a figure of its own: tinted by the act it
            // came from and half there. The magician is the one bright figure on the stage.
            if (understudy.IsOnStage)
            {
                DrawFigure(
                    Figure.Magician,
                    Feet(understudy, alpha),
                    opacity: UnderstudyOpacity,
                    tint: UnderstudyTints[(understudy.Act - 1) % UnderstudyTints.Length]);
            }
        }

        foreach (Critic critic in _simulation.Critics)
        {
            Vector2 feet = Vector2.Lerp(critic.PreviousPosition, critic.Position, alpha);
            DrawFigure(Figure.Critic, feet, pale: critic.IsStunned, white: _juice.CriticWhite(critic.Id));
        }

        foreach (ThrownCard card in _simulation.ThrownCards)
        {
            // The card's position is the point of the floor it is over.
            Vector2 below = Vector2.Lerp(card.PreviousPosition, card.Position, alpha);
            Color face = card.ThrownByMagician ? ThrownCardFace : UnderstudysCardFace;
            DrawUpright(below, ThrownCardWidth, ThrownCardHeight, face, lift: ThrownCardLift);

            // A card thrown on this tick has not flown yet and has no way to trail along.
            Vector2 flown = card.Position - card.PreviousPosition;
            if (flown == Vector2.Zero)
            {
                continue;
            }

            // The trail: a streak from the middle of the card back along its flight, so that a card that is in the
            // air for an eighth of a second is seen. It never reaches back past where the card was thrown from.
            Vector2 middle = below - new Vector2(0f, ThrownCardLift + (ThrownCardHeight / 2f));
            Vector2 back = -Vector2.Normalize(flown)
                * MathF.Min(Juice.TrailLength, Vector2.Distance(below, card.ThrownFrom));
            FillTurned(
                middle + (back / 2f),
                new Vector2(back.Length(), Juice.TrailWidth),
                MathF.Atan2(back.Y, back.X),
                face * Juice.TrailOpacity,
                Depth(below));
        }

        _spriteBatch.End();

        // Over everything: the scraps in the air, then a performance that is over goes dark, and the box office's
        // hit points are a bar above it, a critic's height above, clear of the heads of the critics who stand
        // behind the box.
        _spriteBatch.Begin(transformMatrix: worldToScreen);
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

        // Only a closed show goes dark: an ovation is told from a loss at a glance.
        if (_simulation.Phase == Phase.Closed)
        {
            Fill(Vector2.Zero, Tuning.StageSize, Color.Black * 0.6f);
        }

        var bar = new Vector2(Tuning.BoxOfficeSize, 0.4f);
        Vector2 barTopLeft = boxOfficeFeet
            - new Vector2(bar.X / 2f, Tuning.BoxOfficeSize + CriticBodyHeight + CriticHeadSize + bar.Y);
        FillBar(barTopLeft, bar, _simulation.BoxOfficeHitPoints / Tuning.BoxOfficeHitPoints, HitPoints);

        // The magician has two small bars, told apart by place and by colour. Its hit points are under its feet, in
        // the colour of the box office's. The Vanish's is over its head: it fills as the Vanish comes back, and a
        // full bar is a Vanish that is ready. A fallen magician has no Vanish to wait for.
        var smallBar = new Vector2(1.6f, 0.25f);
        Vector2 atTheFeet = magicianFeet - new Vector2(smallBar.X / 2f, 0f);
        FillBar(
            atTheFeet + new Vector2(0f, 0.3f),
            smallBar,
            _simulation.MagicianHitPoints / Tuning.MagicianHitPoints,
            HitPoints);
        if (!_simulation.MagicianHasFallen)
        {
            FillBar(
                atTheFeet - new Vector2(0f, MagicianHeight + 0.3f + smallBar.Y),
                smallBar,
                1f - _simulation.VanishCooldownLeft,
                VanishBar);
        }

        // The act's applause: its share of the act's critics so far, with a notch at each threshold. A share past
        // a notch has earned what the notch stands for.
        float fullBar = MathF.Max(0.01f, Tuning.ApplauseSecondThreshold / ApplauseBarSecondNotchAt);
        var applauseTopLeft = new Vector2(
            (Tuning.StageSize.X - ApplauseBar.X) / 2f,
            MathF.Max(Tuning.StageFloorTop, WordsHeight + 0.4f + ApplauseBar.Y + (2f * ApplauseBarGap))
                - ApplauseBarGap - ApplauseBar.Y);
        FillBar(applauseTopLeft, ApplauseBar, _simulation.ActApplauseShare / fullBar, ApplauseGlow);
        foreach (float threshold in new[] { Tuning.ApplauseFirstThreshold, Tuning.ApplauseSecondThreshold })
        {
            Fill(
                applauseTopLeft + new Vector2((ApplauseBar.X * threshold / fullBar) - (ApplauseNotchWidth / 2f), -ApplauseNotchPast),
                new Vector2(ApplauseNotchWidth, ApplauseBar.Y + (2f * ApplauseNotchPast)),
                Words);
        }

        _spriteBatch.End();
        DrawWords(scale, corner, alpha, besideTheBar: barTopLeft + new Vector2(bar.X + 0.3f, bar.Y / 2f));
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
    // ponytail: every stretch of the route is measured twice a frame, 9,000 square roots an understudy and nine
    // understudies at most, for the curtain's one second. Keep each route's lengths when a frame feels it.
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
    /// The words of the screen: along the back wall the act, a line when no act is played or the magician has
    /// fallen in the one that is, and the act's time left; as the second act begins the one caption, over the
    /// understudy that is drawn <paramref name="alpha"/> between its last two ticks; and the box office's hit
    /// points as a number at <paramref name="besideTheBar"/>, the point just to the right of the middle of its
    /// bar's end. They are drawn in screen pixels, so they stay sharp: the font is asked for at the size the window
    /// makes of it, and <paramref name="scale"/> and <paramref name="corner"/> only say where on the screen a point
    /// of the stage is.
    /// </summary>
    private void DrawWords(float scale, Vector2 corner, float alpha, Vector2 besideTheBar)
    {
        if (_fonts is null)
        {
            return;
        }

        // The text's line is centred on the height of `at`, a point of the stage, with its left end there, its
        // middle (`anchor` 0.5) or its right end (1). Words `keptOnTheStage` are moved by as much as it takes to
        // have the whole line on the stage.
        void Write(float height, string text, Vector2 at, float anchor, Color color, bool keptOnTheStage = false)
        {
            // A whole number of pixels tall and on whole pixels: a glyph drawn between two pixels is smeared over
            // both. A window too small for words still has a pixel's worth.
            SpriteFontBase font = _fonts.GetFont(MathF.Max(1f, MathF.Round(height * scale)));
            var size = new Vector2(font.MeasureString(text).X, font.LineHeight);
            Vector2 topLeft = corner + (at * scale) - new Vector2(size.X * anchor, size.Y / 2f);
            if (keptOnTheStage)
            {
                // The far corner first: a line wider than the stage starts at the stage's left edge.
                topLeft = Vector2.Max(corner, Vector2.Min(topLeft, corner + (Tuning.StageSize * scale) - size));
            }

            _spriteBatch.DrawString(font, text, new Vector2(MathF.Round(topLeft.X), MathF.Round(topLeft.Y)), color);
        }

        // Halfway up the back wall, and on a stage with no wall just clear of the top edge.
        float line = MathF.Max(Tuning.StageFloorTop, WordsHeight + 0.4f) / 2f;

        // A second that has begun still shows: the time reads 0:00 only when the act is over.
        int seconds = (_simulation.ActTicksLeft + Simulation.TicksPerSecond - 1) / Simulation.TicksPerSecond;
        string? said = _simulation.Phase switch
        {
            Phase.BetweenActs => $"Act {_simulation.Act} is over. Press Enter or Start to go on.",
            Phase.Ovation => "A standing ovation! R starts a new performance.",
            Phase.Closed => "The box office fell. R starts a new performance.",
            Phase.Act when _simulation.MagicianHasFallen =>
                "The magician has fallen. The understudies carry on to the end of the act.",
            _ => null,
        };

        // Beside the act's number, how many of its critics are still to enter: from the curtain on.
        int toCome = _simulation.ActEntries.Count - _simulation.ActEntriesMade;
        string act = $"Act {_simulation.Act} of {Tuning.ActsInPerformance}";
        if (_simulation.Phase is Phase.Act or Phase.Curtain)
        {
            act += $": {toCome} to come";
        }

        _spriteBatch.Begin();
        Write(WordsHeight, act, new Vector2(1f, line), 0f, Words);
        Write(WordsHeight, $"{seconds / 60}:{seconds % 60:00}", new Vector2(Tuning.StageSize.X - 1f, line), 1f, Words);
        if (said is not null)
        {
            Write(WordsHeight, said, new Vector2(Tuning.StageSize.X / 2f, line), 0.5f, Magician);
        }

        // The first understudy is told once what it is, as the act it first appears in begins (vision 13): the
        // words are in its colour and over its head, wherever it is drawn, so they are its label and nobody
        // else's. How long they stay is read off the act's time left: it is no rule and no state.
        bool actHasJustBegun =
            _simulation.ActTicksLeft > (Tuning.ActLength - CaptionTimeInTheAct) * Simulation.TicksPerSecond;
        if (_simulation is { Act: 2, Phase: Phase.Curtain or Phase.Act, Understudies: [{ IsOnStage: true } first, ..] }
            && actHasJustBegun)
        {
            Write(
                CaptionHeight,
                Caption,
                Feet(first, alpha) - new Vector2(0f, CaptionLift),
                0.5f,
                UnderstudyTints[0],
                keptOnTheStage: true);
        }

        Write(NumberHeight, $"{MathF.Ceiling(_simulation.BoxOfficeHitPoints)}", besideTheBar, 0f, Words);
        _spriteBatch.End();
    }

    /// <summary>A bar that is <paramref name="share"/> full, from its left end.</summary>
    private void FillBar(Vector2 topLeft, Vector2 size, float share, Color color)
    {
        Fill(topLeft, size, HitPointsLost);
        Fill(topLeft, size with { X = size.X * Math.Clamp(share, 0f, 1f) }, color);
    }

    /// <summary>
    /// The one call that draws a figure. Whatever is asked of a figure's look is asked of this call, and nothing
    /// outside it knows that a figure is rectangles: no flash, no body and no fade paints over a figure with a
    /// shape of its own. When figures become sprites (T07c) the five things asked here must go on working:
    /// <paramref name="pale"/> is a tint; <paramref name="white"/> is a white copy of the sprite drawn over it,
    /// that thick, since a tint can only darken; <paramref name="fallen"/> is the sprite laid on its side, or a
    /// sprite of its own; <paramref name="opacity"/> is all that is drawn of the figure, that much see-through; and
    /// <paramref name="tint"/> is the colour an understudy's sprite is multiplied by.
    /// </summary>
    /// <param name="pale">A stunned critic: it has gone pale all over.</param>
    /// <param name="white">The flash of a figure that was hurt a moment ago: how far to white, from 0 to 1.</param>
    /// <param name="fallen">Lying flat where <paramref name="feet"/> is, and not standing on it.</param>
    /// <param name="tint">An understudy: the magician's figure in the colour of the act it came from.</param>
    private void DrawFigure(
        Figure figure,
        Vector2 feet,
        bool pale = false,
        float white = 0f,
        bool fallen = false,
        float opacity = 1f,
        Color? tint = null)
    {
        // A fallen figure is as long on the floor as it stood tall, with its middle where its feet were.
        float tall = figure switch
        {
            Figure.Magician => MagicianHeight,
            Figure.Critic => CriticBodyHeight + CriticHeadSize,
            _ => Tuning.BoxOfficeSize,
        };

        void Part(float width, float height, Color color, float lift = 0f)
        {
            color = Color.Lerp(color, Color.White, white) * opacity;
            if (fallen)
            {
                // What was `lift` above the feet is as far along the floor, and every part lies on the floor.
                Fill(
                    new Vector2(feet.X - (tall / 2f) + lift, feet.Y - width),
                    new Vector2(height, width),
                    color,
                    Depth(feet));
            }
            else
            {
                DrawUpright(feet, width, height, color, lift);
            }
        }

        switch (figure)
        {
            case Figure.Magician:
                Part(Tuning.MagicianRadius * 2f, MagicianHeight, tint ?? Magician);
                break;

            // A body with a paler head on it, so that the critics of a crowd can be told apart.
            case Figure.Critic:
                // ponytail: every critic is drawn as wide as the first kind. With a second kind (T15) the figure
                // is given its critic's own.
                Part(Tuning.EnemyKinds[0].Radius * 2f, CriticBodyHeight, pale ? CriticStunnedBody : CriticBody);
                Part(CriticHeadSize, CriticHeadSize, CriticHead, lift: CriticBodyHeight);
                break;

            case Figure.BoxOffice:
                Part(Tuning.BoxOfficeSize, Tuning.BoxOfficeSize, BoxOffice);
                break;
        }
    }

    /// <summary>
    /// What stands on a floor position is drawn upward from there, or from <paramref name="lift"/> above it. What
    /// stands lower on the screen is in front.
    /// </summary>
    private void DrawUpright(Vector2 feet, float width, float height, Color color, float lift = 0f) =>
        Fill(new Vector2(feet.X - (width / 2f), feet.Y - lift - height), new Vector2(width, height), color, Depth(feet));

    /// <summary>Where in a sorted batch what stands on <paramref name="feet"/> is drawn: the lower, the later.</summary>
    private float Depth(Vector2 feet) => Math.Clamp(feet.Y / Tuning.StageSize.Y, 0f, 1f);

    /// <summary>A filled circle in world units, flat on the floor.</summary>
    private void FillDisc(Vector2 middle, float radius, Color color)
    {
        // Strips that lie side by side and never on one another: where two overlapped, a see-through colour would
        // show twice as thick.
        float height = 2f * radius / DiscStrips;
        for (int i = 0; i < DiscStrips; i++)
        {
            // Each strip is as wide as the circle is at the strip's own middle.
            float y = ((i + 0.5f) * height) - radius;
            float halfWidth = MathF.Sqrt((radius * radius) - (y * y));
            Fill(middle + new Vector2(-halfWidth, y - (height / 2f)), new Vector2(2f * halfWidth, height), color);
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

    /// <summary>What <see cref="DrawFigure"/> can draw.</summary>
    private enum Figure
    {
        Magician,
        Critic,
        BoxOffice,
    }
}
