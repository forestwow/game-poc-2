using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Understudies.Core;

using Vector2 = System.Numerics.Vector2;

namespace Understudies.Game;

// The offer's screen (plan T18, laid out by T48): the offer as paper cards on the dimmed stage, the choosing, and
// the card just taken shown for a moment. An encore's cards and the program's one are offered on the same cards
// (plan T23). When an act is over, in the program and between two acts, four panels stand beside the card's
// place (plan T50): the act on the books, the understudy that joins, what the next act brings and the cast at
// the curtain. It is a part of the game's one class because it draws with that class's batch and words.
internal sealed partial class UnderstudiesGame
{
    // The offer's screen, from the curtain down (plan T48, S5 of "The screens"): the two lines under the curtain,
    // the row of what is held (the HUD's chips), the cards, and under them the countdown, its bar and the keys.
    // Each size is the design's screen pixels in a window 1280 wide, kept in world units. A card is paper in an
    // ink border with a hard shadow: a head strip in the colour of whose it is, its name, its flavour, what it
    // does, and at its foot how many of it are held. Three are a row with room at the sides, and one is as tall
    // as the floor leaves between the held row and the countdown.
    private static readonly Vector2 CardSize = new(12.6f, 13.9f);
    private const float CardGap = 1.2f;
    private const float CardsTop = 8.6f;
    private const float CardBorder = 0.15f;
    private const float CardShadow = 0.225f;

    // The chosen card stands this much higher than the others, in a ring this thick, on lighter paper.
    private const float CardRaise = 0.6f;
    private const float CardRing = 0.15f;
    private static readonly Color ChosenPaper = new(255, 253, 244);

    // The head strip, under the card's top border, with an ink line under it: the card's key and whose it is.
    private const float HeadStrip = 1.35f;
    private const float HeadWordsHeight = 0.675f;

    // A card's name is Pixelify Sans, which is crisp at 28 screen pixels and not at 32: 1.05 units in a window
    // 1280 wide.
    private const float NameHeight = 1.05f;
    private const float SmallWordsHeight = 0.8f;

    // Where the lines of a card are, from its top. The flavour is the smaller face, under the name; what the card
    // does is the larger, under that, and starts at one height on every card: under two lines of flavour. A line
    // of either is no wider than the card less CardMargin at each side. The foot's line is the flavour's size.
    // The picture (plan S5a, T47) is a square of 124 screen pixels in a window 1280 wide (4.65 units: a picture
    // of 62 sprite pixels at two screen pixels each) under the head strip, in the middle of the card's width, its
    // top 1.95 from the card's, with the name, the flavour and what the card does under it: three lines of what
    // the card does end just over the foot's line.
    private const float PictureTop = 1.95f;
    private const float PictureSide = 4.65f;
    private const float NameLine = 7.45f;
    private const float FlavourDrop = 1f;
    private const float SentenceDrop = 2.7f;
    private const float FootLift = 0.6f;
    private const float FlavourHeight = 0.6f;
    private const float FlavourPitch = 0.7f;
    private const float SentencePitch = 0.9f;
    private const float CardMargin = 0.6f;

    // Under the cards: the countdown's line, its bar in an ink rim on a track lighter than the dimmed boards, so
    // that how much of the time is gone is read from the bar, and the keys, each key a cap: its name in a
    // thin cream border.
    private const float CountdownLine = 23.3f;
    private const float CountdownBarTop = 23.95f;
    private static readonly Vector2 CountdownBar = new(18f, 0.375f);
    private static readonly Color CountdownTrack = new(96, 84, 104);
    private const float KeysLine = 24.95f;
    private const float KeyHeight = 0.825f;
    private const float KeyPad = 0.225f;
    private const float KeyWordsHeight = 0.6f;
    private const float KeysApart = 0.3f;

    // An arrow key's cap has its arrow drawn, and no letter: the face is not asked for one.
    private const string LeftKey = "<";
    private const string RightKey = ">";

    // How dark the stage goes under the cards.
    private const float ProgramDim = 0.6f;

    // Seconds a card just taken stays on the screen before the line between two acts. No go-on is taken in them.
    private const float TakenTime = 0.8f;

    // How far a stick is pushed to the side before it moves the highlight.
    private const float StickLean = 0.5f;

    // Seconds from an encore's opening in which no card is taken: an encore opens in the middle of a fight, and
    // the Space pressed for a Vanish a moment too late must not take a card nobody has read.
    private const float EncoreGuardTime = 0.4f;

    // The flavour and the foot's line are in a fainter ink than the rest of a card.
    private static readonly Color FaintInk = new(90, 77, 94);

