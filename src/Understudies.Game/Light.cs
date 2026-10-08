using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Understudies.Core;

using Vector2 = System.Numerics.Vector2;

namespace Understudies.Game;

// The house rules, and the first of them: the spotlight night (plan T56, L6 of "The ladder"; the document's §1.4).
// A rule is a name (Night.RuleNames) that a night has in nights.json or that the command line put on the night
// played (--rule); the simulation knows of none. The spotlight night is the view's alone: what is dark is not
// drawn, and is still there. A fifth part of the game's one class.
internal sealed partial class UnderstudiesGame
{
    // The dark, laid over the stage where no lamp reaches: what is of the set and of the magician's own company
    // (the floor, the curtain, a shut door, the box office, an understudy) is seen through it, dimly.
    private static readonly Color TheDark = new Color(6, 4, 10) * 0.82f;

    // A lamp's edge: over this many units the light goes out, in as many steps as LampBands.
    private const float LampSoft = 1.6f;
    private const int LampBands = 4;

    // A lamp is cut out of the dark in rows this many screen pixels tall.
    private const float LampRow = 4f;

    // The light of an open door (the document's "cone"): circles from the door's picture out onto the floor
    // toward the box office, the further the wider. The view's numbers: only the magician's own circle is the
    // tuning's (spotlightRadius), since the document gives a number to that one alone.
    private const float DoorLampReach = 5.4f;
    private const float DoorLampNear = 2.6f;
    private const float DoorLampFar = 4.2f;
    private const int DoorLamps = 4;

    // The first of them is this far up the door's picture, which stands on the door's line.
    private const float DoorLampLift = 1f;

    // The box office is what is defended: it stands in a glow of its own that takes this share of the dark off it
    // and shows nobody (Lit does not count it): a critic that strikes it in the dark is not seen, and the box
    // office is seen to flash.
    private const float BoxOfficeGlow = 0.55f;
    private const float BoxOfficeGlowRadius = 3.6f;

    // What a lamp takes out of the dark: nothing of the colour it is drawn in is left, only that much less dark.
    private static readonly BlendState Cut = new()
    {
        ColorSourceBlend = Blend.Zero,
        AlphaSourceBlend = Blend.Zero,
        ColorDestinationBlend = Blend.InverseSourceAlpha,
        AlphaDestinationBlend = Blend.InverseSourceAlpha,
    };

    // The rules the command line put on whatever night is played.
    private readonly IReadOnlyList<string> _askedRules;

    // The dark of the frame being drawn, as large as what is drawn to, and the lamps it was cut by. Made by the
    // first frame of a spotlight night and by none before it: a game without the rule has neither.
    private RenderTarget2D? _dark;
    private readonly List<(Vector2 Middle, float Radius)> _lamps = [];

    /// <summary>
    /// Whether the night that is played has a house rule: its own in nights.json, or the command line's.
    /// ponytail: the one place a rule is switched on, and it is by hand. The document draws a night's rules from
    /// pools by the show's seed; with one rule there is nothing to draw from. The draw (a stream, the
    /// compatibility check) comes with the second rule, and answers here.
    /// </summary>
    private bool HasRule(string rule) =>
        _askedRules.Contains(rule) || (_night is { } number && _nights.Any(night => night.Number == number && night.Has(rule)));

    /// <summary>The spotlight night's dark is on the stage: not at the ovation, where the house lights come up.</summary>
    private bool LightsAreDown => HasRule(Night.Spotlight) && _simulation.Phase != Phase.Ovation;

    /// <summary>
    /// How much of what stands at a place of the floor is seen, from 0 to 1: all of it when the lights are up,
    /// and on the spotlight night what the lamps of the frame show. An enemy, standing or fallen, is drawn this
    /// thick, by where its feet are, so that it comes out of the dark whole and not head first.
    /// </summary>
    private float Lit(Vector2 at)
    {
        if (!LightsAreDown)
        {
            return 1f;
        }

        float most = 0f;
        foreach ((Vector2 middle, float radius) in _lamps)
        {
            most = MathF.Max(most, Math.Clamp((radius - Vector2.Distance(at, middle)) / LampSoft, 0f, 1f));
        }

        return most;
    }

