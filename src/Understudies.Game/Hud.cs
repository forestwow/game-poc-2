using Microsoft.Xna.Framework;
using Understudies.Core;

using Vector2 = System.Numerics.Vector2;

namespace Understudies.Game;

// The HUD (plan T45, S3 of "The screens"): what is read of the show while an act runs. On the curtain the act and
// who is still to come, the way to the next encore under its label, the clock and the encores' count; under the
// curtain the one line that announces; at the magician's feet its hit points as pips over the Vanish's bar; and
// along the front of the floor, above the footlights, what is held as paper chips and the cast as squares. Every
// number is the simulation's own. A part of the game's one class, as the program's screen is: it draws with that
// class's batch and words. Its sizes are in world units, each the design's screen pixels in a window 1280 wide,
// where a unit is 26.67.
internal sealed partial class UnderstudiesGame
{
    // The curtain's two lines of words, as far from the stage's side as HudSide: the act and the clock on the
    // first, and under each a line in the small face. The clock is the larger of the two.
    private const float HudSide = 0.9f;
    private const float HudFirstLine = 1f;
    private const float HudSecondLine = 2.15f;
    private const float ClockHeight = 1.5f;
    private const float HudSmallHeight = 0.65f;

    // A label is in capitals, 14 screen pixels at 1280, its letters this far apart. In Atkinson Hyperlegible
    // and not in the design's Pixelify Sans, whose capital C is read as an O at every size tried ("ENOORE",
    // "OAST"): the design's face waits for the owner (plan T45).
    private const float LabelHeight = 0.525f;
    private const float LabelSpacing = 0.075f;

    // The way to the next encore, in the middle of the curtain under its label: a bar in an ink rim, the filled
    // part with a light upper edge, ApplauseShine of its height, and what it counts beside its end.
    private const string ApplauseLabel = "APPLAUSE TO THE NEXT ENCORE";
    private static readonly Vector2 ApplauseBar = new(11.25f, 0.525f);
    private const float ApplauseLabelLine = 0.85f;
    private const float ApplauseBarTop = 1.5f;
    private const float ApplauseShine = 3f / 14f;
    private const float ApplauseCountHeight = 0.8f;

    // The line that announces is under the curtain, its middle this far below the floor's top, and keeps this far
    // from the stage's sides. In an offer and between two acts the encores' line is under it.
    private const float AnnounceHeight = 0.9f;
    private const float AnnounceDrop = 1.05f;
    private const float AnnounceSide = 1f;
    private const float EncoresLineDrop = 2f;

    // The magician's hit points are a row of square pips PipsDrop under its feet, a pip for a hit point, and the
    // Vanish's bar is under the row and as wide. They are the HUD's size and not the figure's: a pip that followed
    // FiguresMeasure would be four screen pixels at the committed measure, and is not counted at a glance. A row
    // is no wider than PipsWidest: more pips than fit at the design's size are smaller ones, and where they would
    // be smaller than PipSmallest screen pixels the hit points are one bar of that width, as they were before.
    private const float PipsDrop = 0.3f;
    private const float PipSize = 0.2625f;
    private const float PipGap = 0.075f;
    private const float PipsWidest = 2.7f;
    private const float PipSmallest = 3f;
    private const float VanishBarGap = 0.15f;
    private const float VanishBarHeight = 0.19f;

    // Paper (a chip, a square of the cast) has an ink border this wide and a hard shadow as far down and to the
    // right: two screen pixels at 1280.
    private const float PaperBorder = 0.075f;

    // What is held is a row of chips, a card's name and, a little larger, how many of it, each in Atkinson
    // Hyperlegible (a name in Pixelify Sans at this size has the labels' trouble: "Oard Sharp"), with its label over it; a row that is too wide goes on in another above it. The bottom of the lowest row is HudFoot above the footlights.
    private const string HeldLabel = "YOU HOLD";
    private const float ChipHeight = 0.975f;
    private const float ChipPad = 0.375f;
    private const float ChipSpace = 0.225f;
    private const float ChipsApart = 0.3f;
    private const float ChipWordsHeight = 0.525f;
    private const float LabelLift = 0.45f;
    private const float HudFoot = 0.15f;
    private const float HudFootSide = 0.75f;
    private static readonly Color ChorusPaper = new(216, 240, 230);
    private static readonly Color ChipCount = new(160, 54, 95);

