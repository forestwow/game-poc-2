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

    // The plain tuning and the nights as they were last read, which a night's tuning is composed from; the one
    // night the command line asked for, whatever the progress says; and where the progress is kept, null for a
    // game that keeps none (a capture, and a night that was asked for).
    private Tuning _plain;
    private IReadOnlyList<Night> _nights;
    private readonly int? _askedNight;
    private string? _progressPath;
    private Progress _progress;

    // The night that the show just ended has opened, for the line that says so; null when it opened none.
    private int? _opened;

    // The night that is played, or whose poster is up (a shut night's too), or that "Perform" opens the poster of. Its tuning is the simulation's: StartAgain makes the next
    // show from that, so R plays the same night again. Null for the capture of a show that asked for no night,
    // which plays the plain tuning as it is.
    private int? _night;

    /// <summary>The nights that are open: the one that was asked for, or those the progress has opened.</summary>
    private IReadOnlyList<int> Unlocked => _askedNight is { } asked ? [asked] : _progress.Unlocked(_nights);

    /// <summary>A night is chosen: the numbers are its own from here, for the show behind the menu and the next.</summary>
    private void ChooseTheNight(int night)
    {
        _night = night;
        _simulation.Tuning = Night.Compose(_plain, _nights, night);
    }

    /// <summary>
    /// The player's progress as the file has it, and none for a game that keeps none. The console is told where
    /// it is kept, and when a file that could not be read was put aside. A file that is there and was not read
    /// and not put aside (it could not be read, or moved, or is a newer game's) is not this game's to write
    /// over: the path is given up for this run, and the progress is remembered while the game runs and no longer.
    /// </summary>
    private Progress LoadTheProgress()
    {
        if (_progressPath is not { } path)
        {
            return Progress.None;
        }

        bool wasThere = File.Exists(path);
        Progress progress = Progress.Load(path, out string? notKept);
        if (notKept is not null)
        {
            Console.Error.WriteLine($"Progress not read from {path}: {notKept}. The file is left as it is, and nothing is kept of this run");
            _progressPath = null;
        }
        else
        {
            Console.WriteLine(wasThere && !File.Exists(path)
                ? $"Progress not read from {path}: the file is kept as {path}{Progress.DamagedSuffix} and the game starts with none"
                : $"Progress is kept in {path}");
        }

        return progress;
    }

    /// <summary>
    /// The show that is played is recorded as played: its night, the act it is in, and whether it ended in its
    /// ovation. The file is written at once, whole. A file that cannot be written is said on the console and the
    /// game goes on with what it remembers.
    /// </summary>
    private void Remember()
    {
        // A night that was asked for is nobody's progress, and a capture of the plain tuning is no night.
        if (_askedNight is not null || _night is not { } night)
        {
            return;
        }

        // Remembered while the game runs even where there is no file to keep it in.
        IReadOnlyList<int> before = Unlocked;
        _progress = _progress.With(night, _simulation.Act, won: _simulation.Phase == Phase.Ovation);
        _opened = Unlocked.Except(before).Cast<int?>().FirstOrDefault();
        if (_progressPath is null)
        {
            return;
        }

        try
        {
            _progress.Save(_progressPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Progress not written to {_progressPath}: {exception.Message}");
        }
    }

    /// <summary>
    /// A show is left by Esc or R. One that has ended is recorded already. One left while its first act is still
    /// played is not recorded at all: nothing of the night was played through, and "a lost night counts" is said
    /// of a night lost, not of one opened and shut. Once the first act is over (its program, the stage between
    /// the first two acts, and every act after) it is a night played, with the act it was left in.
    /// </summary>
    private void GiveUp()
    {
        if (_simulation.Phase is not (Phase.Ovation or Phase.Closed)
            && (_simulation.Act > 1 || _simulation.Phase is Phase.Program or Phase.BetweenActs))
        {
            Remember();
        }
    }

    /// <summary>
    /// What a show that is over says of the two keys, and before them of the night it has just opened, when it
    /// has: R is the poster, of the new night when there is one and of this night otherwise.
    /// </summary>
    private string TheWayOn =>
        _opened is { } night ? $"Night {night} is open. R: its poster  ·  Esc: the menu." : "R: the poster  ·  Esc: the menu.";

    /// <summary>
    /// The menu, and no show: the one way to it. The show that was played is given up, and the one that stands
    /// behind the menu is new and is never played, so nothing of the last one is left in the view.
    /// </summary>
    private void ShowTheMenu()
    {
        // The newest night that is open is the one "Perform" opens the poster of: a night just opened is there at once.
        ChooseTheNight(Unlocked[^1]);
        StartAgain();
        _onTheMenu = true;
        _chosenEntry = Perform;
        _guardLeft = MenuGuardTime;
    }

    /// <summary>
    /// A frame's presses on the menu. Up and down (the arrows, W and S, a gamepad's d-pad or its left stick) move
    /// the choice; Enter, Space or the gamepad's A or Start take the chosen entry; Esc or the gamepad's Back
    /// quits. Every one of them is a press and never a hold, as in the program, and in the menu's first moment
    /// (<see cref="MenuGuardTime"/>) the choice moves and nothing else is done.
    /// </summary>
    private void ChooseOnTheMenu()
    {
        _chosenEntry = Math.Clamp(_chosenEntry + StepDown(), 0, Entries - 1);
        if (_guardLeft > 0f)
        {
            return;
        }

        // Start takes as A does, here and on the poster.
        bool taken = Pressed(Keys.Enter) || Pressed(Keys.Space) || PadPressed(Buttons.A) || PadPressed(Buttons.Start);
        if (Pressed(Keys.Escape) || PadPressed(Buttons.Back) || (taken && _chosenEntry == Quit))
        {
            Exit();
        }
        else if (taken)
        {
            // "Perform" is the poster of the newest night that is open (plan T55): the curtain is raised there.
            // ponytail: a card's chime, which is the chime of a piece of applause picked up.
            ShowThePoster(_night!.Value);
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

        // The entries. "Perform" names the night whose poster it opens.
        string tonight = _nights.First(night => night.Number == _night) is { Name: { } itsName }
            ? $"Night {_night}: {itsName}"
            : $"Night {_night}";
        float top = EntriesTop;
        foreach ((int entry, string name, string note) in new[] { (Perform, "Perform", tonight), (Quit, "Quit", "Esc") })
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
            "Move: WASD, the arrows or the stick  ·  Vanish: Space or A  ·  Go on: Enter or Start  ·  R starts the night again  ·  Esc or Back comes back here",
            new Vector2(stage.X / 2f, ControlsLine),
            0.5f,
            Words);
        _spriteBatch.End();
    }
}