    /// <summary>
    /// Makes the dark of this frame: the stage's dark with a hole for every lamp, in a picture of its own that
    /// <see cref="LayTheDark"/> lays over the stage. Called before anything of the frame is drawn, since what is
    /// drawn to is set aside for it and comes back empty.
    /// </summary>
    private void MakeTheDark(Vector2 magicianFeet)
    {
        // The lamps: the magician's own, and at every open door a row of circles toward the box office, the
        // first on the door's picture.
        _lamps.Clear();
        _lamps.Add((magicianFeet, Tuning.SpotlightRadius));
        for (int i = 0; i < Tuning.StageDoors.Count; i++)
        {
            if (!_simulation.DoorIsOpen(i))
            {
                continue;
            }

            Vector2 door = Tuning.StageDoors[i].Position;
            Vector2 toward = Tuning.BoxOfficePosition - door;
            toward = toward == Vector2.Zero ? Vector2.UnitY : Vector2.Normalize(toward);
            for (int lamp = 0; lamp < DoorLamps; lamp++)
            {
                float along = lamp / (DoorLamps - 1f);
                _lamps.Add((
                    Vector2.Lerp(door - new Vector2(0f, DoorLampLift), door + (toward * DoorLampReach), along),
                    MathHelper.Lerp(DoorLampNear, DoorLampFar, along)));
            }
        }

        Viewport viewport = GraphicsDevice.Viewport;
        if (_dark is null || _dark.Width != viewport.Width || _dark.Height != viewport.Height)
        {
            _dark?.Dispose();
            _dark = new RenderTarget2D(GraphicsDevice, viewport.Width, viewport.Height);
        }

        RenderTargetBinding[] drawnTo = GraphicsDevice.GetRenderTargets();
        GraphicsDevice.SetRenderTarget(_dark);
        GraphicsDevice.Clear(TheDark);
        _spriteBatch.Begin(blendState: Cut, samplerState: SamplerState.PointClamp, transformMatrix: _worldToScreen);
        foreach ((Vector2 middle, float radius) in _lamps)
        {
            CutALamp(middle, radius, 1f);
        }

        CutALamp(Tuning.BoxOfficePosition, BoxOfficeGlowRadius, BoxOfficeGlow);
        _spriteBatch.End();
        GraphicsDevice.SetRenderTargets(drawnTo);
    }

    /// <summary>
    /// A lamp's hole in the dark: <paramref name="most"/> of the dark is gone from its middle, and less in steps
    /// over <see cref="LampSoft"/> to its edge. Each band takes its share of what the bands outside it left.
    /// </summary>
    private void CutALamp(Vector2 middle, float radius, float most)
    {
        float row = LampRow / _scale;
        for (int band = 0; band < LampBands; band++)
        {
            float reach = radius - (LampSoft * band / LampBands);
            float share = most / LampBands / (1f - (most * band / LampBands));
            for (float y = -reach; y < reach; y += row)
            {
                float mid = MathF.Min(y + (row / 2f), reach);
                float halfWidth = MathF.Sqrt(MathF.Max(0f, (reach * reach) - (mid * mid)));
                Fill(middle + new Vector2(-halfWidth, y), new Vector2(2f * halfWidth, MathF.Min(row, reach - y)), Color.White * share);
            }
        }
    }

    /// <summary>
    /// Lays the dark over the stage: over the set and every figure, and under whatever is drawn after it, which
    /// is seen whatever the light (a thrown card, every effect of a card, applause, the footlights, the bars and
    /// the words).
    /// </summary>
    private void LayTheDark()
    {
        _spriteBatch.Begin();
        _spriteBatch.Draw(_dark, Microsoft.Xna.Framework.Vector2.Zero, Color.White);
        _spriteBatch.End();
    }

    /// <summary>A house rule as the poster says it: its name and its one sentence.</summary>
    private static string RuleInWords(string rule) => rule switch
    {
        Night.Spotlight => "The spotlight night: you see what the lamps show.",
        _ => rule,
    };
}