    // The four panels of an act that is over (plan T50, S6 of "The screens"), two at each side of the card's
    // place, as far from the card as PanelFromTheCard: a side door is clear of them, so the door the next act
    // opens is seen. Dark, in a cream border; the upper two are PanelUpperTall and the lower two reach down to
    // PanelsFoot, over the keys' line. The sizes are the design's screen pixels at 1280, in world units.
    private const float PanelWide = 13.5f;
    private const float PanelFromTheCard = 1.2f;
    private const float PanelsTop = 8f;
    private const float PanelUpperTall = 6.9f;
    private const float PanelsApart = 0.4f;
    private const float PanelsFoot = 23.5f;
    private const float PanelBorder = 0.1125f;
    private const float PanelPad = 0.6f;
    private static readonly Color PanelGround = new(24, 18, 28);

    // A panel's heading is a label, a little larger than the HUD's; under it rows of a name and what it counts,
    // or sentences, or a small note in a fainter cream.
    private const float PanelHeadingLine = 0.9f;
    private const float PanelHeadingHeight = 0.6f;
    private const float PanelFirstLine = 1.95f;
    private const float PanelRowPitch = 0.87f;
    private const float PanelWordsHeight = 0.65f;
    private const float PanelNoteHeight = 0.5625f;
    private const float PanelNotePitch = 0.72f;
    private static readonly Color PanelNote = new(200, 191, 204);
    private static readonly Color ApplauseCount = new(255, 138, 216);

    // The offer that is up, an encore's or the program's, or the one last taken from: the simulation empties its
    // own with the pick.
    private IReadOnlyList<Card> _offered = [];
    private int _highlighted;
    private int _taken;
    private float _takenLeft;
    private float _guardLeft;

    // The act's books (plan T50), which are the view's to keep: what the box office had when the act began, and
    // how many critics fell to whose cards in it, as the Kill events said: the magician's first, and then each
    // understudy's by its place. Both are set back when the next act begins.
    private float _boxOfficeAtTheActsStart;
    private readonly List<int> _actKills = [];

    /// <summary>Cards are on offer: an encore is read, or the program.</summary>
    private bool IsOffered => _simulation.Phase is Phase.Encore or Phase.Program;

    /// <summary>The cards are on the screen: they are on offer, or the program's card was taken a moment ago.</summary>
    private bool CardsAreShown => IsOffered || (_simulation.Phase == Phase.BetweenActs && _takenLeft > 0f);

    /// <summary>
    /// The stage stands under its wash for a screen: an encore's cards, or the panels of an act that is over,
    /// which are up in the program and between two acts, until the next act is gone on to.
    /// </summary>
    private bool StageStands => IsOffered || _simulation.Phase == Phase.BetweenActs;

    /// <summary>A count of pieces of applause, as words: one is a piece.</summary>
    private static string Pieces(int count) => count == 1 ? "1 piece" : $"{count} pieces";

    /// <summary>How many critics fell in this act to the cards of a thrower, as a <see cref="TickEvent"/> names one.</summary>
    private int KillsOf(int thrower) =>
        thrower - TickEvent.TheMagician < _actKills.Count ? _actKills[thrower - TickEvent.TheMagician] : 0;

    /// <summary>
    /// A frame's presses in the program, and in an encore. Left and right (the arrows, A and D, a gamepad's d-pad or its left stick)
    /// move the highlight; Enter, Space or the gamepad's A take the highlighted card; 1, 2 and 3 take the card in
    /// that place at once. Every one of them is a press and never a hold: a key or a stick held since the act,
    /// Space for a Vanish or D to walk, was down the frame before and does nothing until it has been let go. In
    /// the first moment of an encore (<see cref="EncoreGuardTime"/>) the highlight moves and nothing is taken.
    /// </summary>
    private void ChooseInTheProgram(KeyboardState keys, GamePadState pad)
    {
        bool Pressed(Keys key) => keys.IsKeyDown(key) && !_keysBefore.IsKeyDown(key);
        bool PadPressed(Buttons button) => pad.IsButtonDown(button) && _padBefore.IsButtonUp(button);

        // A stick counts when it comes to a side, as a key does when it goes down.
        static int Lean(GamePadState pad) =>
            pad.ThumbSticks.Left.X > StickLean ? 1 : pad.ThumbSticks.Left.X < -StickLean ? -1 : 0;

        int lean = Lean(pad);
        int step = (lean != Lean(_padBefore) ? lean : 0)
            + (Pressed(Keys.Right) || Pressed(Keys.D) || PadPressed(Buttons.DPadRight) ? 1 : 0)
            - (Pressed(Keys.Left) || Pressed(Keys.A) || PadPressed(Buttons.DPadLeft) ? 1 : 0);
        _highlighted = Math.Clamp(_highlighted + step, 0, _offered.Count - 1);
        if (_guardLeft > 0f)
        {
            return;
        }

        for (int place = 0; place < _offered.Count; place++)
        {
            if (Pressed(Keys.D1 + place) || Pressed(Keys.NumPad1 + place))
            {
                Take(place);
                return;
            }
        }

        if (Pressed(Keys.Enter) || Pressed(Keys.Space) || PadPressed(Buttons.A))
        {
            Take(_highlighted);
        }
    }

