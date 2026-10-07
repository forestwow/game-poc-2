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