    // While cards are shown the chips are a row over them, in the middle, the row's bottom this far down the
    // stage, and each says how many of its card are held of how many may be (plan T41): a card that is full, which
    // no encore offers again, has its chip in the magician's gold. With nothing held the row's place says so.
    private const float HeldFootInAnOffer = 7.5f;
    private const string NothingHeld = "YOU HOLD NO CARD YET";

    // While an encore is read the act's clock stands, and is greyed.
    private static readonly Color ClockStands = new(140, 130, 144);

    // The cast is a row of squares at the right, one for every understudy in its act's tint and one for the act
    // that is being recorded, dark with a broken border. An understudy that is off the stage (its act's magician
    // fell, and its route has run out) has its square this much nearer the dark one, with its number in its tint.
    private const string CastLabel = "THE CAST ON STAGE";
    private const float CastSquare = 1.125f;
    private const float CastApart = 0.225f;
    private const float CastNumberHeight = 0.6f;
    private const float CastOffStage = 0.7f;
    private static readonly Color CastRecording = new(58, 46, 62);

    /// <summary>A chip of what is held, as wide as its words make it.</summary>
    private readonly record struct Chip(string Name, string Count, Color Paper, float Width);

    /// <summary>How tall a footlight is drawn, in world units: the HUD's foot stands on the row of them.</summary>
    private float FootlightTall
    {
        get
        {
            Sheet lamp = _sheets[(int)Figure.Footlight][0];
            return lamp.First.Height / (lamp.Block * SetPixelsPerUnit);
        }
    }

    /// <summary>
    /// The magician's hit points and its Vanish under its <paramref name="feet"/>, in the batch of the stage's
    /// shapes. A pip is lit while the magician has more hit points than the pips before it: a part of a hit point
    /// lost leaves its pip lit. A fallen magician has no Vanish to wait for. Laid on whole screen pixels, so that
    /// the pips are all one size, and kept on the stage.
    /// </summary>
    private void DrawTheMagiciansBars(Vector2 feet)
    {
        float Whole(float units) => MathF.Max(1f, MathF.Round(units * _scale));
        void Pixels(float x, float y, float width, float height, Color color) =>
            Fill(new Vector2(x, y) / _scale, new Vector2(width, height) / _scale, color);

        int count = Math.Max(1, (int)MathF.Ceiling(Tuning.MagicianHitPoints));
        float gap = Whole(PipGap);
        float widest = Whole(PipsWidest);
        float pip = MathF.Min(Whole(PipSize), MathF.Floor((widest - ((count - 1) * gap)) / count));
        bool pips = pip >= PipSmallest;
        float wide = pips ? (count * pip) + ((count - 1) * gap) : widest;
        float tall = pips ? pip : Whole(PipSize);
        float between = Whole(VanishBarGap);
        float bar = Whole(VanishBarHeight);

        // Kept on the stage sideways as well, the Vanish's bar with the row: a pixel of ink is left at each side.
        float left = MathF.Max(
            1f,
            MathF.Min(MathF.Round((feet.X * _scale) - (wide / 2f)), MathF.Round(Tuning.StageSize.X * _scale) - wide - 1f));
        float top = MathF.Min(
            MathF.Round((feet.Y + PipsDrop) * _scale),
            MathF.Floor(Tuning.StageSize.Y * _scale) - tall - between - bar - 1f);

        // The row stands in ink a pixel wider all round, which is a lost pip's colour: what is lost is dark.
        Pixels(left - 1f, top - 1f, wide + 2f, tall + 2f, HitPointsLost);
        if (pips)
        {
            for (int i = 0; i < count; i++)
            {
                if (_simulation.MagicianHitPoints > i)
                {
                    Pixels(left + (i * (pip + gap)), top, pip, pip, HitPoints);
                }
            }
        }
        else
        {
            float share = Math.Clamp(_simulation.MagicianHitPoints / Tuning.MagicianHitPoints, 0f, 1f);
            Pixels(left, top, wide * share, tall, HitPoints);
        }

        // The Vanish's bar fills as the Vanish comes back, and a full bar is a Vanish that is ready.
        if (!_simulation.MagicianHasFallen)
        {
            Pixels(left, top + tall + between, wide, bar, HitPointsLost);
            Pixels(left, top + tall + between, wide * (1f - _simulation.VanishCooldownLeft), bar, VanishBar);
        }
    }

