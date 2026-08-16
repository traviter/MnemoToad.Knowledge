using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.EntityMappers;
using NUnit.Framework;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Tests.EntityMappers;

[TestFixture]
public class KnowledgeNodeMediaJsonMapperTests
{
    private KnowledgeNodeMediaJsonMapper _mapper = null!;

    [SetUp]
    public void SetUp() => _mapper = new KnowledgeNodeMediaJsonMapper();

    [Test]
    public void ToJson_NoMetadata_ReturnsIdAndAltTextOnly()
    {
        var mediaAssetId = Guid.NewGuid();
        var media = new KnowledgeNodeMedia { KnowledgeNodeId = Guid.NewGuid(), Key = "flag", MediaAssetId = mediaAssetId, AltText = "A flag", Metadata = null };

        var json = _mapper.ToJson(media);

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

        var json = _mapper.ToJson(media);

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

        var json = _mapper.ToJson(media);

        Assert.That(json["id"]!.GetValue<string>(), Is.EqualTo(canonicalMediaAssetId.ToString()));
        Assert.That(json["alt_text"]!.GetValue<string>(), Is.EqualTo("Current text"));
    }

    [Test]
    public void ToJson_DoesNotMutateOriginalMetadata()
    {
        var originalMediaAssetId = Guid.NewGuid();
        var metadata = new JsonObject { ["id"] = originalMediaAssetId.ToString(), ["alt_text"] = "Original" };
        var media = new KnowledgeNodeMedia { KnowledgeNodeId = Guid.NewGuid(), Key = "flag", MediaAssetId = Guid.NewGuid(), AltText = "Overridden", Metadata = metadata };

        _mapper.ToJson(media);

        Assert.That(metadata["id"]!.GetValue<string>(), Is.EqualTo(originalMediaAssetId.ToString()));
        Assert.That(metadata["alt_text"]!.GetValue<string>(), Is.EqualTo("Original"));
    }

    [Test]
    public void UpdateFromJson_ValidStanza_UpdatesFieldsInPlace()
    {
        var knowledgeNodeId = Guid.NewGuid();
        var originalMediaAssetId = Guid.NewGuid();
        var media = new KnowledgeNodeMedia
        {
            KnowledgeNodeId = knowledgeNodeId,
            Key = "flag",
            MediaAssetId = originalMediaAssetId,
            AltText = "Old text",
            Metadata = new JsonObject { ["id"] = originalMediaAssetId.ToString(), ["alt_text"] = "Old text" }
        };
        var newMediaAssetId = Guid.NewGuid();
        var newStanza = new JsonObject { ["id"] = newMediaAssetId.ToString(), ["alt_text"] = "New text", ["credit"] = "Someone" };

        _mapper.UpdateFromJson(newStanza, media);

        Assert.That(media.KnowledgeNodeId, Is.EqualTo(knowledgeNodeId));
        Assert.That(media.Key, Is.EqualTo("flag"));
        Assert.That(media.MediaAssetId, Is.EqualTo(newMediaAssetId));
        Assert.That(media.AltText, Is.EqualTo("New text"));
        Assert.That(media.Metadata, Is.SameAs(newStanza));
    }

    [Test]
    public void UpdateFromJson_MissingId_ThrowsValidationExceptionAndLeavesMediaUnchanged()
    {
        var originalMediaAssetId = Guid.NewGuid();
        var media = new KnowledgeNodeMedia
        {
            KnowledgeNodeId = Guid.NewGuid(),
            Key = "flag",
            MediaAssetId = originalMediaAssetId,
            AltText = "Old text",
            Metadata = new JsonObject { ["id"] = originalMediaAssetId.ToString(), ["alt_text"] = "Old text" }
        };
        var invalidStanza = new JsonObject { ["alt_text"] = "New text" };

        var ex = Assert.Throws<ValidationException>(() => _mapper.UpdateFromJson(invalidStanza, media));

        Assert.That(ex!.Message, Is.EqualTo("The media entry 'flag' must include a valid 'id'."));
        Assert.That(media.MediaAssetId, Is.EqualTo(originalMediaAssetId));
        Assert.That(media.AltText, Is.EqualTo("Old text"));
    }

    [Test]
    public void UpdateFromJson_InvalidIdFormat_ThrowsValidationException()
    {
        var media = new KnowledgeNodeMedia { KnowledgeNodeId = Guid.NewGuid(), Key = "flag" };
        var stanza = new JsonObject { ["id"] = "not-a-guid", ["alt_text"] = "A flag" };

        var ex = Assert.Throws<ValidationException>(() => _mapper.UpdateFromJson(stanza, media));
        Assert.That(ex!.Message, Is.EqualTo("The media entry 'flag' must include a valid 'id'."));
    }

    [Test]
    public void UpdateFromJson_MissingAltText_ThrowsValidationException()
    {
        var media = new KnowledgeNodeMedia { KnowledgeNodeId = Guid.NewGuid(), Key = "flag" };
        var stanza = new JsonObject { ["id"] = Guid.NewGuid().ToString() };

        var ex = Assert.Throws<ValidationException>(() => _mapper.UpdateFromJson(stanza, media));
        Assert.That(ex!.Message, Is.EqualTo("The media entry 'flag' must include a valid 'alt_text'."));
    }

    [Test]
    public void UpdateFromJson_NonStringAltText_ThrowsValidationException()
    {
        var media = new KnowledgeNodeMedia { KnowledgeNodeId = Guid.NewGuid(), Key = "flag" };
        var stanza = new JsonObject { ["id"] = Guid.NewGuid().ToString(), ["alt_text"] = 123 };

        var ex = Assert.Throws<ValidationException>(() => _mapper.UpdateFromJson(stanza, media));
        Assert.That(ex!.Message, Is.EqualTo("The media entry 'flag' must include a valid 'alt_text'."));
    }

    [Test]
    public void UpdateFromJson_NullStanza_ThrowsValidationException()
    {
        var media = new KnowledgeNodeMedia { KnowledgeNodeId = Guid.NewGuid(), Key = "flag" };
        JsonObject? stanza = null;

        var ex = Assert.Throws<ValidationException>(() => _mapper.UpdateFromJson(stanza!, media));
        Assert.That(ex!.Message, Is.EqualTo("The media entry 'flag' must include a valid 'id'."));
    }
}