    private void Take(int place)
    {
        _simulation.Pick(place);
        Acknowledge(place);
    }

    /// <summary>
    /// The card at <paramref name="place"/> was taken, with a chime: the program's stays lit for a moment, and an
    /// encore's is gone at once, for the act goes on.
    /// </summary>
    private void Acknowledge(int place)
    {
        if (_simulation.Phase == Phase.BetweenActs)
        {
            _taken = place;
            _takenLeft = TakenTime;
        }

        // ponytail: the chime of a piece of applause picked up. A sound of its own when the sounds are done over.
        _sound.Play(TickEventKind.ApplausePickedUp);
    }

    /// <summary>Where the card at <paramref name="place"/> has its top left corner, raised when it is chosen.</summary>
    private Vector2 CardTopLeft(int place)
    {
        float row = (_offered.Count * CardSize.X) + ((_offered.Count - 1) * CardGap);
        return new Vector2(
            ((Tuning.StageSize.X - row) / 2f) + (place * (CardSize.X + CardGap)),
            CardsTop - (IsLit(place) ? CardRaise : 0f));
    }

    /// <summary>The highlighted card while cards are offered, and afterwards the one that was taken.</summary>
    private bool IsLit(int place) => place == (IsOffered ? _highlighted : _taken);

    /// <summary>A whole screen pixel for a point of the stage, and whole screen pixels for a length in units.</summary>
    private Vector2 OnAPixel(Vector2 point) => Vector2.Round(_corner + (point * _scale));

    private float Pixels(float units) => MathF.Max(1f, MathF.Round(units * _scale));

    /// <summary>
    /// The offer's screen, in the batch of the words (plan T48, T50): in an encore how many encores were taken,
    /// under the line that announces, and when an act is over its four panels; and while the cards are shown the
    /// cards, with the countdown and the keys under them while they are offered. What the magician holds is the
    /// HUD's, in a row over the cards (<see cref="DrawTheHeld"/>).
    /// </summary>
    private void DrawTheOffer()
    {
        float middle = Tuning.StageSize.X / 2f;
        if (_simulation.Phase == Phase.Encore)
        {
            // While an encore is read its cost is still the one it was earned at: "the next" would be this very one.
            int encores = _simulation.EncoresTaken;
            Write(
                Face.Sentence,
                SmallWordsHeight,
                $"{(encores == 1 ? "1 encore" : $"{encores} encores")} so far. This one was earned with {Pieces(_simulation.EncoreCost)} of applause.",
                new Vector2(middle, Tuning.StageFloorTop + EncoresLineDrop),
                0.5f,
                ApplauseHeart);
        }
        else
        {
            DrawThePanels();
        }

        if (!CardsAreShown)
        {
            // Between two acts, with no card on the screen: the one key there is.
            DrawTheKeys([("Enter", true), ("or Start raises the curtain", false)]);
            return;
        }

        // An offer of the chorus card is the chorus's colour all through, and one of self cards applause's pink.
        Color accent = _offered[0] == Card.ChorusDamage ? UnderstudyTints[0] : ApplauseGlow;
        for (int place = 0; place < _offered.Count; place++)
        {
            DrawACard(place, accent);
        }

        if (!IsOffered)
        {
            return;
        }

        // A second that has begun still shows, as on the act's clock. Nobody is to be surprised by what the end
        // of the time does.
        int seconds = (_simulation.OfferTicksLeft + Simulation.TicksPerSecond - 1) / Simulation.TicksPerSecond;
        bool choice = _offered.Count > 1;
        Write(
            Face.Sentence,
            SmallWordsHeight,
            $"{seconds} s left: then {(choice ? "the leftmost card" : "this card")} is taken for you",
            new Vector2(middle, CountdownLine),
            0.5f,
            ApplauseHeart);

        // The bar runs down from full, whatever the offer's time is.
        float time = _simulation.Phase == Phase.Encore ? Tuning.EncoreTime : Tuning.ProgramTime;
        float left = Math.Clamp(_simulation.OfferTicksLeft / MathF.Max(1f, time * Simulation.TicksPerSecond), 0f, 1f);
        Vector2 bar = OnAPixel(new Vector2(middle - (CountdownBar.X / 2f), CountdownBarTop));
        var barSize = new Vector2(Pixels(CountdownBar.X), Pixels(CountdownBar.Y));
        float rim = Pixels(PaperBorder);
        Fill(bar - new Vector2(rim), barSize + new Vector2(2f * rim), OutlineInk);
        Fill(bar, barSize, CountdownTrack);
        Fill(bar, barSize with { X = MathF.Round(barSize.X * left) }, accent);

        // The keys of the cards there are: one card has no choosing, and two have no 3.
        List<(string Words, bool IsAKey)> keys = choice
            ? [(LeftKey, true), (RightKey, true), ("choose  ·", false), ("Enter", true), ("or", false), ("Space", true), ("takes  ·  or press", false)]
            : [("Enter", true), ("or", false), ("Space", true), ("takes it  ·  or press", false)];
        for (int place = 0; place < _offered.Count; place++)
        {
            keys.Add(($"{place + 1}", true));
        }

        keys.Add((choice ? "·  gamepad: the stick and A" : "·  gamepad: A", false));
        DrawTheKeys(keys);
    }

