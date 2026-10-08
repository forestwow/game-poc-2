using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Understudies.Core;

using Vector2 = System.Numerics.Vector2;

namespace Understudies.Game;

// The poster (plan T55, the second half of L4 of "The ladder"): the page between the menu's "Perform" and the
// curtain, and where R goes when a show is over. A page for each night nights.json has: an open night's says what
// is true of the show that follows, read from the night's composed tuning (the acts, the doors, who comes and from
// which act, the kinds that the night before did not have) and from the progress (what opens the next night, the
// player's record); a shut night's says its name and what opens it, and nothing else. It holds no rule: the gate
// is Core's (Progress.GateOf) and the words for it are here. A fourth part of the game's one class, as the menu is.
internal sealed partial class UnderstudiesGame
{
    // The sheet, on the menu's set under the program's wash: cream paper in a card's border and shadow.
    private static readonly Vector2 PosterTopLeft = new(5f, 1.5f);
    private static readonly Vector2 PosterSize = new(38f, 22.3f);
    private const float PosterPad = 1.5f;

    // Down the sheet: the small line, the night's name (56 screen pixels at 1280 wide, four times the 14 that
    // Pixelify's squares are whole at, and 42 or 28 for a longer name), and a line of ink under it.
    private const float PosterSmallLine = 2.75f;
    private const float PosterNameLine = 4.75f;
    private static readonly float[] PosterNameHeights = [2.1f, 1.575f, 1.05f];
    private const float PosterRule = 6.3f;

    // The left column: a label in small capitals and what it says beside it, a line for each.
    private const float PosterRowsTop = 7.6f;
    private const float PosterRowPitch = 1.05f;
    private const float PosterRowsApart = 0.45f;
    private const float PosterValuesFrom = 5.4f;
    private const float PosterColumn = 18.5f;
    private const float PosterWordsHeight = 0.8f;

    // The right column: what is new tonight, each new kind its name in the heading's face, its figure at the
    // menu's understudies' measure (two screen pixels to a sprite pixel at 1280 wide) and a sentence.
    private const float PosterNewFrom = 21f;
    private static readonly float[] PosterNewHeights = [1.575f, 1.05f];
    private const float PosterNewName = 1.6f;
    private const float PosterFigureWide = 5f;
    private const float PosterNewApart = 0.6f;
    private const float PosterFigureDrop = 0.25f;

    // Three new kinds on one page (plan T57): smaller figures, the names at the smaller height and nearer.
    private const float PosterSmallMeasure = 1f;
    private const float PosterSmallName = 1.1f;
    private const float PosterSmallApart = 0.35f;

    // What the game calls a kind and says of it, by the kind's place in enemyKinds, as FigureOf knows the kinds:
    // the one place the game says in words what an enemy does. No number is in a sentence.
    // ponytail: the sentences are true of the kinds as the committed tuning.json has them, and nothing holds
    // them to it: a tuning that changes a kind's turnsOnTheMagician, its speed or its understudyDamageShare makes
    // the poster say what is no longer so. A sentence made from the kind's own numbers, as a card's is in
    // Describe, when the kinds' numbers are tuned again or a night overrides one.
    private static readonly (string Name, string Plural, string Does)[] KindsInWords =
    [
        ("The critic", "Critics", "Walks to the box office and strikes it. Come near, and it turns on you."),
        ("The stagehand", "Stagehands", "Small and quick. It runs for the box office and never turns on you."),
        ("The rival's understudy", "Rival's understudies", "Slow and wide, tougher with every act, and it strikes hard. It never turns on you."),
        ("The headliner", "Headliners", "It hardly notices an understudy's card: this one is yours to fell."),
        ("The scalper", "Scalpers", "It runs for your applause and eats what you leave lying."),
    ];

    private bool _onThePoster;

    // The night and the seed of the show that was last played: what "the same show again" plays (the ladder's "a
    // night can be replayed as it was"). Kept while the game runs, until F5 reads other numbers, and written
    // nowhere. A show played again is a show: it is recorded in the progress as any other.
    private (int Night, ulong Seed)? _played;

    /// <summary>
    /// The poster's pages: every night the file has, the shut ones too. Under <c>--night</c> the one night that
    /// was asked for, since nothing can be said of how another opens there.
    /// </summary>
    private IReadOnlyList<int> Pages => _askedNight is { } asked ? [asked] : [.. _nights.Select(night => night.Number)];

    /// <summary>
    /// The poster of a night, and no show: the one way to it, from the menu's "Perform" and from R when a show is
    /// over. As with the menu, the show that stands behind it is new and is never played.
    /// </summary>
    private void ShowThePoster(int night)
    {
        ChooseTheNight(night);
        StartAgain();
        _onThePoster = true;
        _guardLeft = MenuGuardTime;
    }

