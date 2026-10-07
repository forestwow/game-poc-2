using Understudies.Core.Hashing;

namespace Understudies.Core.Tests.Hashing;

public class Fnv1a64Tests
{
    [Test]
    public void AddByte_LowercaseA_MatchesTheFnv1aReferenceValue()
    {
        var hasher = new Fnv1a64();

        hasher.AddByte(0x61);

        Assert.That(hasher.Value, Is.EqualTo(0xAF63DC4C8601EC8CUL));
    }

    [Test]
    public void AddInt_IsItsFourBytesLowestFirst()
    {
        var fromInt = new Fnv1a64();
        var fromBytes = new Fnv1a64();

        fromInt.AddInt(0x04030201);
        foreach (byte b in new byte[] { 1, 2, 3, 4 })
        {
            fromBytes.AddByte(b);
        }

        Assert.That(fromInt.Value, Is.EqualTo(fromBytes.Value));
    }

    [Test]
    public void AddULong_IsItsEightBytesLowestFirst()
    {
        var fromULong = new Fnv1a64();
        var fromBytes = new Fnv1a64();

        fromULong.AddULong(0x0807060504030201UL);
        foreach (byte b in new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })
        {
            fromBytes.AddByte(b);
        }

        Assert.That(fromULong.Value, Is.EqualTo(fromBytes.Value));
    }

    [Test]
    public void AddFloat_IsItsBitsAndNotItsText()
    {
        // 1 is 0x3F800000 in IEEE 754. Zero and minus zero are equal as numbers and are different bits.
        var one = new Fnv1a64();
        var bits = new Fnv1a64();
        var zero = new Fnv1a64();
        var minusZero = new Fnv1a64();

        one.AddFloat(1f);
        bits.AddInt(0x3F800000);
        zero.AddFloat(0f);
        minusZero.AddFloat(-0f);

        Assert.That(one.Value, Is.EqualTo(bits.Value));
        Assert.That(minusZero.Value, Is.Not.EqualTo(zero.Value));
    }
}