    /// <summary>
    /// The four panels of the act that is over (plan T50), in the batch of the words. Every number is the
    /// simulation's as it stands, but two that the view keeps of what the simulation told it: what the box office
    /// had when the act began, and the Kill events counted by whose card it was. Nothing is worked out that a
    /// rule works out: what the understudy holds is what the magician holds now, since the act's last tick is the
    /// one its recording ends with (or the tick of the fall, after which no card is taken).
    /// </summary>
    private void DrawThePanels()
    {
        int act = _simulation.Act;
        float cardLeft = (Tuning.StageSize.X - CardSize.X) / 2f;
        float leftColumn = cardLeft - PanelFromTheCard - PanelWide;
        float rightColumn = cardLeft + CardSize.X + PanelFromTheCard;
        float lowerTop = PanelsTop + PanelUpperTall + PanelsApart;
        float room = PanelWide - (2f * PanelPad);
        Color joins = TintOfAct(act);

        // A panel and its heading; the answer is its top left corner inside the pad.
        Vector2 Panel(float left, float top, float tall, string heading, Color colour)
        {
            Vector2 at = OnAPixel(new Vector2(left, top));
            Vector2 size = Vector2.Round(new Vector2(PanelWide, tall) * _scale);
            float border = Pixels(PanelBorder);
            Fill(at, size, Words);
            Fill(at + new Vector2(border), size - new Vector2(2f * border), PanelGround);
            Write(
                Face.Sentence,
                PanelHeadingHeight * MathF.Min(1f, room / Wide(Face.Sentence, PanelHeadingHeight, heading, LabelSpacing)),
                heading,
                new Vector2(left + PanelPad, top + PanelHeadingLine),
                0f,
                colour,
                onPaper: true,
                spacing: LabelSpacing);
            return new Vector2(left + PanelPad, top);
        }

        // Sentences under one another from a line of a panel, each begun on a line of its own and broken to a
        // width; the answer is the line after.
        float Sentences(Vector2 at, float width, float height, float pitch, Color colour, params string[] sentences)
        {
            foreach (string line in sentences.SelectMany(sentence => Wrapped(Face.Sentence, height, sentence.Split(' '), width)))
            {
                Write(Face.Sentence, height, line, at, 0f, colour, onPaper: true);
                at.Y += pitch;
            }

            return at.Y;
        }

        // The act on the books: a name at the left and what it counts at the right. These are the notices of the
        // road's T30, as far as the game has a number for them.
        Vector2 books = Panel(leftColumn, PanelsTop, PanelUpperTall, $"ACT {act} ON THE BOOKS", Magician);
        float line = PanelsTop + PanelFirstLine;
        void Row(string name, string counts, Color colour)
        {
            Write(Face.Sentence, PanelWordsHeight, name, new Vector2(books.X, line), 0f, Words, onPaper: true);
            Write(Face.Sentence, PanelWordsHeight, counts, new Vector2(books.X + room, line), 1f, colour, onPaper: true);
            line += PanelRowPitch;
        }

        // The box office as its number is written beside its bar: a part of a hit point lost is a whole one.
        float left = MathF.Ceiling(_simulation.BoxOfficeHitPoints);
        float lost = MathF.Ceiling(_boxOfficeAtTheActsStart) - left;
        Row("Applause picked up", $"{_simulation.ActApplause}", ApplauseCount);
        Row("Encores taken", $"{_simulation.ActEncores}, and {_simulation.EncoresTaken} tonight", Words);
        Row("Critics your own cards felled", $"{KillsOf(TickEvent.TheMagician)}", Magician);
        Row("Box office left", $"{left} / {Tuning.BoxOfficeHitPoints}", HitPoints);
        Row("Box office lost in this act", lost > 0f ? $"{lost}" : "nothing", lost > 0f ? HeadlinerWash : Words);
        Row("The magician", _simulation.MagicianHasFallen ? "fell" : "stood to the end", _simulation.MagicianHasFallen ? HeadlinerWash : Words);

        // The understudy that joins: the magician's figure in the act's tint, as every understudy is drawn, with
        // what it does from now on beside it and the cards it holds when its act ends.
        float lowerTall = PanelsFoot - lowerTop;
        Vector2 joining = Panel(leftColumn, lowerTop, lowerTall, $"UNDERSTUDY {act} JOINS THE CAST", joins);
        float figureWide = _sheets[(int)Figure.Magician][0].First.Width * FigurePixel;
        var feet = new Vector2(joining.X + (figureWide / 2f), lowerTop + lowerTall - PanelPad);

        // A figure is drawn in world units, and this batch is in screen pixels: it is the stage's for the figure.
        _spriteBatch.End();
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _worldToScreen);
        DrawFigure(Figure.Magician, feet, opacity: UnderstudyOpacity, cardboard: joins);
        _spriteBatch.End();
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        var beside = new Vector2(joining.X + figureWide + PanelPad, lowerTop + PanelFirstLine);
        float besideRoom = room - figureWide - PanelPad;
        List<string> cards = [.. Enum.GetValues<Card>()
            .Where(card => card != Card.ChorusDamage && Held(card) > 0)
            .Select(card => $"{Describe(card).Name} ×{Held(card)}")];
        beside.Y = Sentences(
            beside,
            besideRoom,
            PanelWordsHeight,
            PanelRowPitch,
            Words,
            _simulation.MagicianHasFallen
                ? "It runs this route to where you fell and leaves the stage there, in every act to come."
                : "It runs this route in every act to come.",
            cards.Count > 0 ? "At its end it holds what you hold:" : "It holds no card.");

