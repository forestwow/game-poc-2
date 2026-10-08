namespace Understudies.Core.Randomness;

/// <summary>
/// What a show draws for, each with a generator of its own. A number is a stream's for good: it is never given to
/// another, and a stream that is no longer drawn from keeps its line here.
/// </summary>
public enum RngStream : ulong
{
    /// <summary>The plan of the waves: what each act buys, and when and by which door each of them enters.</summary>
    Waves = 1,

    /// <summary>Where along its door each critic enters.</summary>
    DoorPlaces = 2,

    /// <summary>
    /// Which cards each program offered, while a program offered by its act's applause: nothing draws from it now.
    /// </summary>
    Program = 3,

    /// <summary>Which cards each encore offers.</summary>
    Encore = 4,
}

/// <summary>
/// Deterministic generator (SplitMix64: Steele, Lea, Flood 2014). The same seed gives the same sequence on every
/// platform and runtime, which <see cref="System.Random"/> does not promise.
/// </summary>
public sealed class Rng(ulong seed)
{
    private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;

    /// <summary>All the generator remembers: two with the same state give the same numbers from here on.</summary>
    internal ulong State { get; private set; } = seed;

    /// <summary>
    /// The generator of one <paramref name="stream"/> of a show: each stream of one seed gives numbers of its own,
    /// so a change to how one thing draws never changes what another is given.
    /// </summary>
    public static Rng ForStream(ulong seed, RngStream stream) => new(Mix(seed ^ Mix((ulong)stream)));

    public ulong NextULong()
    {
        ulong value = Mix(State);
        State = unchecked(State + GoldenGamma);
        return value;
    }

    /// <summary>The generator's output for a state: the golden gamma added, then the bits scrambled.</summary>
    private static ulong Mix(ulong state)
    {
        unchecked
        {
            ulong z = state + GoldenGamma;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }

    /// <summary>Uniform in [0, 1): the top 24 bits, which is every bit a float keeps, so the division is exact.</summary>
    public float NextFloat() => (NextULong() >> 40) / 16777216f;

    /// <summary>Uniform integer in [0, maxExclusive), without modulo bias.</summary>
    public int NextInt(int maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxExclusive);

        ulong bound = (ulong)maxExclusive;
        // Values below the threshold would make some results more likely; reject them.
        ulong threshold = unchecked(0UL - bound) % bound;
        while (true)
        {
            ulong value = NextULong();
            if (value >= threshold)
            {
                return (int)(value % bound);
            }
        }
    }
}
