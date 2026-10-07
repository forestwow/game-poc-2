using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Understudies.Game;

// The base class is spelled out because `Game` alone means this namespace here.
internal sealed class UnderstudiesGame : Microsoft.Xna.Framework.Game
{
    public UnderstudiesGame()
    {
        _ = new GraphicsDeviceManager(this) { PreferredBackBufferWidth = 1280, PreferredBackBufferHeight = 720 };
        Window.Title = "The Understudies";
        IsMouseVisible = true;
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(24, 18, 28));
        base.Draw(gameTime);
    }
}