        // A card's name and its count are never parted: a line is as many of them as fit.
        foreach (string held in Wrapped(
            Face.Sentence, PanelNoteHeight, cards.Select((card, i) => i == cards.Count - 1 ? card : $"{card},"), besideRoom))
        {
            Write(Face.Sentence, PanelNoteHeight, held, beside, 0f, joins, onPaper: true);
            beside.Y += PanelNotePitch;
        }

        // What the next act brings, read from its plan, the doors and the encore's cost: who enters, by the
        // kinds' own names in the tuning, which door opens and what the first encore costs.
        int next = act + 1;
        Vector2 brings = Panel(rightColumn, PanelsTop, PanelUpperTall, $"NEXT: ACT {next} OF {Tuning.ActsInPerformance}", Magician);
        // An act that a reload of the tuning added has no plan (the guard of Simulation.ActEntries).
        // ponytail: a kind's plural is its name and an s. A plural of its own in the tuning, for a kind whose
        // name does not take one.
        IReadOnlyList<PlannedEntry> entries = act < _simulation.Plan.Count ? _simulation.Plan[act] : [];
        IEnumerable<string> kinds = entries
            .GroupBy(entry => Math.Min(entry.Kind, Tuning.EnemyKinds.Count - 1))
            .OrderBy(kind => kind.Key)
            .Select(kind => $"{kind.Count()} {Tuning.EnemyKinds[kind.Key].Name}{(kind.Count() == 1 ? string.Empty : "s")}");

        // ponytail: a door is named by where it stands, in the top edge or in a half of the stage. Names of
        // their own in the tuning, when a fourth door or one elsewhere is added.
        // ponytail: which doors the next act has open is Simulation.DoorIsOpen's rule said again for another
        // act: a door that shuts would make this panel wrong. The simulation asked for an act, when one does.
        List<string> opening = [.. Tuning.StageDoors
            .Where(door => door.OpensInAct == next)
            .Select(door => door.Position.Y <= Tuning.StageFloorTop ? "back" : door.Position.X < Tuning.StageSize.X / 2f ? "left" : "right")];
        int doors = Tuning.StageDoors.Count;
        int open = Tuning.StageDoors.Count(door => door.OpensInAct <= next);
        Sentences(
            new Vector2(brings.X, PanelsTop + PanelFirstLine),
            room,
            PanelWordsHeight,
            PanelRowPitch,
            Words,
            entries.Count == 0 ? "Nobody enters." : $"{entries.Count} enter: {string.Join(", ", kinds)}.",
            $"{(opening.Count == 0 ? "No new door opens" : opening.Count == 1 ? $"The {opening[0]} door opens" : $"The {string.Join(" and the ", opening)} doors open")}: {open} of {doors} open.",
            $"Next encore: {Pieces(_simulation.EncoreCost)} of applause in one act.");