    /// <summary>
    /// The way to the next encore, in the batch of the stage's shapes: the act's applause that no encore was paid
    /// with, over what the next costs.
    /// </summary>
    // ponytail: the design's bar has a faint glow about it, which is a blur and is not drawn: there is no
    // texture but the one pixel. A picture of a glow, if the bar is lost on the curtain.
    private void DrawTheApplauseBar()
    {
        var topLeft = new Vector2((Tuning.StageSize.X - ApplauseBar.X) / 2f, ApplauseBarTop);
        float share = Math.Clamp((float)_simulation.EncoreApplause / Math.Max(1, _simulation.EncoreCost), 0f, 1f);
        Fill(topLeft - new Vector2(PaperBorder), ApplauseBar + new Vector2(2f * PaperBorder), OutlineInk);
        Fill(topLeft, ApplauseBar with { X = ApplauseBar.X * share }, ApplauseGlow);
        Fill(topLeft, new Vector2(ApplauseBar.X * share, ApplauseBar.Y * ApplauseShine), ApplauseHeart);
    }

    /// <summary>
    /// The HUD's words and its paper, in the batch of the words: the curtain's lines, the line that announces
    /// when there is something <paramref name="said"/>, what is held and the cast.
    /// </summary>
    private void DrawTheHud(string? said)
    {
        float right = Tuning.StageSize.X - HudSide;
        float middle = Tuning.StageSize.X / 2f;
        Write(
            Face.Sentence,
            WordsHeight,
            $"Act {_simulation.Act} of {Tuning.ActsInPerformance}",
            new Vector2(HudSide, HudFirstLine),
            0f,
            Magician);

        // Under the act, how many of its critics are still to enter: from the curtain on.
        if (_simulation.Phase is Phase.Act or Phase.Encore or Phase.Curtain)
        {
            int toCome = _simulation.ActEntries.Count - _simulation.ActEntriesMade;
            Write(
                Face.Sentence,
                HudSmallHeight,
                $"{(toCome == 1 ? "1 critic" : $"{toCome} critics")} still to come",
                new Vector2(HudSide, HudSecondLine),
                0f,
                Words);
        }

        // A second that has begun still shows: the time reads 0:00 only when the act is over.
        int seconds = (_simulation.ActTicksLeft + Simulation.TicksPerSecond - 1) / Simulation.TicksPerSecond;
        bool encore = _simulation.Phase == Phase.Encore;
        Write(Face.Sentence, ClockHeight, $"{seconds / 60}:{seconds % 60:00}", new Vector2(right, HudFirstLine), 1f, encore ? ClockStands : Words);

        // Under the clock, the encores of the whole performance. While the stage stands for an offer or between
        // two acts the encores' line under the curtain says it, with what the next costs, and in an encore the
        // line under the clock says why the clock is grey.
        if (encore)
        {
            Write(Face.Sentence, HudSmallHeight, "the clock stands", new Vector2(right, HudSecondLine), 1f, Words);
        }
        else if (_simulation.Phase is not (Phase.Program or Phase.BetweenActs))
        {
            int encores = _simulation.EncoresTaken;
            Write(
                Face.Sentence,
                HudSmallHeight,
                encores == 0 ? "no encore yet tonight" : encores == 1 ? "1 encore tonight" : $"{encores} encores tonight",
                new Vector2(right, HudSecondLine),
                1f,
                Words);
        }

        Write(Face.Sentence, LabelHeight, ApplauseLabel, new Vector2(middle, ApplauseLabelLine), 0.5f, ApplauseHeart, spacing: LabelSpacing);
        Write(
            Face.Sentence,
            ApplauseCountHeight,
            $"{_simulation.EncoreApplause} / {_simulation.EncoreCost}",
            new Vector2(middle + (ApplauseBar.X / 2f) + 0.4f, ApplauseBarTop + (ApplauseBar.Y / 2f)),
            0f,
            ApplauseHeart);

        if (said is not null)
        {
            // In the middle of the stage, and smaller where it is longer than the stage leaves it between its
            // sides (to within the rounding of its size to a whole pixel, which the side takes).
            float room = Tuning.StageSize.X - (2f * AnnounceSide);
            Write(
                Face.Sentence,
                AnnounceHeight * MathF.Min(1f, room / Wide(Face.Sentence, AnnounceHeight, said)),
                said,
                new Vector2(middle, Tuning.StageFloorTop + AnnounceDrop),
                0.5f,
                Magician);
        }

        // While cards are shown, what is held is a row over them (plan T48), and the cast is not drawn: the
        // floor's front is the countdown's and the keys'.
        // ponytail: one row, which all nine kinds of card are at 1280 wide. A second row, which a longer name or a
        // tenth card would make, goes up into the encores' line: the cards move down by a row when that is seen.
        if (ProgramIsShown)
        {
            if (!DrawTheHeld(new Vector2(middle, HeldFootInAnOffer), 0.5f, Tuning.StageSize.X - 2f, ofTheLimit: true))
            {
                Write(
                    Face.Sentence,
                    LabelHeight,
                    NothingHeld,
                    new Vector2(middle, HeldFootInAnOffer - (ChipHeight / 2f)),
                    0.5f,
                    Words,
                    spacing: LabelSpacing);
            }

            return;
        }

        // Along the front of the floor, above the footlights: what is held at the left, and the cast at the right,
        // which the chips keep clear of however many acts it comes to have.
        float foot = Tuning.StageSize.Y - FootlightTall - HudFoot;
        float castWidest = (Tuning.ActsInPerformance * (CastSquare + CastApart)) - CastApart;
        DrawTheHeld(new Vector2(HudFootSide, foot), 0f, Tuning.StageSize.X - (2f * HudFootSide) - castWidest - 1f);
        DrawTheCast(new Vector2(Tuning.StageSize.X - HudFootSide, foot));
    }

