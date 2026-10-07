namespace Understudies.Core.Hashing;

/// <summary>
/// 64-bit FNV-1a hash (Fowler, Noll, Vo). The same bytes give the same value on every platform and runtime, which
/// <see cref="object.GetHashCode"/> does not promise.
/// </summary>
public sealed class Fnv1a64
{
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public ulong Value { get; private set; } = OffsetBasis;

    public void AddByte(byte value)
    {
        unchecked
        {
            Value = (Value ^ value) * Prime;
        }
    }

    /// <summary>The four bytes of <paramref name="value"/>, the lowest first.</summary>
    public void AddInt(int value)
    {
        for (int shift = 0; shift < 32; shift += 8)
        {
            AddByte(unchecked((byte)(value >> shift)));
        }
    }

    /// <summary>The eight bytes of <paramref name="value"/>, the lowest first.</summary>
    public void AddULong(ulong value)
    {
        for (int shift = 0; shift < 64; shift += 8)
        {
            AddByte(unchecked((byte)(value >> shift)));
        }
    }

    /// <summary>The bits of <paramref name="value"/>, never its text: two machines may print a float differently.</summary>
    public void AddFloat(float value) => AddInt(BitConverter.SingleToInt32Bits(value));
}