    /// <summary>
    /// The curtain goes up on the night that is chosen: the one way a show that is played starts (the poster's
    /// press, and R in a show). With a <paramref name="seed"/> it is that show again.
    /// </summary>
    private void RaiseTheCurtain(ulong? seed = null)
    {
        ulong its = seed ?? (ulong)DateTime.UtcNow.Ticks;
        StartAgain(its);
        _played = (_night!.Value, its);
    }

    /// <summary>The seed of the last show, when it was a show of the night whose page is up.</summary>
    private ulong? TheSameAgain => _played is { } played && played.Night == _night ? played.Seed : null;

    /// <summary>
    /// A frame's presses on the poster. Left and right (the arrows, A and D, the d-pad, the stick) turn the pages;
    /// on an open night's page Enter, Space or the gamepad's A or Start raise the curtain, and T or the gamepad's
    /// X the curtain of the last show of that night again; Esc or Back goes back to the menu. Every one is a press
    /// and never a hold, and in the poster's first moment (<see cref="MenuGuardTime"/>) the pages turn and nothing
    /// else is done: the R that left a show, or the Enter that took "Perform", pressed twice, raises no curtain.
    /// </summary>
    private void ChooseOnThePoster()
    {
        int[] pages = [.. Pages];
        if (StepAcross() is not 0 and var across)
        {
            ChooseTheNight(pages[Math.Clamp(Array.IndexOf(pages, _night!.Value) + across, 0, pages.Length - 1)]);
        }

        if (_guardLeft > 0f)
        {
            return;
        }

        bool open = Unlocked.Contains(_night!.Value);
        if (Pressed(Keys.Escape) || PadPressed(Buttons.Back))
        {
            ShowTheMenu();
        }
        else if (open && (Pressed(Keys.Enter) || Pressed(Keys.Space) || PadPressed(Buttons.A) || PadPressed(Buttons.Start)))
        {
            // ponytail: a card's chime, which is the chime of a piece of applause picked up.
            RaiseTheCurtain();
            _sound.Play(TickEventKind.ApplausePickedUp);
        }
        else if (open && TheSameAgain is { } seed && (Pressed(Keys.T) || PadPressed(Buttons.X)))
        {
            RaiseTheCurtain(seed);
            _sound.Play(TickEventKind.ApplausePickedUp);
        }
    }

    /// <summary>What a gate asks, of a night as the sentence names it ("this night", "night 2").</summary>
    private static string Asks(Gate gate, string night) => gate switch
    {
        Gate.Played => $"Play {night}, won or lost",
        Gate.ActReachedOrWon => $"Reach act {Progress.ActThatUnlocks} of {night}, or win it",
        _ => $"Win {night}",
    };

    /// <summary>Numbers in words: "1", "1 and 3", "1, 3 and 6".</summary>
    private static string Listed(IReadOnlyList<int> numbers) => numbers.Count < 2
        ? string.Join("", numbers)
        : $"{string.Join(", ", numbers.Take(numbers.Count - 1))} and {numbers[^1]}";

    /// <summary>
    /// The kinds a night's acts have, by their places in enemyKinds: those the composed tuning buys (a weight, or
    /// a number for an act) from an act the night has, in the order of the acts they come from (of two from one
    /// act, the one that is earlier in the list: the scalper is the last there and comes from act three).
    /// </summary>
    private static int[] KindsOf(Tuning tuning) =>
        [.. Enumerable.Range(0, tuning.EnemyKinds.Count).Where(kind =>
            tuning.EnemyKinds[kind] is var its && (its.Weight > 0 || its.InAnAct > 0) && its.FromAct <= tuning.ActsInPerformance)
            .OrderBy(kind => tuning.EnemyKinds[kind].FromAct)];

    /// <summary>The poster's frame: the page of the night that is chosen, on the menu's set.</summary>
    private void DrawThePoster()
    {
        FitTheStage(shake: Vector2.Zero);
        GraphicsDevice.Clear(Surround);
        LayTheSet(Vector2.Zero, MenuFloorTop, MenuCurtain, MenuGoldLine);
        Vector2 stage = Tuning.StageSize;
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _worldToScreen);
        Fill(Vector2.Zero, stage, Color.Black * ProgramDim);
        _spriteBatch.End();

        int number = _night!.Value;
        int place = Enumerable.Range(0, _nights.Count).First(at => _nights[at].Number == number);
        bool open = Unlocked.Contains(number);
        string name = _nights[place].Name ?? $"Night {number}";
        float left = PosterTopLeft.X + PosterPad;
        float right = PosterTopLeft.X + PosterSize.X - PosterPad;

