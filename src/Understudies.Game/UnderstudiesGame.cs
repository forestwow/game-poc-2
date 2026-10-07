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

    private static readonly Color Surround = new(24, 18, 28);
    private static readonly Color Floor = new(96, 74, 58);
    private static readonly Color BoxOffice = new(150, 44, 52);
    private static readonly Color Magician = new(250, 226, 120);

    private readonly Simulation _simulation = new();
    private readonly SimulationClock _clock = new();
    private readonly string? _capturePath;
    private readonly int _captureTicks;
    private bool _captured;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;

    /// <summary>With a <paramref name="capturePath"/> the game does not play: it saves one frame there and exits.</summary>
    public UnderstudiesGame(string? capturePath, int captureTicks)
    {
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

        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        MagicianInput input = ReadInput();
        int ticks = _clock.Advance(gameTime.ElapsedGameTime.TotalSeconds);
        for (int i = 0; i < ticks; i++)
        {
            _simulation.Step(input);
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        DrawStage(_clock.Alpha);
        base.Draw(gameTime);
    }

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
        var rightAndDown = new MagicianInput(new Vector2(1f, 1f));
        for (int i = 0; i < _captureTicks; i++)
        {
            _simulation.Step(rightAndDown);
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

        Vector2 magician = Vector2.Lerp(_simulation.MagicianPreviousPosition, _simulation.MagicianPosition, alpha);
        // What is lower on the screen is in front.
        // ponytail: two things, so an if; a sort by y when there are more figures (T04).
        bool magicianInFront = magician.Y >= Tuning.BoxOfficePosition.Y;

        GraphicsDevice.Clear(Surround);
        _spriteBatch.Begin(transformMatrix: worldToScreen);
        Fill(Vector2.Zero, Tuning.StageSize, Floor);
        if (!magicianInFront)
        {
            DrawUpright(magician, Tuning.MagicianRadius * 2f, MagicianHeight, Magician);
        }

        DrawUpright(Tuning.BoxOfficePosition, Tuning.BoxOfficeSize, Tuning.BoxOfficeSize, BoxOffice);
        if (magicianInFront)
        {
            DrawUpright(magician, Tuning.MagicianRadius * 2f, MagicianHeight, Magician);
        }

        _spriteBatch.End();
    }

    /// <summary>A figure stands on its floor position and is drawn upward from there.</summary>
    private void DrawUpright(Vector2 feet, float width, float height, Color color) =>
        Fill(new Vector2(feet.X - (width / 2f), feet.Y - height), new Vector2(width, height), color);

    /// <summary>A filled rectangle in world units.</summary>
    private void Fill(Vector2 topLeft, Vector2 size, Color color) =>
        _spriteBatch.Draw(
            _pixel, topLeft, null, color, 0f, Microsoft.Xna.Framework.Vector2.Zero, size, SpriteEffects.None, 0f);
}
