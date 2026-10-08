using System.Text.Json;
using System.Text.Json.Nodes;

namespace Understudies.Core.Tests;

public class TuningTests
{
    [Test]
    public void Parse_TheCommittedFile_Parses()
    {
        Assert.That(CommittedTuning.Parse, Throws.Nothing);
    }

    [Test]
    public void Parse_AnyKeyMissing_IsRefusedWithItsName()
    {
        JsonNode file = JsonNode.Parse(CommittedTuning.Json)!;
        foreach (JsonObject braces in Objects(file).ToList())
        {
            foreach (string key in braces.Select(member => member.Key).ToList())
            {
                JsonNode? value = braces[key];
                braces.Remove(key);

                Assert.That(
                    () => Tuning.Parse(file.ToJsonString()),
                    Throws.TypeOf<JsonException>().With.Message.Contains($"'{key}'"));

                braces[key] = value;
            }
        }
    }

    [Test]
    public void Parse_AnUnknownKeyAnywhere_IsRefusedWithItsName()
    {
        JsonNode file = JsonNode.Parse(CommittedTuning.Json)!;
        foreach (JsonObject braces in Objects(file).ToList())
        {
            braces["notATuning"] = 1;

            Assert.That(
                () => Tuning.Parse(file.ToJsonString()),
                Throws.TypeOf<JsonException>().With.Message.Contains("'notATuning'"));

            braces.Remove("notATuning");
        }
    }

    [Test]
    public void Parse_AKeyWrittenTwice_IsRefusedWithItsName()
    {
        // The slip of copying a line to try another value: without the refusal the last one wins in silence.
        (string key, JsonNode? value) = JsonNode.Parse(CommittedTuning.Json)!.AsObject().First();
        string twice = CommittedTuning.Json.Insert(
            CommittedTuning.Json.IndexOf('{') + 1,
            $"\"{key}\": {value!.ToJsonString()},");

        Assert.That(
            () => Tuning.Parse(twice),
            Throws.TypeOf<JsonException>().With.Message.Contains($"'{key}'"));
    }

    [TestCase("[]")]
    [TestCase("null")]
    public void Parse_NoStageDoor_IsRefusedWithTheKeysName(string doors)
    {
        // Without a door nobody could enter.
        JsonNode file = JsonNode.Parse(CommittedTuning.Json)!;
        file["stageDoors"] = JsonNode.Parse(doors);

        Assert.That(
            () => Tuning.Parse(file.ToJsonString()),
            Throws.TypeOf<JsonException>().With.Message.Contains("'stageDoors'"));
    }

    [Test]
    public void Parse_ADoorAboveTheFloorsTop_IsRefusedWithTheKeysName()
    {
        // The floor starts three units down the stage, and the second door is half a unit up the back wall.
        JsonNode file = JsonNode.Parse(CommittedTuning.Json)!;
        file["stageFloorTop"] = 3;
        file["stageDoors"] = JsonNode.Parse(
            """[{ "position": { "x": 0, "y": 15 }, "opensInAct": 1 }, { "position": { "x": 17, "y": 2.5 }, "opensInAct": 3 }]""");

        Assert.That(
            () => Tuning.Parse(file.ToJsonString()),
            Throws.TypeOf<JsonException>().With.Message.Contains("'stageDoors'"));
    }

    [TestCase(1.4f)]
    [TestCase(46.6f)]
    public void Parse_ADoorThatReachesPastTheStagesSide_IsRefusedWithTheKeysName(float x)
    {
        // A door is three units wide and runs along the stage's width (plan T49): its middle is at least half of
        // that in from either side, or somebody would come in outside the stage.
        JsonNode file = JsonNode.Parse(CommittedTuning.Json)!;
        file["stageDoorWidth"] = 3;
        file["stageDoors"] = JsonNode.Parse(
            $$"""[{ "position": { "x": {{x.ToString(System.Globalization.CultureInfo.InvariantCulture)}}, "y": 15 }, "opensInAct": 1 }]""");

        Assert.That(
            () => Tuning.Parse(file.ToJsonString()),
            Throws.TypeOf<JsonException>().With.Message.Contains("'stageDoors'"));
    }

    [Test]
    public void Parse_ADoorWithNoActToOpenIn_IsRefusedWithTheKeysName()
    {
        JsonNode file = JsonNode.Parse(CommittedTuning.Json)!;
        file["stageDoors"] = JsonNode.Parse("""[{ "position": { "x": 0, "y": 15 } }]""");

        Assert.That(
            () => Tuning.Parse(file.ToJsonString()),
            Throws.TypeOf<JsonException>().With.Message.Contains("'opensInAct'"));
    }

    [Test]
    public void Parse_NoDoorOpenInTheFirstAct_IsRefusedWithTheKeysName()
    {
        // What the first act buys would have no way in.
        JsonNode file = JsonNode.Parse(CommittedTuning.Json)!;
        file["stageDoors"] = JsonNode.Parse("""[{ "position": { "x": 0, "y": 15 }, "opensInAct": 2 }]""");

        Assert.That(
            () => Tuning.Parse(file.ToJsonString()),
            Throws.TypeOf<JsonException>().With.Message.Contains("'stageDoors'"));
    }

    [TestCase("[]")]
    [TestCase("null")]
    public void Parse_NoKindOfEnemy_IsRefusedWithTheKeysName(string kinds)
    {
        JsonNode file = JsonNode.Parse(CommittedTuning.Json)!;
        file["enemyKinds"] = JsonNode.Parse(kinds);

        Assert.That(
            () => Tuning.Parse(file.ToJsonString()),
            Throws.TypeOf<JsonException>().With.Message.Contains("'enemyKinds'"));
    }

    [Test]
    public void Parse_AKindOfEnemyThatCostsNothing_IsRefusedWithTheKeysName()
    {
        // An act buys until it can afford nothing more: it would never stop buying this one.
        JsonNode file = JsonNode.Parse(CommittedTuning.Json)!;
        file["enemyKinds"]![0]!["cost"] = 0;

        Assert.That(
            () => Tuning.Parse(file.ToJsonString()),
            Throws.TypeOf<JsonException>().With.Message.Contains("'enemyKinds'"));
    }

    [Test]
    public void Parse_ANumberOfActsThatIsNotWhole_IsRefusedWithTheKeysName()
    {
        // The one number of the file that is counted and not measured: three acts are read as three.
        JsonNode file = JsonNode.Parse(CommittedTuning.Json)!;
        file["actsInPerformance"] = 3;
        Assert.That(Tuning.Parse(file.ToJsonString()).ActsInPerformance, Is.EqualTo(3));

        file["actsInPerformance"] = 10.5;

        Assert.That(
            () => Tuning.Parse(file.ToJsonString()),
            Throws.TypeOf<JsonException>().With.Message.Contains("actsInPerformance"));
    }

    [Test]
    public void Parse_AFileThatSaysNull_IsRefused()
    {
        Assert.That(() => Tuning.Parse("null"), Throws.TypeOf<JsonException>());
    }

    /// <summary>The file itself and everything written in braces inside it, so a point too.</summary>
    private static IEnumerable<JsonObject> Objects(JsonNode? node) => node switch
    {
        JsonObject braces => braces.SelectMany(member => Objects(member.Value)).Prepend(braces),
        JsonArray list => list.SelectMany(Objects),
        _ => [],
    };
}
