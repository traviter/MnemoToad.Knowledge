using MnemoToad.Knowledge.Data.Entities;
using NUnit.Framework;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Tests.Entities;

[TestFixture]
public class KnowledgeNodeMediaExtensionsTests
{
    [Test]
    public void ToJson_NoMetadata_ReturnsIdAndAltTextOnly()
    {
        var mediaAssetId = Guid.NewGuid();
        var media = new KnowledgeNodeMedia { KnowledgeNodeId = Guid.NewGuid(), Key = "flag", MediaAssetId = mediaAssetId, AltText = "A flag", Metadata = null };

        var json = media.ToJson();

        Assert.That(json["id"]!.GetValue<string>(), Is.EqualTo(mediaAssetId.ToString()));
        Assert.That(json["alt_text"]!.GetValue<string>(), Is.EqualTo("A flag"));
        Assert.That(json.Count, Is.EqualTo(2));
    }

    [Test]
    public void ToJson_MetadataWithExtraFields_ReturnsAllFieldsFlat()
    {
        var mediaAssetId = Guid.NewGuid();
        var metadata = new JsonObject { ["id"] = mediaAssetId.ToString(), ["alt_text"] = "A coat of arms", ["credit"] = "Wikimedia Commons" };
        var media = new KnowledgeNodeMedia { KnowledgeNodeId = Guid.NewGuid(), Key = "coatOfArms", MediaAssetId = mediaAssetId, AltText = "A coat of arms", Metadata = metadata };

        var json = media.ToJson();

        Assert.That(json["id"]!.GetValue<string>(), Is.EqualTo(mediaAssetId.ToString()));
        Assert.That(json["alt_text"]!.GetValue<string>(), Is.EqualTo("A coat of arms"));
        Assert.That(json["credit"]!.GetValue<string>(), Is.EqualTo("Wikimedia Commons"));
    }

    [Test]
    public void ToJson_OverridesIdAndAltTextFromColumnsNotMetadata()
    {
        var canonicalMediaAssetId = Guid.NewGuid();
        var metadata = new JsonObject { ["id"] = Guid.NewGuid().ToString(), ["alt_text"] = "Stale text" };
        var media = new KnowledgeNodeMedia { KnowledgeNodeId = Guid.NewGuid(), Key = "flag", MediaAssetId = canonicalMediaAssetId, AltText = "Current text", Metadata = metadata };

        var json = media.ToJson();

        Assert.That(json["id"]!.GetValue<string>(), Is.EqualTo(canonicalMediaAssetId.ToString()));
        Assert.That(json["alt_text"]!.GetValue<string>(), Is.EqualTo("Current text"));
    }

    [Test]
    public void ToJson_DoesNotMutateOriginalMetadata()
    {
        var originalMediaAssetId = Guid.NewGuid();
        var metadata = new JsonObject { ["id"] = originalMediaAssetId.ToString(), ["alt_text"] = "Original" };
        var media = new KnowledgeNodeMedia { KnowledgeNodeId = Guid.NewGuid(), Key = "flag", MediaAssetId = Guid.NewGuid(), AltText = "Overridden", Metadata = metadata };

        media.ToJson();

        Assert.That(metadata["id"]!.GetValue<string>(), Is.EqualTo(originalMediaAssetId.ToString()));
        Assert.That(metadata["alt_text"]!.GetValue<string>(), Is.EqualTo("Original"));
    }
}
