namespace Understudies.Core.Randomness;

/// <summary>
/// Deterministic generator (SplitMix64: Steele, Lea, Flood 2014). The same seed gives the same sequence on every
/// platform and runtime, which <see cref="System.Random"/> does not promise.
/// </summary>
public sealed class Rng(ulong seed)
{
    private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;

    /// <summary>All the generator remembers: two with the same state give the same numbers from here on.</summary>
    internal ulong State { get; private set; } = seed;

    public ulong NextULong()
    {
        unchecked
        {
            ulong z = State += GoldenGamma;
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
