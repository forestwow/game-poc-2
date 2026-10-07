using System.Buffers.Binary;
using System.Diagnostics;
using Microsoft.Xna.Framework.Audio;
using Understudies.Core;

namespace Understudies.Game;

/// <summary>
/// What a blow sounds like: six short sounds, each played when a tick reports what it is for, and all of them
/// worked out in code when the game starts. There is no sound file. Like the juice it is the view's own state and
/// decides no rule, so it may use what the rules may not (<see cref="Random"/>, a dictionary, trigonometry, a
/// clock), and like the juice it is fed the simulation after every tick (<see cref="Feed"/>: a tick's events are
/// gone on the next).
/// </summary>
internal sealed class Sound
{
    // Every number of the sound is here, to be turned by hand: none is a rule, so none is in tuning.json. Times are
    // in seconds and pitches in hertz. Nobody can hear a number: a changed one is judged by starting the game.

    // How loud everything is: every sound's own level is taken times this. 1 is as loud as the machine is set.
    private const float MasterLevel = 0.5f;

    // How loud each sound is, from 0 to 1. What comes many times a second (a throw, a hit, a crowd's strikes) is
    // quiet, and what comes now and then (a kill, a Vanish) or is bad news (the magician is hurt) is not. A level
    // is of a sound's loudest sample, which is the same in all six (Peak), so one level is not one loudness: a
    // buzz that holds is louder than a tap at the same level.
    private const float ThrowLevel = 0.15f;
    private const float HitLevel = 0.4f;
    private const float StrikeLevel = 0.35f;
    private const float KillLevel = 0.9f;
    private const float VanishLevel = 0.7f;
    private const float HurtLevel = 0.5f;

    // A sound does not start again within this long of its own last start: a crowd strikes the box office many
    // times a second, and every blow of one tick would start at once.
    // ponytail: one gap for all six, so a sound longer than the gap still lies on itself under a crowd, the buzz
    // of a hurt magician four deep. A gap of its own for each sound, near its length, when that is too much.
    private const float RepeatGap = 0.05f;

    // Each time a sound is played it is this far higher or lower at most, in octaves, by chance: the same sound
    // twice in a row is not the same sound twice.
    private const float PitchSpread = 0.06f;

    // A sound comes from the side of the stage it happened on. At the stage's edge it is this far to that side,
    // where 1 is all MonoGame gives: sixty degrees off the middle.
    private const float PanReach = 0.35f;

    // The throw: a flick. A tone that climbs from ThrowFrom to ThrowTo, with a hiss over it, ThrowHiss as loud, of
    // what noise has between ThrowHissAbove and ThrowHissBelow. It swells and dies with no edge to it, loudest
    // when ThrowLoudestAt of its time has gone by.
    private const float ThrowTime = 0.07f;
    private const float ThrowFrom = 700f;
    private const float ThrowTo = 1700f;
    private const float ThrowHiss = 0.6f;
    private const float ThrowHissAbove = 2500f;
    private const float ThrowHissBelow = 6000f;
    private const float ThrowLoudestAt = 0.3f;

    // The hit: a dry tap. Noise between HitNoiseAbove and HitNoiseBelow, and under it a body HitBody as loud, a
    // tone that drops from HitBodyFrom to HitBodyTo. Both are a third as loud after every HitDecay.
    private const float HitTime = 0.06f;
    private const float HitNoiseAbove = 700f;
    private const float HitNoiseBelow = 3000f;
    private const float HitBody = 0.3f;
    private const float HitBodyFrom = 520f;
    private const float HitBodyTo = 300f;
    private const float HitDecay = 0.012f;

    // The kill: paper torn and a fall. A tone that falls from KillFrom to KillTo, with its octave KillOctave as
    // loud over it, a third as loud after every KillDecay. And the tear, KillTear as loud: noise between
    // KillTearAbove and KillTearBelow whose loudness jumps by chance every KillTearGrain, a third as loud after
    // every KillTearDecay.
    private const float KillTime = 0.3f;
    private const float KillFrom = 660f;
    private const float KillTo = 165f;
    private const float KillOctave = 0.25f;
    private const float KillDecay = 0.1f;
    private const float KillTear = 4f;
    private const float KillTearAbove = 1500f;
    private const float KillTearBelow = 5000f;
    private const float KillTearGrain = 0.005f;
    private const float KillTearDecay = 0.08f;