        // A shut night's sheet is dark in a broken border, as the cast's square of the act that is not played yet.
        Color ink = open ? OutlineInk : Words;
        Color faint = open ? FaintInk : ClockStands;

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        Paper(PosterTopLeft, PosterSize, open ? ThrownCardFace : CastRecording, broken: !open, border: CardBorder, shadow: CardShadow);

        // The house first, as the ladder's document has the order. There is one stage, and "the street corner"
        // is its name there and on the menu: flavour, until a night has a house of its own.
        Write(
            Face.Sentence,
            MenuSmallHeight,
            $"NIGHT {number}  ·  AT THE STREET CORNER",
            new Vector2(left, PosterSmallLine),
            0f,
            open ? ChipCount : faint,
            onPaper: true,
            spacing: MenuSmallSpacing);

        // The player's record of the night, at the right of that line. A night that was asked for has none.
        if (_askedNight is null && open)
        {
            NightPlayed? played = _progress.Nights.FirstOrDefault(night => night.Night == number);
            Write(
                Face.Sentence,
                MenuSmallHeight,
                played is null ? "NOT PLAYED YET"
                    : played.Won ? "YOU HAVE WON IT"
                    : $"PLAYED  ·  YOUR BEST: ACT {played.BestAct} OF {Tuning.ActsInPerformance}",
                new Vector2(right, PosterSmallLine),
                1f,
                faint,
                onPaper: true,
                spacing: MenuSmallSpacing);
        }

        // The name in the largest of its sizes that the sheet is wide enough for, or the smallest.
        float nameHeight = PosterNameHeights.FirstOrDefault(
            tall => Wide(Face.Heading, tall, name) <= right - left, PosterNameHeights[^1]);
        Write(Face.Heading, nameHeight, name, new Vector2(left, PosterNameLine), 0f, ink, onPaper: true);
        Fill(OnAPixel(new Vector2(left, PosterRule)), new Vector2(Pixels(right - left), Pixels(PaperBorder)), ink);

        float line = PosterRowsTop;

        // A row of the left column: its label, and its lines.
        void Row(string label, IEnumerable<string> lines)
        {
            Write(Face.Sentence, LabelHeight, label, new Vector2(left, line), 0f, faint, onPaper: true, spacing: LabelSpacing);
            foreach (string words in lines)
            {
                Write(Face.Sentence, PosterWordsHeight, words, new Vector2(left + PosterValuesFrom, line), 0f, ink, onPaper: true);
                line += PosterRowPitch;
            }

            line += PosterRowsApart;
        }

        List<string> Broken(string sentence) =>
            Wrapped(Face.Sentence, PosterWordsHeight, sentence.Split(' '), PosterColumn - PosterValuesFrom);

        if (!open)
        {
            // A shut night: its name, and what opens it, which is the gate of the night before it in the file.
            int before = _nights[place - 1].Number;
            Row("NOT OPEN YET", Broken($"{Asks(Progress.GateOf(before), $"night {before}")}, and this poster opens."));
            _spriteBatch.End();
            DrawThePostersKeys(open);
            return;
        }

        Tuning tuning = Tuning;
        int acts = tuning.ActsInPerformance;
        Row("ACTS", [string.Create(CultureInfo.InvariantCulture, $"{acts} acts of {tuning.ActLength:0.#} s")]);

        // The doors that open in one of the night's acts, and in which.
        int[] doors = [.. tuning.StageDoors.Select(door => Math.Max(1, door.OpensInAct)).Where(act => act <= acts).Order()];
        int[] opening = [.. doors.Distinct()];
        Row(
            "DOORS",
            Broken($"{doors.Length} of {tuning.StageDoors.Count} open: from {(opening.Length == 1 ? "act" : "acts")} {Listed(opening)}"));

        int[] kinds = KindsOf(tuning);
        Row(
            "WHO COMES",
            kinds.Select(kind =>
                $"{(kind < KindsInWords.Length ? KindsInWords[kind].Plural : tuning.EnemyKinds[kind].Name)}, from act {Math.Max(1, tuning.EnemyKinds[kind].FromAct)}"));

        // The night's house rules, each its name and its one sentence (plan T56; the document's §4), where the
        // document has them: after who comes and before what opens the next night. A night without one has no row.
        // ponytail: no icon, since no rule has one, and a long night's page has room for one rule under four
        // kinds: lay the column out again when a night has two.
        string[] houseRules = [.. Night.RuleNames.Where(HasRule)];
        if (houseRules.Length > 0)
        {
            Row("HOUSE RULES", houseRules.SelectMany(rule => Broken(RuleInWords(rule))));
        }

