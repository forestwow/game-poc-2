using Understudies.Core.Randomness;

namespace Understudies.Core.Tests.Randomness;

public class RngTests
{
    [Test]
    public void NextULong_SeedZero_MatchesTheSplitMix64ReferenceSequence()
    {
        var rng = new Rng(0UL);

        Assert.That(rng.NextULong(), Is.EqualTo(0xE220A8397B1DCDAFUL));
        Assert.That(rng.NextULong(), Is.EqualTo(0x6E789E6AA1B965F4UL));
        Assert.That(rng.NextULong(), Is.EqualTo(0x06C45D188009454FUL));
    }

    [Test]
    public void ForStream_TwoStreamsOfOneSeed_GiveNumbersOfTheirOwn_AndTheSameStreamTheSame()
    {
        ulong Waves(ulong seed) => Rng.ForStream(seed, RngStream.Waves).NextULong();

        ulong first = Waves(7);
        Assert.That(Waves(7), Is.EqualTo(first));
        Assert.That(Waves(8), Is.Not.EqualTo(first));
        Assert.That(Rng.ForStream(7, RngStream.DoorPlaces).NextULong(), Is.Not.EqualTo(first));
        Assert.That(new Rng(7).NextULong(), Is.Not.EqualTo(first));
    }

    [Test]
    public void RngStream_TheNumbersOfTheStreams_AreNeverGivenAnew()
    {
        // A stream's number decides its numbers: renumbering one changes every show ever seeded.
        Assert.That((ulong)RngStream.Waves, Is.EqualTo(1UL));
        Assert.That((ulong)RngStream.DoorPlaces, Is.EqualTo(2UL));
        Assert.That((ulong)RngStream.Program, Is.EqualTo(3UL));
        Assert.That((ulong)RngStream.Encore, Is.EqualTo(4UL));
    }

    [Test]
    public void NextULong_SameSeed_SameSequence()
    {
        var a = new Rng(12345);
        var b = new Rng(12345);

        for (int i = 0; i < 1000; i++)
        {
            Assert.That(a.NextULong(), Is.EqualTo(b.NextULong()));
        }
    }

    [TestCase(1)]
    [TestCase(6)]
    [TestCase(int.MaxValue)]
    public void NextInt_ReturnsAValueBelowTheBound(int maxExclusive)
    {
        var rng = new Rng(7);

        for (int i = 0; i < 10_000; i++)
        {
            Assert.That(rng.NextInt(maxExclusive), Is.InRange(0, maxExclusive - 1));
        }
    }

    [Test]
    public void NextInt_Six_IsRoughlyUniform()
    {
        var rng = new Rng(99);
        var counts = new int[6];

        for (int i = 0; i < 60_000; i++)
        {
            counts[rng.NextInt(6)]++;
        }

        Assert.That(counts, Has.All.InRange(9_500, 10_500));
    }

    [TestCase(0)]
    [TestCase(-5)]
    public void NextInt_NonPositiveBound_Throws(int maxExclusive)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Rng(1).NextInt(maxExclusive));
    }

    [Test]
    public void NextFloat_SeedZero_IsTheTop24BitsOfTheReferenceSequence()
    {
        var rng = new Rng(0UL);

        // 0xE220A8397B1DCDAF and 0x6E789E6AA1B965F4, as above.
        Assert.That(rng.NextFloat(), Is.EqualTo(0xE220A8 / 16777216f));
        Assert.That(rng.NextFloat(), Is.EqualTo(0x6E789E / 16777216f));
    }

    [Test]
    public void NextFloat_ReturnsAValueFromZeroToBelowOne()
    {
        var rng = new Rng(7);

        for (int i = 0; i < 10_000; i++)
        {
            Assert.That(rng.NextFloat(), Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
        }
    }
}