    // The Vanish: a whoosh. Noise heard through a gap that climbs from VanishFrom to VanishTo (the gap is the
    // narrower the greater VanishSharpness), swelling until VanishLoudestAt of its time has gone by and dying
    // over the rest.
    private const float VanishTime = 0.33f;
    private const float VanishFrom = 500f;
    private const float VanishTo = 3000f;
    private const float VanishSharpness = 2f;
    private const float VanishLoudestAt = 0.25f;

    // The strike on the box office: a thump. A tone that drops from StrikeFrom to StrikeTo within StrikeDrop and
    // is a third as loud after every StrikeDecay, pushed StrikeDrive hard into a ceiling, which gives it the edge
    // that small speakers need to say anything of a low tone. And the knock of the blow, StrikeKnock as loud:
    // what noise has below StrikeKnockBelow, a third as loud after every StrikeKnockDecay.
    private const float StrikeTime = 0.12f;
    private const float StrikeFrom = 300f;
    private const float StrikeTo = 90f;
    private const float StrikeDrop = 0.07f;
    private const float StrikeDecay = 0.04f;
    private const float StrikeDrive = 3f;
    private const float StrikeKnock = 6f;
    private const float StrikeKnockBelow = 1200f;
    private const float StrikeKnockDecay = 0.008f;

    // The magician is hurt: a low buzz, and bad news. Two square tones that sink together from HurtFrom to HurtTo,
    // the second HurtClash times as high as the first: half an octave, which grates. What they have above
    // HurtBelow is dulled, and they fade over the last HurtFade.
    private const float HurtTime = 0.22f;
    private const float HurtFrom = 262f;
    private const float HurtTo = 175f;
    private const float HurtClash = 1.41f;
    private const float HurtBelow = 2500f;
    private const float HurtFade = 0.08f;

    // No sound begins or ends with a jump, which would be heard as a click: each rises from nothing over Attack
    // and is brought down to nothing over Release. And each is made as loud as it can be told to play: its
    // loudest sample is Peak of all that 16 bits hold.
    private const float Attack = 0.003f;
    private const float Release = 0.01f;
    private const float Peak = 0.9f;

    // Samples a second, and where the noise starts: the same noise at every start, so what was listened to is
    // what plays.
    private const int SampleRate = 44100;
    private const int NoiseSeed = 1;

    // What plays for each kind of event, how loud, and when it last started by the stopwatch's count: 0 before
    // its first start, which is long ago to a stopwatch that counts from the machine's own start.
    private readonly Dictionary<TickEventKind, (SoundEffect Effect, float Level, long StartedAt)> _sounds = [];

    /// <param name="silent">A capture only draws a frame: it asks for no audio device and plays nothing.</param>
    public Sound(bool silent)
    {
        if (silent)
        {
            return;
        }

        // ponytail: the sounds are never disposed: they are there for as long as the game runs, and MonoGame
        // closes the audio device under them when the game is disposed.
        try
        {
            foreach (var (kind, level, samples) in Synthesise())
            {
                _sounds[kind] = (new SoundEffect(samples, SampleRate, AudioChannels.Mono), level, 0);
            }
        }
        catch (NoAudioHardwareException)
        {
            // A machine with no audio device plays the game all the same.
            Console.WriteLine("No audio device could be opened: the game runs silent.");
        }
    }

    /// <summary>
    /// Nothing new is played while this is set. A sound that has started rings out: none is longer than a third of
    /// a second.
    /// </summary>
    public bool Muted { get; set; }

