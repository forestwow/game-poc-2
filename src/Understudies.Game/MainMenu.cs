using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Understudies.Core;

using Vector2 = System.Numerics.Vector2;

namespace Understudies.Game;

// The main menu (plan T53, S7 of "The screens"): what the game opens on and what Esc comes back to from a show.
// Two entries, "Perform" and "Quit", one of them chosen; the title; the line of the controls; and the magician
// with five of its understudies on the game's own set. It holds no rule and reads none but the tuning's numbers:
// the show that stands behind it is never played. It is a part of the game's one class because it draws with
// that class's batch, figures and words.
internal sealed partial class UnderstudiesGame
{
    // Seconds from the menu's coming up in which nothing is taken and Esc does not quit, as an encore's first
    // moment is guarded: the Esc that left the show, pressed twice, must not close the game as well.
    private const float MenuGuardTime = EncoreGuardTime;

    // The menu's set (the design's screen pixels in a window 1280 wide, in world units): a curtain twice as
    // tall as an act's, a gold line twice as thick with a line of ink under it, and a glow of the footlights
    // that rises over the last of the floor.
    // ponytail: the menu is laid out for a stage about as large as the committed one (48 by 27): the words from
    // its top left corner and the cast from its bottom right. A stage much smaller has the two on one another.
    private const float MenuCurtain = 2f;
    private const float MenuFloorTop = 6f;
    private const float MenuGoldLine = 0.3f;
    private const float MenuGlowTall = 6f;
    private const float MenuGlowRow = 0.25f;
    private const float MenuGlow = 0.22f;

    // The words, down the left side: a small line in pink capitals, the title in two lines of Pixelify Sans
    // (98 screen pixels at 1280 wide: seven times the 14 its squares are whole at) with an outline twice the
    // stage's, and one sentence broken to a width.
    private const float MenuSide = 3f;
    private const float MenuSmallLine = 7.55f;
    private const float MenuSmallHeight = 0.675f;
    private const float MenuSmallSpacing = 0.15f;
    private const float TitleFirstLine = 10f;
    private const float TitlePitch = 3.3f;
    private const float TitleHeight = 3.675f;
    private const float TitleOutline = 0.15f;
    private const float MenuSentenceLine = 16.2f;
    private const float MenuSentenceHeight = 0.9f;
    private const float MenuSentencePitch = 1.1f;
    private const float MenuSentenceWide = 21f;

    // The entries: paper in an ink border with a hard shadow, a name at the left and a small note at the right.
    // The first is taller, as the design has it. The chosen one is gold in a pink ring, as a chosen card is
    // ringed, and the other is cream.
    private const float EntriesTop = 18.75f;
    private const float EntryWide = 14.25f;
    private const float EntryTall = 2.1f;
    private const float FirstEntryTall = 2.4f;
    private const float EntriesApart = 0.5f;
    private const float EntryPad = 0.75f;
    private const float EntryBorder = 0.1125f;
    private const float EntryShadow = 0.15f;
    private const float EntryNoteHeight = 0.6f;
    private const int Perform = 0;
    private const int Quit = 1;
    private const int Entries = 2;

    // The line of the controls, over the footlights.
    private const float ControlsLine = 24.95f;
    private const float ControlsHeight = 0.6f;

    // The cast, from the stage's bottom right corner: the magician at three screen pixels to a sprite pixel in
    // a window 1280 wide and five understudies at two, whatever FiguresMeasure is: a poster's figures, larger
    // than the stage's. The understudies are those of acts five to one from the left, each this far left of
    // the magician and its feet this far up from the stage's foot.
    private const float MenuMagicianMeasure = 2.4f;
    private const float MenuUnderstudyMeasure = 1.6f;
    private static readonly Vector2 MenuMagician = new(5.85f, 4.05f);
    private const float MenuUnderstudiesFrom = 3f;
    private const float MenuUnderstudiesApart = 2.625f;
    private static readonly float[] MenuUnderstudiesUp = [3.75f, 4.5f, 3.6f, 4.65f, 3.9f];

    private bool _onTheMenu;
    private int _chosenEntry;

    /// <summary>
    /// The menu, and no show: the one way to it. The show that was played is given up, and the one that stands
    /// behind the menu is new and is never played, so nothing of the last one is left in the view.
    /// </summary>
    private void ShowTheMenu()
    {
        StartAgain();
        _onTheMenu = true;
        _chosenEntry = Perform;
        _guardLeft = MenuGuardTime;
    }

    /// <summary>
    /// A frame's presses on the menu. Up and down (the arrows, W and S, a gamepad's d-pad or its left stick) move
    /// the choice; Enter, Space or the gamepad's A take the chosen entry; Esc quits. Every one of them is a press
    /// and never a hold, as in the program, and in the menu's first moment (<see cref="MenuGuardTime"/>) the
    /// choice moves and nothing else is done.
    /// </summary>
    private void ChooseOnTheMenu(KeyboardState keys, GamePadState pad)
    {
        bool Pressed(Keys key) => keys.IsKeyDown(key) && !_keysBefore.IsKeyDown(key);
        bool PadPressed(Buttons button) => pad.IsButtonDown(button) && _padBefore.IsButtonUp(button);

        // A stick counts when it comes up or down, as a key does when it goes down. Pushed up it reports +Y.
        static int Lean(GamePadState pad) =>
            pad.ThumbSticks.Left.Y > StickLean ? -1 : pad.ThumbSticks.Left.Y < -StickLean ? 1 : 0;

        int lean = Lean(pad);
        int step = (lean != Lean(_padBefore) ? lean : 0)
            + (Pressed(Keys.Down) || Pressed(Keys.S) || PadPressed(Buttons.DPadDown) ? 1 : 0)
            - (Pressed(Keys.Up) || Pressed(Keys.W) || PadPressed(Buttons.DPadUp) ? 1 : 0);
        _chosenEntry = Math.Clamp(_chosenEntry + step, 0, Entries - 1);
        if (_guardLeft > 0f)
        {
            return;
        }

        bool taken = Pressed(Keys.Enter) || Pressed(Keys.Space) || PadPressed(Buttons.A);
        if (Pressed(Keys.Escape) || (taken && _chosenEntry == Quit))
        {
            Exit();
        }
        else if (taken)
        {
            // ponytail: a card's chime, which is the chime of a piece of applause picked up.
            StartAgain();
            _sound.Play(TickEventKind.ApplausePickedUp);
        }
    }