    /// <summary>
    /// What is held, as rows of chips no wider than <paramref name="width"/>, in the order of the cards: a card
    /// that is not held has no chip, and a row has as many chips as fit. A chip says how many of its card are
    /// held, and <paramref name="ofTheLimit"/> of how many a magician may hold, where the card has a limit: the
    /// chorus card has none.
    /// </summary>
    private List<List<Chip>> HeldRows(float width, bool ofTheLimit)
    {
        List<List<Chip>> rows = [];
        foreach (Card card in Enum.GetValues<Card>())
        {
            if (Held(card) == 0)
            {
                continue;
            }

            // The count is a number read in a glance, and so in the sentences' face (plan T40).
            string name = Describe(card).Name;
            bool limited = ofTheLimit && card != Card.ChorusDamage && Tuning.CardMaxCopies > 0;
            string count = limited ? $"{Held(card)}/{Tuning.CardMaxCopies}" : $"×{Held(card)}";
            Color paper = card == Card.ChorusDamage ? ChorusPaper
                : limited && Held(card) >= Tuning.CardMaxCopies ? Magician
                : ThrownCardFace;
            float wide = (2f * ChipPad) + Wide(Face.Sentence, ChipWordsHeight, name) + ChipSpace + Wide(Face.Sentence, HudSmallHeight, count);
            if (rows.Count == 0 || RowWide(rows[^1]) + ChipsApart + wide > width)
            {
                rows.Add([]);
            }

            rows[^1].Add(new Chip(name, count, paper, wide));
        }

        return rows;
    }

    private static float RowWide(List<Chip> row) => row.Sum(chip => chip.Width) + ((row.Count - 1) * ChipsApart);

    /// <summary>
    /// What is held, as chips under their label, in the batch of the words: the lowest row's bottom edge is at
    /// <paramref name="foot"/> and the rows go up from there, each with its left end there, its middle
    /// (<paramref name="anchor"/> 0.5) or its right end (1), and none wider than <paramref name="width"/>. With
    /// nothing held nothing is drawn, and the answer is no. The one routine that draws what is held, wherever a
    /// screen wants it; <paramref name="ofTheLimit"/> is the offer's screen's, where the limit is what is chosen by.
    /// </summary>
    private bool DrawTheHeld(Vector2 foot, float anchor, float width, bool ofTheLimit = false)
    {
        List<List<Chip>> rows = HeldRows(width, ofTheLimit);
        float top = foot.Y;
        for (int row = rows.Count - 1; row >= 0; row--)
        {
            top -= ChipHeight;
            float left = foot.X - (RowWide(rows[row]) * anchor);
            foreach (Chip chip in rows[row])
            {
                float line = top + (ChipHeight / 2f);
                Paper(new Vector2(left, top), new Vector2(chip.Width, ChipHeight), chip.Paper);
                Write(Face.Sentence, ChipWordsHeight, chip.Name, new Vector2(left + ChipPad, line), 0f, OutlineInk, onPaper: true);
                Write(Face.Sentence, HudSmallHeight, chip.Count, new Vector2(left + chip.Width - ChipPad, line), 1f, ChipCount, onPaper: true);
                left += chip.Width + ChipsApart;
            }

            top -= ChipsApart;
        }

        if (rows.Count > 0)
        {
            Write(Face.Sentence, LabelHeight, HeldLabel, new Vector2(foot.X, top + ChipsApart - LabelLift), anchor, Words, spacing: LabelSpacing);
        }

        return rows.Count > 0;
    }

