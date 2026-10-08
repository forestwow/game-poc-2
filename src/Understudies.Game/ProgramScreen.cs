using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Understudies.Core;

using Vector2 = System.Numerics.Vector2;

namespace Understudies.Game;

// The program's screen (plan T18): the offer as cards on the dimmed stage, the choosing, and the card just taken
// shown for a moment. It is a part of the game's one class because it draws with that class's batch and words.
internal sealed partial class UnderstudiesGame
{
    // The cards stand side by side in the front half of the floor, centred, their tops this far above the stage's
    // bottom edge: the box office and whoever stands at it are clear of them. Under them the countdown, and at
    // the stage's bottom edge what the magician holds.
    private static readonly Vector2 PanelSize = new(13f, 8f);
    private const float PanelGap = 1.5f;
    private const float PanelLift = 13.4f;

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

    // A self card is the magician's yellow and the chorus card the first understudy's green: the two colours the
    // stage already has for the two.
    private static readonly Color Ink = new(24, 18, 28);
    private static readonly Color FaintInk = new(110, 96, 100);

    // The offer of the program that is up, or of the one whose card was just taken: the simulation empties its
    // own with the pick.
    private IReadOnlyList<Card> _offered = [];
    private int _highlighted;
    private int _taken;
    private float _takenLeft;

    /// <summary>The cards are on the screen: a program is up, or its card was taken a moment ago.</summary>
    private bool ProgramIsShown =>
        _simulation.Phase == Phase.Program || (_simulation.Phase == Phase.BetweenActs && _takenLeft > 0f);

    /// <summary>
    /// A frame's presses in the program. Left and right (the arrows, A and D, a gamepad's d-pad or its left stick)
    /// move the highlight; Enter, Space or the gamepad's A take the highlighted card; 1, 2 and 3 take the card in
    /// that place at once. Every one of them is a press and never a hold: a key or a stick held since the act,
    /// Space for a Vanish or D to walk, was down the frame before and does nothing until it has been let go.
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

    /// <summary>The card at <paramref name="place"/> was taken: it stays lit for a moment, with a chime.</summary>
    private void Acknowledge(int place)
    {
        _taken = place;
        _takenLeft = TakenTime;

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

    /// <summary>The highlighted card of a program that is up, and afterwards the one that was taken.</summary>
    private bool IsLit(int place) => place == (_simulation.Phase == Phase.Program ? _highlighted : _taken);

    /// <summary>All of a card that was taken and of every card while they are offered; little of one passed over.</summary>
    private float Seen(int place) => _simulation.Phase == Phase.Program || IsLit(place) ? 1f : NotTakenOpacity;

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

        if (_simulation.Phase == Phase.Program)
        {
            FillBar(
                new Vector2((Tuning.StageSize.X - CountdownBar.X) / 2f, PanelsBottom + 2f),
                CountdownBar,
                _simulation.ProgramTicksLeft / MathF.Max(1f, Tuning.ProgramTime * Simulation.TicksPerSecond),
                Words);
        }
    }

    /// <summary>
    /// The words of the program and of the stage between two acts: what the act's applause earned, under its bar,
    /// and what the magician holds, at the stage's bottom edge; and while the cards are shown the words on them,
    /// with the countdown and the keys under them while they are offered.
    /// </summary>
    private void DrawProgramWords()
    {
        float middle = Tuning.StageSize.X / 2f;
        string earned = _simulation.ActApplauseBand switch
        {
            ApplauseBand.None => "The house sat on its hands: no applause, no card.",
            ApplauseBand.UnderTheFirst => "A little applause, short of the first notch: one card, and no choice.",
            ApplauseBand.First => "The applause reached the first notch: a choice of two cards.",
            _ => "The applause reached the second notch: a choice of three cards.",
        };
        Write(SmallWordsHeight, earned, new Vector2(middle, Tuning.StageFloorTop + 0.9f), 0.5f, ApplauseHeart);

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
            (string name, string what) = Describe(card);
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
            Write(NameHeight, name, centre + new Vector2(0f, 2.7f), 0.5f, ink);
            string[] lines = what.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                Write(SmallWordsHeight, lines[i], centre + new Vector2(0f, 4.4f + (i * 1.1f)), 0.5f, ink);
            }

            if (Held(card) > 0)
            {
                Write(
                    SmallWordsHeight,
                    chorus ? $"the chorus has {Held(card)}" : $"you have {Held(card)}",
                    centre + new Vector2(0f, PanelSize.Y - 0.8f),
                    0.5f,
                    FaintInk * Seen(place));
            }
        }

        if (_simulation.Phase != Phase.Program)
        {
            return;
        }

        // A second that has begun still shows, as on the act's clock. Nobody is to be surprised by what the end
        // of the time does.
        int seconds = (_simulation.ProgramTicksLeft + Simulation.TicksPerSecond - 1) / Simulation.TicksPerSecond;
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

    /// <summary>A card's name and what one of it changes, in the numbers of now and in two lines.</summary>
    private (string Name, string What) Describe(Card card)
    {
        // A point and never a comma, whatever the machine's language.
        static string Say(FormattableString words) => words.ToString(CultureInfo.InvariantCulture);

        return card switch
        {
            Card.Damage => ("Sharper cards", Say($"your cards hurt\n{Tuning.CardDamage:0.##} more")),
            Card.AttackSpeed => ("Quicker hands", Say($"you throw\n{Tuning.CardAttackSpeed * 100f:0}% more often")),
            Card.Range => ("Longer arm", Say($"your throw reaches\n{Tuning.CardRange:0.##} further")),
            Card.VanishCooldown =>
                ("Quicker Vanish", Say($"your Vanish comes back\n{Tuning.CardVanishCooldown * 100f:0}% sooner")),
            Card.OneMoreCard => ("One more card", "each throw sends one more,\nat the next nearest"),
            _ => ("Sharper chorus", Say($"every understudy's cards\nhurt {Tuning.CardChorusDamage:0.##} more")),
        };
    }
}
