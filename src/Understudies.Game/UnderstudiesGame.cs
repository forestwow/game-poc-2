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

        _keysBefore = keys;
        _padBefore = pad;

        // The hit-stop: while the moment of a Vanish holds the world still, a frame's time is dropped. The clock
        // gets none of it and no tick is run, so the frame is drawn as the one before it was.
        double frameSeconds = gameTime.ElapsedGameTime.TotalSeconds;
        if (!_juice.Holds((float)frameSeconds))
        {
            // A closed show stands as it fell until R: its Step changes nothing. The juice goes on: what flew when
            // the show closed still settles.
            Vector2 move = ReadMove(keys, pad);
            int ticks = _clock.Advance(frameSeconds);
            for (int i = 0; i < ticks; i++)
            {
                Tick(new MagicianInput(move, _vanishAsked));
                _vanishAsked = false;
            }

            _juice.Advance((float)frameSeconds);
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // A closed show has no next tick to draw towards.
        DrawStage(_simulation.ShowClosed ? 1f : _clock.Alpha);
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
        // Two seconds right and down, out of every critic's range, and still there while critics gather at the box
        // office: more of them than the magician's cards can fell in time. Then two seconds back to the mark, which
        // is at the edge of the crowd by now, and three seconds still: the crowd turns on the magician and its hit
        // points go. On the first tick after thirty-one seconds a Vanish to the left: the cloud lies on the crowd,
        // and the frames of the next second show it, the stunned in it and the Vanish's bar part full. Two seconds
        // later the magician walks back into them, the Vanish's six units in forty ticks, and stands there until it
        // falls, a little before thirty-five seconds.
        const int second = Simulation.TicksPerSecond;
        for (int i = 0; i < _captureTicks; i++)
        {
            Tick(i switch
            {
                < 2 * second => new MagicianInput(new Vector2(1f, 1f)),
                < 26 * second => default,
                < 28 * second => new MagicianInput(new Vector2(-1f, -1f)),
                < 31 * second => default,
                31 * second => new MagicianInput(new Vector2(-1f, 0f), Vanish: true),
                < 33 * second => default,
                // Forty ticks at the magician's speed are the Vanish's six units: back to where it stood.
                < (33 * second) + 40 => new MagicianInput(new Vector2(1f, 0f)),
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
            // A door is a mat as wide as the door, the half of it that is on the floor. Only the first door is open.
            var half = new Vector2(Tuning.StageDoorWidth / 2f);
            Vector2 topLeft = Vector2.Max(Tuning.StageDoors[i] - half, floorTopLeft);
            Vector2 bottomRight = Vector2.Min(Tuning.StageDoors[i] + half, Tuning.StageSize);
            Fill(topLeft, bottomRight - topLeft, i == 0 ? OpenDoor : ShutDoor);
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
        foreach (Critic critic in _simulation.Critics)
        {
            Vector2 feet = Vector2.Lerp(critic.PreviousPosition, critic.Position, alpha);
            DrawFigure(Figure.Critic, feet, pale: critic.IsStunned, white: _juice.CriticWhite(critic.Id));
        }

        foreach (ThrownCard card in _simulation.ThrownCards)
        {
            // The card's position is the point of the floor it is over.
            Vector2 below = Vector2.Lerp(card.PreviousPosition, card.Position, alpha);
            DrawUpright(below, ThrownCardWidth, ThrownCardHeight, ThrownCardFace, lift: ThrownCardLift);

            // A card thrown on this tick has not flown yet and has no way to trail along.
            Vector2 flown = card.Position - card.PreviousPosition;
            if (flown == Vector2.Zero)
            {
                continue;
            }

            // The trail: a streak from the middle of the card back along its flight, so that a card that is in the
            // air for an eighth of a second is seen.
            // ponytail: the streak never reaches back past the magician, who throws every card there is. When an
            // understudy throws too, a card has to say where it was thrown from.
            Vector2 middle = below - new Vector2(0f, ThrownCardLift + (ThrownCardHeight / 2f));
            Vector2 back = -Vector2.Normalize(flown)
                * MathF.Min(Juice.TrailLength, Vector2.Distance(below, magicianFeet));
            FillTurned(
                middle + (back / 2f),
                new Vector2(back.Length(), Juice.TrailWidth),
                MathF.Atan2(back.Y, back.X),
                ThrownCardFace * Juice.TrailOpacity,
                Depth(below));
        }

        _spriteBatch.End();

        // Over everything: the scraps in the air, then a closed show goes dark, and the box office's hit points are
        // a bar above it, a critic's height above, clear of the heads of the critics who stand behind the box.
        _spriteBatch.Begin(transformMatrix: worldToScreen);
        foreach (var scrap in _juice.Scraps)
        {
            FillTurned(scrap.Middle, Juice.ScrapSize, scrap.Turn, ScrapOfPaper * scrap.Opacity);
        }

        if (_simulation.ShowClosed)
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
    /// shape of its own. When figures become sprites (T07c) the four things asked here must go on working:
    /// <paramref name="pale"/> is a tint; <paramref name="white"/> is a white copy of the sprite drawn over it,
    /// that thick, since a tint can only darken; <paramref name="fallen"/> is the sprite laid on its side, or a
    /// sprite of its own; and <paramref name="opacity"/> is all that is drawn of the figure, that much see-through.
    /// </summary>
    /// <param name="pale">A stunned critic: it has gone pale all over.</param>
    /// <param name="white">The flash of a figure that was hurt a moment ago: how far to white, from 0 to 1.</param>
    /// <param name="fallen">Lying flat where <paramref name="feet"/> is, and not standing on it.</param>
    private void DrawFigure(
        Figure figure, Vector2 feet, bool pale = false, float white = 0f, bool fallen = false, float opacity = 1f)
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
                Part(Tuning.MagicianRadius * 2f, MagicianHeight, Magician);
                break;

            // A body with a paler head on it, so that the critics of a crowd can be told apart.
            case Figure.Critic:
                Part(Tuning.CriticRadius * 2f, CriticBodyHeight, pale ? CriticStunnedBody : CriticBody);
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