    /// <summary>Plays what the last tick did. Called after every tick: a frame may run several.</summary>
    public void Feed(Simulation simulation)
    {
        if (Muted)
        {
            return;
        }

        foreach (TickEvent happened in simulation.Events)
        {
            // A kind of event that has no sound makes none; nor does any, on a machine with no audio device.
            if (!_sounds.TryGetValue(happened.Kind, out var sound)
                || Stopwatch.GetElapsedTime(sound.StartedAt).TotalSeconds < RepeatGap)
            {
                continue;
            }

            _sounds[happened.Kind] = sound with { StartedAt = Stopwatch.GetTimestamp() };

            // From -1 at the stage's left edge to 1 at its right. A critic may stand a little outside the stage.
            float side = Math.Clamp((2f * happened.Position.X / simulation.Tuning.StageSize.X) - 1f, -1f, 1f);
            float pitch = ((Random.Shared.NextSingle() * 2f) - 1f) * PitchSpread;

            // No source left to play it on is no error: the sound is not heard this once.
            sound.Effect.Play(sound.Level * MasterLevel, pitch, side * PanReach);
        }
    }

    /// <summary>
    /// The six sounds as they are played: the kind of event each is for, how loud it plays, and its samples, 16
    /// bits each and one channel, <see cref="SampleRate"/> of them a second.
    /// </summary>
    private static (TickEventKind Kind, float Level, byte[] Samples)[] Synthesise() =>
    [
        (TickEventKind.Throw, ThrowLevel, Throw()),
        (TickEventKind.Hit, HitLevel, Hit()),
        (TickEventKind.Kill, KillLevel, Kill()),
        (TickEventKind.Vanish, VanishLevel, Vanish()),
        (TickEventKind.BoxOfficeStruck, StrikeLevel, Strike()),
        (TickEventKind.MagicianHurt, HurtLevel, Hurt()),
    ];

    private static byte[] Throw()
    {
        Func<float, float> tone = Sine(t => Glide(ThrowFrom, ThrowTo, t / ThrowTime));
        Func<float, float> above = HighPass(ThrowHissAbove);
        Func<float, float> below = LowPass(ThrowHissBelow);
        Func<float> noise = Noise();
        return Render(ThrowTime, t =>
            (tone(t) + (ThrowHiss * below(above(noise())))) * Swell(t / ThrowTime, ThrowLoudestAt));
    }

    private static byte[] Hit()
    {
        Func<float, float> body = Sine(t => Glide(HitBodyFrom, HitBodyTo, t / HitTime));
        Func<float, float> above = HighPass(HitNoiseAbove);
        Func<float, float> below = LowPass(HitNoiseBelow);
        Func<float> noise = Noise();
        return Render(HitTime, t => (below(above(noise())) + (HitBody * body(t))) * Decay(t, HitDecay));
    }

    private static byte[] Kill()
    {
        Func<float, float> tone = Sine(t => Glide(KillFrom, KillTo, t / KillTime));
        Func<float, float> octave = Sine(t => 2f * Glide(KillFrom, KillTo, t / KillTime));
        Func<float, float> above = HighPass(KillTearAbove);
        Func<float, float> below = LowPass(KillTearBelow);
        Func<float> noise = Noise();
        float grain = 0f;
        int grainLeft = 0;
        return Render(KillTime, t =>
        {
            // The tear is ragged: a new loudness every grain, more often small than great.
            if (grainLeft-- <= 0)
            {
                grainLeft = (int)(KillTearGrain * SampleRate);
                grain = noise() * noise();
            }

            return ((tone(t) + (KillOctave * octave(t))) * Decay(t, KillDecay))
                + (KillTear * grain * below(above(noise())) * Decay(t, KillTearDecay));
        });
    }

    private static byte[] Vanish()
    {
        Func<float> noise = Noise();
        float low = 0f;
        float band = 0f;
        return Render(VanishTime, t =>
        {
            // A state-variable filter: `band` is what the noise has near the pitch the gap is at now.
            float gone = t / VanishTime;
            float rate = 2f * MathF.Sin(MathF.PI * Glide(VanishFrom, VanishTo, gone) / SampleRate);
            low += rate * band;
            band += rate * (noise() - low - (band / VanishSharpness));
            return band * Swell(gone, VanishLoudestAt);
        });
    }

    private static byte[] Strike()
    {
        Func<float, float> tone = Sine(t => Glide(StrikeFrom, StrikeTo, t / StrikeDrop));
        Func<float, float> below = LowPass(StrikeKnockBelow);
        Func<float> noise = Noise();
        return Render(StrikeTime, t =>
            MathF.Tanh(StrikeDrive * tone(t) * Decay(t, StrikeDecay))
            + (StrikeKnock * below(noise()) * Decay(t, StrikeKnockDecay)));
    }