        // The cast at the curtain: the HUD's squares, the one that joins among them, and under each what its
        // cards felled in the act just over, which is what the notices name every understudy by.
        Vector2 curtain = Panel(rightColumn, lowerTop, lowerTall, "THE CAST AT THE CURTAIN", Magician);
        float squares = ((_simulation.Understudies.Count + 1) * (CastSquare + CastApart)) - CastApart;
        float squaresTop = lowerTop + PanelFirstLine - 0.35f;
        DrawTheCast(new Vector2(curtain.X + squares, squaresTop + CastSquare), atTheCurtain: true);
        Sentences(
            new Vector2(curtain.X, squaresTop + CastSquare + CastKillsDrop + 1.1f),
            room,
            PanelNoteHeight,
            PanelNotePitch,
            PanelNote,
            "Under each, the critics its cards felled in this act.",
            "Every understudy is back on its mark when the curtain rises.");
    }

    /// <summary>
    /// The card at <paramref name="place"/>: its paper, laid on whole screen pixels, and its words. The chosen one
    /// is ringed in the offer's <paramref name="accent"/>.
    /// </summary>
    private void DrawACard(int place, Color accent)
    {
        Card card = _offered[place];
        bool chorus = card == Card.ChorusDamage;
        bool lit = IsLit(place);
        (string name, string sentence, string flavour) = Describe(card);
        Vector2 topLeft = CardTopLeft(place);
        Vector2 centre = topLeft + new Vector2(CardSize.X / 2f, 0f);

        Paper(topLeft, CardSize, lit ? ChosenPaper : ThrownCardFace, border: CardBorder, shadow: CardShadow);

        // The ring is about the card and over its shadow, whole on all four sides.
        // ponytail: the design's chosen card has a glow about its ring, which is a blur and is not drawn, as the
        // applause bar's is not. A picture of a glow, if the ring alone is not seen.
        if (lit)
        {
            RingAbout(topLeft, CardSize, accent);
        }

        // The head strip, which flashes on a card just taken: white at first, and back to its colour. A self
        // card's is the magician's gold and the chorus card's the first understudy's green.
        float border = Pixels(CardBorder);
        Vector2 strip = OnAPixel(topLeft) + new Vector2(border);
        var stripSize = new Vector2(MathF.Round(CardSize.X * _scale) - (2f * border), Pixels(HeadStrip));
        Color whose = chorus ? UnderstudyTints[0] : Magician;
        Fill(strip, stripSize, lit ? Color.Lerp(whose, Color.White, _takenLeft / TakenTime) : whose);
        Fill(strip + new Vector2(0f, stripSize.Y), stripSize with { Y = border }, OutlineInk);

        // On the strip the card's key at its left end and whose the card is, in the sentences' face: the key is a
        // number, and Pixelify Sans is kept for the name (plan T45).
        float head = CardBorder + (HeadStrip / 2f);
        Write(Face.Sentence, HeadWordsHeight, $"{place + 1}", topLeft + new Vector2(0.6f, head), 0f, OutlineInk, onPaper: true);
        Write(
            Face.Sentence,
            HeadWordsHeight,
            chorus ? "For every understudy" : "For the magician",
            centre + new Vector2(0f, head),
            0.5f,
            OutlineInk,
            onPaper: true);

        // The card's picture, unsmoothed and from a whole screen pixel, in the middle of the card's width. A
        // sprite pixel is a whole number of screen pixels where the window makes it one, as a figure's is.
        // ponytail: elsewhere the pixels are uneven (1.6 screen pixels at 1024 wide), as DrawFigure's are. A
        // picture drawn at the whole number below, in a smaller place, if that is seen.
        Texture2D picture = _cardPictures[(int)card];
        float asked = PictureSide * _scale / picture.Width;
        float whole = MathF.Max(1f, MathF.Round(asked));
        float pixel = MathF.Abs(asked - whole) <= WholeWithin ? whole : asked;
        Vector2 pictureAt = OnAPixel(centre + new Vector2(0f, PictureTop));
        _spriteBatch.Draw(
            picture,
            new Vector2(pictureAt.X - MathF.Round(picture.Width * pixel / 2f), pictureAt.Y),
            null,
            Color.White,
            0f,
            Microsoft.Xna.Framework.Vector2.Zero,
            pixel,
            SpriteEffects.None,
            0f);

        // The name, no wider than the card; under it the flavour in the smaller face, and under that what the
        // card does, which is what is read first: the larger face and the darker ink.
        float room = CardSize.X - (2f * CardMargin);
        Write(
            Face.Heading,
            NameHeight * MathF.Min(1f, room / Wide(Face.Heading, NameHeight, name)),
            name,
            centre + new Vector2(0f, NameLine),
            0.5f,
            OutlineInk,
            onPaper: true);
        List<string> flavours = Wrapped(Face.Sentence, FlavourHeight, flavour.Split(' '), room);
        for (int line = 0; line < flavours.Count; line++)
        {
            Write(
                Face.Sentence,
                FlavourHeight,
                flavours[line],
                centre + new Vector2(0f, NameLine + FlavourDrop + (line * FlavourPitch)),
                0.5f,
                FaintInk,
                onPaper: true);
        }

        List<string> sentences = Wrapped(Face.Sentence, SmallWordsHeight, sentence.Split(' '), room);
        for (int line = 0; line < sentences.Count; line++)
        {
            Write(
                Face.Sentence,
                SmallWordsHeight,
                sentences[line],
                centre + new Vector2(0f, NameLine + SentenceDrop + (line * SentencePitch)),
                0.5f,
                OutlineInk,
                onPaper: true);
        }

        // At the foot, how many of this card are held, against the limit where it has one (plan T41): the row
        // over the cards says it of every card, and this of the one that is read.
        int held = Held(card);
        string has = chorus ? "the chorus has" : "you have";
        Write(
            Face.Sentence,
            FlavourHeight,
            held == 0 ? $"{has} none yet" : LimitOf(card) == 0 ? $"{has} {held}" : $"{has} {held} of {LimitOf(card)}",
            centre + new Vector2(0f, CardSize.Y - FootLift),
            0.5f,
            FaintInk,
            onPaper: true);
    }

    /// <summary>The ring of what is chosen, a card or the menu's entry: about its paper, in the batch of the words.</summary>
    private void RingAbout(Vector2 topLeft, Vector2 paper, Color accent)
    {
        float ring = Pixels(CardRing);
        Vector2 at = OnAPixel(topLeft) - new Vector2(ring);
        Vector2 size = Vector2.Round(paper * _scale) + new Vector2(2f * ring);
        Fill(at, size with { Y = ring }, accent);
        Fill(at + new Vector2(0f, size.Y - ring), size with { Y = ring }, accent);
        Fill(at, size with { X = ring }, accent);
        Fill(at + new Vector2(size.X - ring, 0f), size with { X = ring }, accent);
    }

    /// <summary>
    /// The line of the keys, centred under the countdown's bar: words, and each key as a cap, a thin cream border
    /// about its name, or about an arrow drawn of squares.
    /// </summary>
    private void DrawTheKeys(List<(string Words, bool IsAKey)> keys)
    {
        float Width((string Words, bool IsAKey) piece) => !piece.IsAKey
            ? Wide(Face.Sentence, KeyWordsHeight, piece.Words)
            : piece.Words is LeftKey or RightKey
                ? KeyHeight * 1.3f
                : MathF.Max(KeyHeight, Wide(Face.Sentence, KeyWordsHeight, piece.Words) + (2f * KeyPad));

        float left = (Tuning.StageSize.X - keys.Sum(Width) - ((keys.Count - 1) * KeysApart)) / 2f;
        foreach ((string Words, bool IsAKey) piece in keys)
        {
            float wide = Width(piece);
            var centre = new Vector2(left + (wide / 2f), KeysLine);
            if (piece.IsAKey)
            {
                Vector2 at = OnAPixel(new Vector2(left, KeysLine - (KeyHeight / 2f)));
                var size = new Vector2(Pixels(wide), Pixels(KeyHeight));
                float border = Pixels(PaperBorder);
                Fill(at, size, Words);
                Fill(at + new Vector2(border), size - new Vector2(2f * border), OutlineInk);

                if (piece.Words is LeftKey or RightKey)
                {
                    // An arrow of squares as large as the border is thick: a head of four columns, each two
                    // squares taller than the one before, and a shaft as long.
                    float way = piece.Words == LeftKey ? 1f : -1f;
                    Vector2 mid = at + Vector2.Round(size / 2f);
                    for (int column = 0; column < 4; column++)
                    {
                        float tall = ((2 * column) + 1) * border;
                        float x = mid.X + (way * (column - 4) * border) - (way < 0f ? border : 0f);
                        Fill(new Vector2(x, mid.Y - MathF.Floor(tall / 2f)), new Vector2(border, tall), Words);
                    }

                    Fill(new Vector2(way > 0f ? mid.X : mid.X - (4f * border), mid.Y - MathF.Floor(border / 2f)), new Vector2(4f * border, border), Words);
                }
            }

            if (piece.Words is not (LeftKey or RightKey))
            {
                Write(Face.Sentence, KeyWordsHeight, piece.Words, centre, 0.5f, Words, onPaper: piece.IsAKey);
            }

            left += wide + KeysApart;
        }
    }

    /// <summary>
    /// The <paramref name="pieces"/> as lines of words a space apart, each line as many of them as are no wider
    /// than <paramref name="width"/> in that face at that height: a piece is never broken, and one wider than a line has a
    /// line to itself.
    /// </summary>
    private List<string> Wrapped(Face face, float height, IEnumerable<string> pieces, float width)
    {
        List<string> lines = [];
        foreach (string piece in pieces)
        {
            if (lines.Count > 0 && Wide(face, height, $"{lines[^1]} {piece}") <= width)
            {
                lines[^1] = $"{lines[^1]} {piece}";
            }
            else
            {
                lines.Add(piece);
            }
        }

        return lines;
    }

    /// <summary>How many of a card were taken in this performance: the magician's own, or the chorus's.</summary>
    private int Held(Card card) => card switch
    {
        Card.Damage => _simulation.MagicianCards.Damage,
        Card.AttackSpeed => _simulation.MagicianCards.AttackSpeed,
        Card.Range => _simulation.MagicianCards.Range,
        Card.VanishCooldown => _simulation.MagicianCards.VanishCooldown,
        Card.OneMoreCard => _simulation.MagicianCards.OneMoreCard,
        Card.Pierce => _simulation.MagicianCards.Pierce,
        Card.Ricochet => _simulation.MagicianCards.Ricochet,
        Card.Burst => _simulation.MagicianCards.Burst,
        _ => _simulation.ChorusCards,
    };

    /// <summary>
    /// How many of a card may be held (plan T41), and 0 for no limit: the chorus card has none, and a tuning
    /// whose limit is nothing has none for any.
    /// </summary>
    private int LimitOf(Card card) => card == Card.ChorusDamage ? 0 : Math.Max(0, Tuning.CardMaxCopies);

    /// <summary>
    /// A card as the player reads it (plan decision 29; the texts are the card catalogue's): its stage name, the
    /// sentence that says what one more of it does, in the numbers of now, and its line of flavour, which says
    /// nothing of the rule.
    /// </summary>
    private (string Name, string Sentence, string Flavour) Describe(Card card)
    {
        // A point and never a comma, whatever the machine's language. A share is a per cent with a decimal where
        // it has one: an eighth is 12.5% and not 12%.
        static string Say(FormattableString words) => words.ToString(CultureInfo.InvariantCulture);

        // A card's share is of the rate and not of the wait: a quarter makes the Vanish come back a quarter
        // faster, which is a fifth sooner. And two of the cards do not add the same again: the pierce's loss is
        // the tuning's over the cards held and the burst's share the tuning's times them, so each says the number
        // it would be with this one taken, and the sentence is true of the card that is offered.
        int withThis = Held(card) + 1;

        // "Each copy again" is not said of the last copy the limit lets the magician hold (plan T41).
        string again = withThis == LimitOf(card) ? string.Empty : " Each copy again.";
        return card switch
        {
            Card.Damage => (
                "Card Sharp",
                Say($"Thrown cards hurt for {Tuning.CardDamage:0.##} more.{again}"),
                "Honed on the edge of a bad review."),
            Card.AttackSpeed => (
                "Sleight of Hand",
                Say($"Throws come {Tuning.CardAttackSpeed * 100f:0.#}% more often.{again}"),
                "The hand is quicker than the critic."),
            Card.Range => (
                "Long Arm",
                Say($"Cards reach {Tuning.CardRange:0.##} further.{again}"),
                "Reach the back row. They paid less; they deserve a card too."),
            Card.VanishCooldown => (
                "Quick Smoke",
                Say($"The Vanish comes back {Tuning.CardVanishCooldown * 100f:0.#}% faster.{again}"),
                "The smoke clears. You do not."),
            Card.OneMoreCard => (
                "One More for the Lady",
                "Each throw sends one more card, at the next nearest critic.",
                "Pick a card. No, you. Yes, you too."),
            Card.Pierce => (
                "The Pierce",
                Say($"A card goes on through the critic it strikes, losing {Tuning.CardPierceLoss / withThis:0.###} each time."),
                "Through the critic, through the review, through the paper it is printed on."),
            Card.Ricochet => (
                "The Ricochet",
                Say($"A card that strikes turns to the next critic within {Tuning.CardRicochetReach:0.##}. Two copies: twice."),
                "No review goes unanswered."),
            Card.Burst => (
                "The Burst",
                Say($"Where a card strikes, a ring of {Tuning.CardBurstRadius:0.##} hurts for {Tuning.CardBurstShare * withThis * 100f:0.#}% of the card."),
                "Applause, but sharp."),
            _ => (
                "Louder Chorus",
                Say($"Every understudy's cards hurt for {Tuning.CardChorusDamage:0.##} more. Each copy again."),
                "Nine of you, and all of you have opinions."),
        };
    }
}