    /// <summary>The menu's frame: the set, the cast, the words and the entries.</summary>
    private void DrawTheMenu()
    {
        FitTheStage(shake: Vector2.Zero);
        GraphicsDevice.Clear(Surround);
        LayTheSet(Vector2.Zero, MenuFloorTop, MenuCurtain, MenuGoldLine);

        // Flat on the floor: the gold line's shadow, and the footlights' glow in rows, stronger toward the foot.
        Vector2 stage = Tuning.StageSize;
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _worldToScreen);
        Fill(new Vector2(0f, MenuFloorTop + MenuGoldLine), new Vector2(stage.X, MenuGoldLine / 2f), OutlineInk);
        for (float up = MenuGlowTall; up > 0f; up -= MenuGlowRow)
        {
            Fill(
                new Vector2(0f, stage.Y - up),
                new Vector2(stage.X, MenuGlowRow),
                Magician * (MenuGlow * (1f - (up / MenuGlowTall))));
        }

        _spriteBatch.End();

        // The cast stands as on the stage: the lower on the screen, the later drawn. An understudy is the
        // magician's figure in the tint of its act, as everywhere.
        _spriteBatch.Begin(SpriteSortMode.FrontToBack, samplerState: SamplerState.PointClamp, transformMatrix: _worldToScreen);
        Vector2 magician = stage - MenuMagician;
        DrawFigure(Figure.Magician, magician, measure: MenuMagicianMeasure);
        for (int i = 0; i < MenuUnderstudiesUp.Length; i++)
        {
            DrawFigure(
                Figure.Magician,
                new Vector2(
                    magician.X - MenuUnderstudiesFrom - ((MenuUnderstudiesUp.Length - 1 - i) * MenuUnderstudiesApart),
                    stage.Y - MenuUnderstudiesUp[i]),
                opacity: UnderstudyOpacity,
                cardboard: TintOfAct(MenuUnderstudiesUp.Length - i),
                measure: MenuUnderstudyMeasure);
        }

        _spriteBatch.End();

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _worldToScreen);
        for (float x = FootlightGap / 2f; x < stage.X; x += FootlightGap)
        {
            DrawFigure(Figure.Footlight, new Vector2(x, stage.Y));
        }

        _spriteBatch.End();

        // The words and the paper, in screen pixels. The small line is in the sentences' face, as every label is
        // (plan T45): Pixelify's capital C is read as an O at that size.
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        Write(
            Face.Sentence,
            MenuSmallHeight,
            "TONIGHT, AT THE STREET CORNER",
            new Vector2(MenuSide, MenuSmallLine),
            0f,
            ApplauseCount,
            spacing: MenuSmallSpacing);
        Write(Face.Heading, TitleHeight, "The", new Vector2(MenuSide, TitleFirstLine), 0f, Magician, outlineWidth: TitleOutline);
        Write(
            Face.Heading,
            TitleHeight,
            "Understudies",
            new Vector2(MenuSide, TitleFirstLine + TitlePitch),
            0f,
            Magician,
            outlineWidth: TitleOutline);

        const string sentence =
            "Where you run is what you build. Every act you play leaves a cardboard you that plays it again.";
        float line = MenuSentenceLine;
        foreach (string words in Wrapped(Face.Sentence, MenuSentenceHeight, sentence.Split(' '), MenuSentenceWide))
        {
            Write(Face.Sentence, MenuSentenceHeight, words, new Vector2(MenuSide, line), 0f, Words);
            line += MenuSentencePitch;
        }

        // The entries. What "Perform" starts is said in the tuning's numbers of now.
        string acts = string.Create(
            CultureInfo.InvariantCulture, $"{Tuning.ActsInPerformance} acts of {Tuning.ActLength:0.#} s");
        float top = EntriesTop;
        foreach ((int entry, string name, string note) in new[] { (Perform, "Perform", acts), (Quit, "Quit", "Esc") })
        {
            var topLeft = new Vector2(MenuSide, top);
            var size = new Vector2(EntryWide, entry == Perform ? FirstEntryTall : EntryTall);
            bool chosen = entry == _chosenEntry;
            Paper(topLeft, size, chosen ? Magician : ThrownCardFace, border: EntryBorder, shadow: EntryShadow);
            if (chosen)
            {
                RingAbout(topLeft, size, ApplauseGlow);
            }

            Write(Face.Heading, NameHeight, name, topLeft + new Vector2(EntryPad, size.Y / 2f), 0f, OutlineInk, onPaper: true);
            Write(
                Face.Sentence,
                EntryNoteHeight,
                note,
                topLeft + new Vector2(size.X - EntryPad, size.Y / 2f),
                1f,
                FaintInk,
                onPaper: true);
            top += size.Y + EntriesApart;
        }

        Write(
            Face.Sentence,
            ControlsHeight,
            "Move: WASD, the arrows or the stick  ·  Vanish: Space or A  ·  Go on: Enter or Start  ·  R starts a new show  ·  Esc comes back here",
            new Vector2(stage.X / 2f, ControlsLine),
            0.5f,
            Words);
        _spriteBatch.End();
    }
}