    private static byte[] Hurt()
    {
        // ponytail: a square made by cutting a sine off has more in it than the sample rate can hold, and the
        // rest comes back as a faint hash. It is not heard under a tone this low; a square an octave or two
        // higher has to be built of its overtones.
        Func<float, float> one = Sine(t => Glide(HurtFrom, HurtTo, t / HurtTime));
        Func<float, float> other = Sine(t => HurtClash * Glide(HurtFrom, HurtTo, t / HurtTime));
        Func<float, float> below = LowPass(HurtBelow);
        return Render(HurtTime, t =>
            below(MathF.Sign(one(t)) + MathF.Sign(other(t))) * MathF.Min(1f, (HurtTime - t) / HurtFade));
    }

    /// <summary>
    /// <paramref name="seconds"/> of <paramref name="wave"/>, which is asked for every sample in turn with the
    /// time since the sound began: brought up from nothing and down to nothing at its ends, made <see cref="Peak"/>
    /// loud, and written as 16-bit samples.
    /// </summary>
    private static byte[] Render(float seconds, Func<float, float> wave)
    {
        var samples = new float[(int)(seconds * SampleRate)];
        float loudest = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / SampleRate;
            float left = (float)(samples.Length - 1 - i) / SampleRate;
            samples[i] = wave(t) * MathF.Min(1f, MathF.Min(t / Attack, left / Release));
            loudest = MathF.Max(loudest, MathF.Abs(samples[i]));
        }

        byte[] bytes = new byte[samples.Length * sizeof(short)];
        for (int i = 0; i < samples.Length; i++)
        {
            BinaryPrimitives.WriteInt16LittleEndian(
                bytes.AsSpan(i * sizeof(short)), (short)(samples[i] / loudest * Peak * short.MaxValue));
        }

        return bytes;
    }

    /// <summary>A tone whose pitch at each moment is what <paramref name="hertz"/> says: from -1 to 1.</summary>
    private static Func<float, float> Sine(Func<float, float> hertz)
    {
        float turns = 0f;
        return t => MathF.Sin(MathF.Tau * (turns += hertz(t) / SampleRate));
    }

    /// <summary>White noise, from -1 to 1.</summary>
    private static Func<float> Noise()
    {
        var random = new Random(NoiseSeed);
        return () => (random.NextSingle() * 2f) - 1f;
    }

    /// <summary>
    /// Lets through what a sound has below <paramref name="hertz"/>, and less and less of what is above: a quarter
    /// of it an octave higher, a sixteenth two octaves higher.
    /// </summary>
    private static Func<float, float> LowPass(float hertz)
    {
        // The same simple filter twice over: once leaves too much hiss in noise.
        float share = 1f - MathF.Exp(-MathF.Tau * hertz / SampleRate);
        float once = 0f;
        float twice = 0f;
        return sample => twice += share * ((once += share * (sample - once)) - twice);
    }

    /// <summary>Lets through what a sound has above <paramref name="hertz"/>, and less and less of what is below.</summary>
    private static Func<float, float> HighPass(float hertz)
    {
        Func<float, float> low = LowPass(hertz);
        return sample => sample - low(sample);
    }

    /// <summary>
    /// From <paramref name="from"/> to <paramref name="to"/> as <paramref name="gone"/> goes from 0 to 1, by equal
    /// steps of pitch and not of hertz; it stays at <paramref name="to"/> after that.
    /// </summary>
    private static float Glide(float from, float to, float gone) => from * MathF.Pow(to / from, MathF.Min(gone, 1f));

    /// <summary>1 at the start and a third (1/e) of what it was after every <paramref name="time"/>.</summary>
    private static float Decay(float t, float time) => MathF.Exp(-t / time);

    /// <summary>
    /// From 0 up to 1 and back down to 0 as <paramref name="gone"/> goes from 0 to 1, with no jump on the way: 1
    /// when <paramref name="loudestAt"/> has gone by.
    /// </summary>
    private static float Swell(float gone, float loudestAt)
    {
        float sine = MathF.Sin(MathF.PI * MathF.Pow(gone, MathF.Log(0.5f) / MathF.Log(loudestAt)));
        return sine * sine;
    }
}