        // What opens the next night: Core's gate, in words, or that it is open already.
        string next = _askedNight is not null ? "Started with --night: nothing of this night is remembered."
            : place == _nights.Count - 1 ? "This is the last night there is, for now."
            : Unlocked.Contains(_nights[place + 1].Number) ? $"Night {_nights[place + 1].Number} is open."
            : $"{Asks(Progress.GateOf(number), "this night")}, and night {_nights[place + 1].Number} opens.";
        Row("THE NEXT NIGHT", Broken(next));

        // The new thing of the night: the kinds the night before it in the file did not have. Tonight's alone on
        // the first night. A kind that is new has its picture, which is drawn after the words.
        // ponytail: two new kinds fit under one another as the design has them, and three in the smaller lay-out
        // below (plan T57: night 10 has three until night 3 takes the scalper), each with a sentence of three
        // lines at the most. A night with four is the night to lay this column out again.
        int[] theNightBefore = place == 0 ? [] : KindsOf(Night.Compose(_plain, _nights, _nights[place - 1].Number));
        int[] newKinds = [.. kinds.Except(theNightBefore)];
        bool three = newKinds.Length > 2;
        float measure = three ? PosterSmallMeasure : MenuUnderstudyMeasure;
        float nameTall = three ? PosterSmallName : PosterNewName;
        float apart = three ? PosterSmallApart : PosterNewApart;
        var figures = new List<(int Kind, Vector2 Feet)>();
        float columnLeft = left + PosterNewFrom;
        float top = PosterRowsTop;
        if (newKinds.Length > 0)
        {
            Write(Face.Sentence, LabelHeight, "NEW TONIGHT", new Vector2(columnLeft, top), 0f, ChipCount, onPaper: true, spacing: LabelSpacing);
            top += 0.5f;
        }

        // One size for every new name on the page: the larger when all of them fit in it.
        (string Name, string Plural, string Does) InWords(int kind) =>
            kind < KindsInWords.Length ? KindsInWords[kind] : (tuning.EnemyKinds[kind].Name, "", "");
        float newHeight = three ? PosterNewHeights[^1] : PosterNewHeights.FirstOrDefault(
            tall => newKinds.All(kind => Wide(Face.Heading, tall, InWords(kind).Name) <= right - columnLeft), PosterNewHeights[^1]);
        foreach (int kind in newKinds)
        {
            Write(Face.Heading, newHeight, InWords(kind).Name, new Vector2(columnLeft, top + (nameTall / 2f)), 0f, ink, onPaper: true);

            // The figure's head is level with the sentence's first line, and the next name is under the taller
            // of the two.
            float tall = _sheets[(int)FigureOf(kind)][0].First.Height * PixelAt(measure);
            figures.Add((kind, new Vector2(columnLeft + (PosterFigureWide / 2f), top + nameTall + PosterFigureDrop + tall)));
            float words = top + nameTall + PosterFigureDrop + (PosterRowPitch / 2f);
            string from = $"From act {Math.Max(1, tuning.EnemyKinds[kind].FromAct)}.";
            foreach (string wrapped in Wrapped(
                Face.Sentence, PosterWordsHeight, $"{InWords(kind).Does} {from}".Trim().Split(' '), right - columnLeft - PosterFigureWide))
            {
                Write(Face.Sentence, PosterWordsHeight, wrapped, new Vector2(columnLeft + PosterFigureWide, words), 0f, ink, onPaper: true);
                words += PosterRowPitch;
            }

            top = MathF.Max(top + nameTall + PosterFigureDrop + tall, words - (PosterRowPitch / 2f)) + apart;
        }

        _spriteBatch.End();

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _worldToScreen);
        foreach ((int kind, Vector2 feet) in figures)
        {
            DrawFigure(FigureOf(kind), feet, tint: WashOf(kind), measure: measure);
        }

        _spriteBatch.End();
        DrawThePostersKeys(open);
    }

    /// <summary>
    /// The poster's keys, as key caps under the sheet: those that do something on this page.
    /// ponytail: the keyboard's alone. A gamepad's (the stick, A or Start, X, Back) are in the README: a line of
    /// both is wider than the stage.
    /// </summary>
    private void DrawThePostersKeys(bool open)
    {
        List<(string Words, bool IsAKey)> keys = [];
        if (Pages.Count > 1)
        {
            keys.AddRange([(LeftKey, true), (RightKey, true), ("another night  ·", false)]);
        }

        if (open)
        {
            keys.AddRange([("Enter", true), ("or", false), ("Space", true), ("raises the curtain  ·", false)]);
            if (TheSameAgain is not null)
            {
                // The last show of this night, as it was: who came and when, and what its encores offered.
                keys.AddRange([("T", true), ("the same show again  ·", false)]);
            }
        }

        keys.AddRange([("Esc", true), ("the menu", false)]);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        DrawTheKeys(keys);
        _spriteBatch.End();
    }
}
