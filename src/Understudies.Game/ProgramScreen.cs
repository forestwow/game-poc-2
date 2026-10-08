using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Understudies.Core;

using Vector2 = System.Numerics.Vector2;

namespace Understudies.Game;

// The program's screen (plan T18): the offer as cards on the dimmed stage, the choosing, and the card just taken
// shown for a moment. An encore's three cards are offered on the same panels, in the act (plan T23). It is a part
// of the game's one class because it draws with that class's batch and words.
internal sealed partial class UnderstudiesGame
{
    // The cards stand side by side in the front half of the floor, centred, their tops this far above the stage's
    // bottom edge: the box office is clear of them, and whoever stands in front of it, or anywhere on that strip
    // of the floor, the magician too, is behind them. Under them the countdown, and at the stage's bottom edge
    // what the magician holds.
    private static readonly Vector2 PanelSize = new(13f, 6.5f);
    private const float PanelGap = 1.5f;
    private const float PanelLift = 11.9f;

    // The band along a panel's top says whose the card is, and the highlighted panel stands this much higher
    // than the others, in a frame this thick.
    private const float PanelBand = 1.3f;
    private const float PanelRaise = 0.4f;
    private const float PanelFrame = 0.2f;

    private const float NameHeight = 1.2f;
    private const float SmallWordsHeight = 0.8f;
    private const float CountdownHeight = 1f;
    private static readonly Vector2 CountdownBar = new(20f, 0.4f);

    // How dark the stage goes under the cards, and how faint the cards not taken are beside the one that was.
    private const float ProgramDim = 0.3f;
    private const float NotTakenOpacity = 0.3f;

    // Seconds a card just taken stays on the screen before the line between two acts. No go-on is taken in them.
    private const float TakenTime = 0.8f;

    // How far a stick is pushed to the side before it moves the highlight.
    private const float StickLean = 0.5f;

    // Seconds from an encore's opening in which no card is taken: an encore opens in the middle of a fight, and
    // the Space pressed for a Vanish a moment too late must not take a card nobody has read.
    private const float EncoreGuardTime = 0.4f;

    // A self card is the magician's yellow and the chorus card the first understudy's green: the two colours the
    // stage already has for the two.
    private static readonly Color Ink = new(24, 18, 28);
    private static readonly Color FaintInk = new(110, 96, 100);

    // The offer that is up, an encore's or the program's, or the one last taken from: the simulation empties its
    // own with the pick.
    private IReadOnlyList<Card> _offered = [];
    private int _highlighted;
    private int _taken;
    private float _takenLeft;
    private float _guardLeft;

    /// <summary>Cards are on offer: an encore is read, or the program.</summary>
    private bool IsOffered => _simulation.Phase is Phase.Encore or Phase.Program;

    /// <summary>The cards are on the screen: they are on offer, or the program's card was taken a moment ago.</summary>
    private bool ProgramIsShown => IsOffered || (_simulation.Phase == Phase.BetweenActs && _takenLeft > 0f);

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

    /// <summary>Where the panel at <paramref name="place"/> has its top left corner, raised when it is lit.</summary>
    private Vector2 PanelTopLeft(int place)
    {
        float row = (_offered.Count * PanelSize.X) + ((_offered.Count - 1) * PanelGap);
        return new Vector2(
            ((Tuning.StageSize.X - row) / 2f) + (place * (PanelSize.X + PanelGap)),
            Tuning.StageSize.Y - PanelLift - (IsLit(place) ? PanelRaise : 0f));
    }

    /// <summary>The highlighted card while cards are offered, and afterwards the one that was taken.</summary>
    private bool IsLit(int place) => place == (IsOffered ? _highlighted : _taken);

    /// <summary>All of a card that was taken and of every card while they are offered; little of one passed over.</summary>
    private float Seen(int place) => IsOffered || IsLit(place) ? 1f : NotTakenOpacity;

    private float PanelsBottom => Tuning.StageSize.Y - PanelLift + PanelSize.Y;

    /// <summary>The shapes of the program, in world units: the panels and the countdown's bar.</summary>
    private void DrawProgramPanels()
    {
        for (int place = 0; place < _offered.Count; place++)
        {
            Vector2 topLeft = PanelTopLeft(place);
            Color whose = _offered[place] == Card.ChorusDamage ? UnderstudyTints[0] : Magician;
            if (IsLit(place))
            {
                // The taken card flashes: its band is white at first and comes back to its colour.
                whose = Color.Lerp(whose, Color.White, _takenLeft / TakenTime);
                Fill(topLeft - new Vector2(PanelFrame), PanelSize + new Vector2(2f * PanelFrame), Color.White);
            }

            Fill(topLeft, PanelSize, (IsLit(place) ? ThrownCardFace : UnderstudysCardFace) * Seen(place));
            Fill(topLeft, PanelSize with { Y = PanelBand }, whose * Seen(place));
        }

        if (IsOffered)
        {
            float time = _simulation.Phase == Phase.Encore ? Tuning.EncoreTime : Tuning.ProgramTime;
            FillBar(
                new Vector2((Tuning.StageSize.X - CountdownBar.X) / 2f, PanelsBottom + 2f),
                CountdownBar,
                _simulation.OfferTicksLeft / MathF.Max(1f, time * Simulation.TicksPerSecond),
                Words);
        }
    }