    /// <summary>
    /// The cast, as a row of squares whose bottom right corner is at <paramref name="foot"/>, under its label:
    /// every understudy by its act's number on its act's tint, and last the act that is played, which is being
    /// recorded.
    /// </summary>
    private void DrawTheCast(Vector2 foot)
    {
        IReadOnlyList<Understudy> cast = _simulation.Understudies;
        var size = new Vector2(CastSquare);
        float left = foot.X - ((cast.Count + 1) * (CastSquare + CastApart)) + CastApart;
        float top = foot.Y - CastSquare;
        for (int i = 0; i <= cast.Count; i++)
        {
            var topLeft = new Vector2(left + (i * (CastSquare + CastApart)), top);
            bool recording = i == cast.Count;
            if (recording)
            {
                Paper(topLeft, size, CastRecording, broken: true);
            }
            else
            {
                Paper(topLeft, size, cast[i].IsOnStage ? TintOf(cast[i]) : Color.Lerp(TintOf(cast[i]), CastRecording, CastOffStage));
            }

            Write(
                Face.Sentence,
                CastNumberHeight,
                $"{(recording ? _simulation.Act : cast[i].Act)}",
                topLeft + (size / 2f),
                0.5f,
                recording ? Words : cast[i].IsOnStage ? OutlineInk : TintOf(cast[i]),
                onPaper: true);
        }

        Write(Face.Sentence, LabelHeight, CastLabel, new Vector2(foot.X, top - LabelLift), 1f, Words, spacing: LabelSpacing);
    }

    /// <summary>
    /// A piece of paper in the batch of the words, on whole screen pixels: its <paramref name="fill"/> in an ink
    /// border, with a hard ink shadow down and to the right (<see cref="Fill"/> is in screen pixels in this
    /// batch, which has no transform). A <paramref name="broken"/> border is in stretches,
    /// each two borders long and as far apart. A card's <paramref name="border"/> and <paramref name="shadow"/>
    /// are wider than a chip's, in world units.
    /// </summary>
    private void Paper(Vector2 topLeft, Vector2 size, Color fill, bool broken = false, float border = PaperBorder, float shadow = PaperBorder)
    {
        Vector2 at = _corner + (topLeft * _scale);
        at = new Vector2(MathF.Round(at.X), MathF.Round(at.Y));
        var whole = new Vector2(MathF.Round(size.X * _scale), MathF.Round(size.Y * _scale));
        border = MathF.Max(1f, MathF.Round(border * _scale));
        Fill(at + new Vector2(MathF.Max(1f, MathF.Round(shadow * _scale))), whole, OutlineInk);
        Fill(at, whole, OutlineInk);
        Fill(at + new Vector2(border), whole - new Vector2(2f * border), fill);
        if (!broken)
        {
            return;
        }

        for (float along = 2f * border; along + (2f * border) <= whole.X - (2f * border); along += 4f * border)
        {
            Fill(at + new Vector2(along, 0f), new Vector2(2f * border, border), fill);
            Fill(at + new Vector2(along, whole.Y - border), new Vector2(2f * border, border), fill);
        }

        for (float along = 2f * border; along + (2f * border) <= whole.Y - (2f * border); along += 4f * border)
        {
            Fill(at + new Vector2(0f, along), new Vector2(border, 2f * border), fill);
            Fill(at + new Vector2(whole.X - border, along), new Vector2(border, 2f * border), fill);
        }
    }
}
