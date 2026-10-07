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

    // The same in every capture, so that a frame can be compared with the one before.
    private const ulong CaptureSeed = 1;

    private static readonly Color Surround = new(24, 18, 28);
    private static readonly Color Floor = new(96, 74, 58);
    private static readonly Color OpenDoor = new(222, 180, 104);
    private static readonly Color ShutDoor = new(66, 50, 42);
    private static readonly Color BoxOffice = new(150, 44, 52);
    private static readonly Color HitPoints = new(132, 204, 110);
    private static readonly Color HitPointsLost = new(30, 22, 30);
    private static readonly Color Magician = new(250, 226, 120);
    private static readonly Color CriticBody = new(62, 88, 156);
    private static readonly Color CriticHead = new(226, 216, 200);
    private static readonly Color ThrownCardFace = new(250, 246, 236);

    private readonly SimulationClock _clock = new();
    private readonly string? _capturePath;
    private readonly int _captureTicks;
    private Simulation _simulation;
    private bool _captured;
    private KeyboardState _keysBefore;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;

    /// <summary>With a <paramref name="capturePath"/> the game does not play: it saves one frame there and exits.</summary>
    public UnderstudiesGame(Tuning tuning, string? capturePath, int captureTicks)
    {
        _simulation = capturePath is null ? NewShow(tuning) : new Simulation(tuning, CaptureSeed);
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
        }

        _keysBefore = keys;

        // A closed show takes no more input: it stands as it fell until R.
        MagicianInput input = ReadInput();
        int ticks = _clock.Advance(gameTime.ElapsedGameTime.TotalSeconds);
        for (int i = 0; i < ticks && !_simulation.ShowClosed; i++)
        {
            _simulation.Step(input);
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

    private static MagicianInput ReadInput()
    {
        KeyboardState keys = Keyboard.GetState();
        var stick = GamePad.GetState(PlayerIndex.One).ThumbSticks.Left;

        float Held(Keys letter, Keys arrow) => keys.IsKeyDown(letter) || keys.IsKeyDown(arrow) ? 1f : 0f;

        // A stick pushed up reports +Y; the stage's y grows downward.
        return new MagicianInput(new Vector2(
            stick.X + Held(Keys.D, Keys.Right) - Held(Keys.A, Keys.Left),
            -stick.Y + Held(Keys.S, Keys.Down) - Held(Keys.W, Keys.Up)));
    }

    /// <summary>Walks a fixed script, draws the frame it ends on and saves it as a PNG.</summary>
    private void Capture(string path)
    {
        // A third of a second right and down, then still: the magician ends in front of the box office's corner, in
        // range of the last stretch of the way the critics of the first door come, so a frame shows the fight.
        var rightAndDown = new MagicianInput(new Vector2(1f, 1f));
        for (int i = 0; i < _captureTicks; i++)
        {
            _simulation.Step(i < 20 ? rightAndDown : default);
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
        Matrix worldToScreen = Matrix.CreateScale(scale, scale, 1f) * Matrix.CreateTranslation(corner.X, corner.Y, 0f);

        GraphicsDevice.Clear(Surround);

        // The floor and what lies flat on it.
        _spriteBatch.Begin(transformMatrix: worldToScreen);
        Fill(Vector2.Zero, Tuning.StageSize, Floor);
        for (int i = 0; i < Tuning.StageDoors.Count; i++)
        {
            // A door is a mat as wide as the door, the half of it that is on the stage. Only the first door is open.
            var half = new Vector2(Tuning.StageDoorWidth / 2f);
            Vector2 topLeft = Vector2.Max(Tuning.StageDoors[i] - half, Vector2.Zero);
            Vector2 bottomRight = Vector2.Min(Tuning.StageDoors[i] + half, Tuning.StageSize);
            Fill(topLeft, bottomRight - topLeft, i == 0 ? OpenDoor : ShutDoor);
        }

        _spriteBatch.End();

        // What stands on the floor: the lower on the screen, the later it is drawn (DrawUpright gives the depth).
        _spriteBatch.Begin(SpriteSortMode.FrontToBack, transformMatrix: worldToScreen);

        // The box office is drawn from halfway between the middle of its circle and the circle's front. A critic
        // that touches the circle from in front then overlaps the foot of the box, one at a side stands against its
        // wall, and one in front of a corner is not hidden behind it.
        Vector2 boxOfficeFeet = Tuning.BoxOfficePosition + new Vector2(0f, Tuning.BoxOfficeSize / 4f);
        DrawUpright(boxOfficeFeet, Tuning.BoxOfficeSize, Tuning.BoxOfficeSize, BoxOffice);
        DrawUpright(
            Vector2.Lerp(_simulation.MagicianPreviousPosition, _simulation.MagicianPosition, alpha),
            Tuning.MagicianRadius * 2f,
            MagicianHeight,
            Magician);
        foreach (Critic critic in _simulation.Critics)
        {
            // A body with a paler head on it, so that the critics of a crowd can be told apart.
            Vector2 feet = Vector2.Lerp(critic.PreviousPosition, critic.Position, alpha);
            DrawUpright(feet, Tuning.CriticRadius * 2f, CriticBodyHeight, CriticBody);
            DrawUpright(feet, CriticHeadSize, CriticHeadSize, CriticHead, lift: CriticBodyHeight);
        }

        foreach (ThrownCard card in _simulation.ThrownCards)
        {
            // The card's position is the point of the floor it is over.
            Vector2 below = Vector2.Lerp(card.PreviousPosition, card.Position, alpha);
            DrawUpright(below, ThrownCardWidth, ThrownCardHeight, ThrownCardFace, lift: ThrownCardLift);
        }

        _spriteBatch.End();

        // Over everything: a closed show goes dark, and the box office's hit points are a bar above it, a critic's
        // height above, clear of the heads of the critics who stand behind the box.
        _spriteBatch.Begin(transformMatrix: worldToScreen);
        if (_simulation.ShowClosed)
        {
            Fill(Vector2.Zero, Tuning.StageSize, Color.Black * 0.6f);
        }

        var bar = new Vector2(Tuning.BoxOfficeSize, 0.4f);
        Vector2 barTopLeft = boxOfficeFeet
            - new Vector2(bar.X / 2f, Tuning.BoxOfficeSize + CriticBodyHeight + CriticHeadSize + bar.Y);
        float left = Math.Clamp(_simulation.BoxOfficeHitPoints / Tuning.BoxOfficeHitPoints, 0f, 1f);
        Fill(barTopLeft, bar, HitPointsLost);
        Fill(barTopLeft, bar with { X = bar.X * left }, HitPoints);
        _spriteBatch.End();
    }

    /// <summary>
    /// A figure stands on its floor position and is drawn upward from there, or from <paramref name="lift"/> above
    /// it. What stands lower on the screen is in front.
    /// </summary>
    private void DrawUpright(Vector2 feet, float width, float height, Color color, float lift = 0f) =>
        Fill(
            new Vector2(feet.X - (width / 2f), feet.Y - lift - height),
            new Vector2(width, height),
            color,
            Math.Clamp(feet.Y / Tuning.StageSize.Y, 0f, 1f));

    /// <summary>A filled rectangle in world units.</summary>
    /// <param name="depth">Counts only in a sorted batch, from 0 to 1: the greater depth is drawn later.</param>
    private void Fill(Vector2 topLeft, Vector2 size, Color color, float depth = 0f) =>
        _spriteBatch.Draw(
            _pixel, topLeft, null, color, 0f, Microsoft.Xna.Framework.Vector2.Zero, size, SpriteEffects.None, depth);
}