    /// <summary>
    /// The words of an encore, of the program and of the stage between two acts: how many encores were taken, under
    /// the applause's bar, and what the magician holds, at the stage's bottom edge; and while the cards are shown
    /// the words on them, with the countdown and the keys under them while they are offered.
    /// </summary>
    private void DrawProgramWords()
    {
        float middle = Tuning.StageSize.X / 2f;

        // While an encore is read its cost is still the one it was earned at: "the next" would be this very one.
        int encores = _simulation.EncoresTaken;
        string soFar = $"{(encores == 1 ? "1 encore" : $"{encores} encores")} so far.";
        Write(
            SmallWordsHeight,
            _simulation.Phase == Phase.Encore
                ? $"{soFar} This one was earned with {_simulation.EncoreCost} pieces of applause."
                : $"{soFar} The next costs {_simulation.EncoreCost} pieces of applause in one act.",
            new Vector2(middle, Tuning.StageFloorTop + 0.9f),
            0.5f,
            ApplauseHeart);

        var held = Enum.GetValues<Card>().Where(card => Held(card) > 0).Select(card => $"{Describe(card).Name} x{Held(card)}");
        string holds = string.Join(", ", held) is { Length: > 0 } cards ? $"You hold: {cards}" : "You hold no card yet.";
        Write(SmallWordsHeight, holds, new Vector2(middle, Tuning.StageSize.Y - 0.8f), 0.5f, Words);

        if (!ProgramIsShown)
        {
            return;
        }

        for (int place = 0; place < _offered.Count; place++)
        {
            Card card = _offered[place];
            bool chorus = card == Card.ChorusDamage;
            (string name, string first, string second) = Describe(card);
            Vector2 topLeft = PanelTopLeft(place);
            Vector2 centre = topLeft + new Vector2(PanelSize.X / 2f, 0f);
            Color ink = Ink * Seen(place);

            // The band: the card's key at its left end, and whose the card is.
            Write(SmallWordsHeight, $"{place + 1}", topLeft + new Vector2(0.4f, PanelBand / 2f), 0f, ink);
            Write(
                SmallWordsHeight,
                chorus ? "For every understudy" : "For the magician",
                centre + new Vector2(0f, PanelBand / 2f),
                0.5f,
                ink);
            Write(NameHeight, name, centre + new Vector2(0f, 2.4f), 0.5f, ink);
            Write(SmallWordsHeight, first, centre + new Vector2(0f, 3.7f), 0.5f, ink);
            Write(SmallWordsHeight, second, centre + new Vector2(0f, 4.7f), 0.5f, ink);

            if (Held(card) > 0)
            {
                Write(
                    SmallWordsHeight,
                    chorus ? $"the chorus has {Held(card)}" : $"you have {Held(card)}",
                    centre + new Vector2(0f, PanelSize.Y - 0.7f),
                    0.5f,
                    FaintInk * Seen(place));
            }
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
            CountdownHeight,
            $"{seconds} s left: then {(choice ? "the leftmost card" : "this card")} is taken for you.",
            new Vector2(middle, PanelsBottom + 1.2f),
            0.5f,
            Magician);
        Write(
            SmallWordsHeight,
            choice
                ? $"Left and right choose, Enter or Space takes. Or press {(_offered.Count == 2 ? "1 or 2" : "1 to 3")}. Gamepad: the stick and A."
                : "Enter, Space, 1 or a gamepad's A takes it.",
            new Vector2(middle, PanelsBottom + 3.3f),
            0.5f,
            Words);
    }

    /// <summary>How many of a card were taken in this performance: the magician's own, or the chorus's.</summary>
    private int Held(Card card) => card switch
    {
        Card.Damage => _simulation.MagicianCards.Damage,
        Card.AttackSpeed => _simulation.MagicianCards.AttackSpeed,
        Card.Range => _simulation.MagicianCards.Range,
        Card.VanishCooldown => _simulation.MagicianCards.VanishCooldown,
        Card.OneMoreCard => _simulation.MagicianCards.OneMoreCard,
        _ => _simulation.ChorusCards,
    };

    /// <summary>A card's name and, in two lines, what one of it changes, in the numbers of now.</summary>
    private (string Name, string First, string Second) Describe(Card card)
    {
        // A point and never a comma, whatever the machine's language. A share is a per cent with a decimal where
        // it has one: an eighth is 12.5% and not 12%.
        static string Say(FormattableString words) => words.ToString(CultureInfo.InvariantCulture);

        // A card's share is of the rate and not of the wait: a quarter makes the Vanish come back a quarter
        // faster, which is a fifth sooner.
        return card switch
        {
            Card.Damage => ("Sharper cards", "your cards hurt", Say($"{Tuning.CardDamage:0.##} more")),
            Card.AttackSpeed =>
                ("Quicker hands", "you throw", Say($"{Tuning.CardAttackSpeed * 100f:0.#}% more often")),
            Card.Range => ("Longer arm", "your throw reaches", Say($"{Tuning.CardRange:0.##} further")),
            Card.VanishCooldown =>
                ("Quicker Vanish", "your Vanish comes back", Say($"{Tuning.CardVanishCooldown * 100f:0.#}% faster")),
            Card.OneMoreCard => ("One more card", "each throw sends one more,", "at the next nearest"),
            _ => ("Sharper chorus", "every understudy's cards", Say($"hurt {Tuning.CardChorusDamage:0.##} more")),
        };
    }
}
