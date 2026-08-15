using Microsoft.EntityFrameworkCore;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.EntityMappers;
using MnemoToad.Knowledge.Data.Repositories;
using MnemoToad.Knowledge.Tests.TestSupport;
using NUnit.Framework;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Tests.Components;

[TestFixture]
public class KnowledgeNodeRepositoryTests
{
    private MockableAppDbContext _db = null!;
    private KnowledgeNodeRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new MockableAppDbContext();
        _repository = new KnowledgeNodeRepository(_db, new KnowledgeNodeMediaJsonMapper());
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private static JsonObject MediaStanza(Guid mediaAssetId, string altText) =>
        new() { ["id"] = mediaAssetId.ToString(), ["alt_text"] = altText };

    [Test]
    public async Task CreateAsync_WithMedia_CreatesRowAndStoresStanzaVerbatim()
    {
        var mediaAssetId = Guid.NewGuid();
        var knowledgeNode = new KnowledgeNode
        {
            Id = Guid.NewGuid(),
            CanonicalName = "France",
            Media = new Dictionary<string, JsonObject> { ["flag"] = MediaStanza(mediaAssetId, "Flag of France") }
        };

        var created = await _repository.CreateAsync(knowledgeNode);

        var row = await _db.KnowledgeNodeMedia.AsNoTracking().SingleAsync(m => m.KnowledgeNodeId == knowledgeNode.Id);
        Assert.That(row.Key, Is.EqualTo("flag"));
        Assert.That(row.MediaAssetId, Is.EqualTo(mediaAssetId));
        Assert.That(row.AltText, Is.EqualTo("Flag of France"));
        Assert.That(created.Media!["flag"]!["id"]!.GetValue<string>(), Is.EqualTo(mediaAssetId.ToString()));
    }

    [Test]
    public async Task CreateAsync_WithMediaExtraFields_StoresEntireStanzaVerbatim()
    {
        var mediaAssetId = Guid.NewGuid();
        var stanza = MediaStanza(mediaAssetId, "Flag of France");
        stanza["other_metadata"] = 2323;
        var knowledgeNode = new KnowledgeNode
        {
            Id = Guid.NewGuid(),
            CanonicalName = "France",
            Media = new Dictionary<string, JsonObject> { ["flag"] = stanza }
        };

        var created = await _repository.CreateAsync(knowledgeNode);

        Assert.That(created.Media!["flag"]!["other_metadata"]!.GetValue<int>(), Is.EqualTo(2323));
    }

    [Test]
    public void CreateAsync_WithMediaMissingId_ThrowsValidationExceptionWithKeyInMessage()
    {
        var knowledgeNode = new KnowledgeNode
        {
            Id = Guid.NewGuid(),
            CanonicalName = "Mercury",
            Media = new Dictionary<string, JsonObject> { ["flag"] = new JsonObject { ["alt_text"] = "x" } }
        };

        var ex = Assert.ThrowsAsync<ValidationException>(() => _repository.CreateAsync(knowledgeNode));

        Assert.That(ex!.Message, Is.EqualTo("The media entry 'flag' must include a valid 'id'."));
    }

    [Test]
    public void CreateAsync_WithMediaMissingAltText_ThrowsValidationExceptionWithKeyInMessage()
    {
        var knowledgeNode = new KnowledgeNode
        {
            Id = Guid.NewGuid(),
            CanonicalName = "Mercury",
            Media = new Dictionary<string, JsonObject> { ["flag"] = new JsonObject { ["id"] = Guid.NewGuid().ToString() } }
        };

        var ex = Assert.ThrowsAsync<ValidationException>(() => _repository.CreateAsync(knowledgeNode));

        Assert.That(ex!.Message, Is.EqualTo("The media entry 'flag' must include a valid 'alt_text'."));
    }

    [Test]
    public async Task GetByIdAsync_WhenExists_PopulatesMediaWithStoredStanzaKeyedByKey()
    {
        var knowledgeNode = new KnowledgeNode { Id = Guid.NewGuid(), CanonicalName = "France" };
        var mediaAssetId = Guid.NewGuid();
        await _db.KnowledgeNode.AddAsync(knowledgeNode);
        await _db.KnowledgeNodeMedia.AddAsync(new KnowledgeNodeMedia
        {
            KnowledgeNodeId = knowledgeNode.Id,
            Key = "flag",
            MediaAssetId = mediaAssetId,
            AltText = "Flag of France",
            Metadata = MediaStanza(mediaAssetId, "Flag of France")
        });
        await _db.SaveChangesAsync();

        var found = await _repository.GetByIdAsync(knowledgeNode.Id);

        Assert.That(found!.Media!.Keys, Is.EquivalentTo(new[] { "flag" }));
        Assert.That(found.Media!["flag"]!["id"]!.GetValue<string>(), Is.EqualTo(mediaAssetId.ToString()));
        Assert.That(found.Media!["flag"]!["alt_text"]!.GetValue<string>(), Is.EqualTo("Flag of France"));
    }

    [Test]
    public async Task UpdateAsync_MediaFullReplace_RemovesOmittedUpdatesChangedAndAddsNew()
    {
        var nodeTypeId = Guid.NewGuid();
        var knowledgeNode = new KnowledgeNode { Id = Guid.NewGuid(), NodeTypeId = nodeTypeId, CanonicalName = "France" };
        var flagAssetId = Guid.NewGuid();
        var newFlagAssetId = Guid.NewGuid();
        var photoAssetId = Guid.NewGuid();
        await _db.KnowledgeNode.AddAsync(knowledgeNode);
        await _db.KnowledgeNodeMedia.AddRangeAsync(
            new KnowledgeNodeMedia { KnowledgeNodeId = knowledgeNode.Id, Key = "flag", MediaAssetId = flagAssetId, AltText = "Old flag", Metadata = MediaStanza(flagAssetId, "Old flag") },
            new KnowledgeNodeMedia { KnowledgeNodeId = knowledgeNode.Id, Key = "pronunciation", MediaAssetId = photoAssetId, AltText = "Pronunciation", Metadata = MediaStanza(photoAssetId, "Pronunciation") });
        await _db.SaveChangesAsync();

        var updated = await _repository.UpdateAsync(new KnowledgeNode
        {
            Id = knowledgeNode.Id,
            NodeTypeId = nodeTypeId,
            CanonicalName = "France",
            Media = new Dictionary<string, JsonObject>
            {
                ["flag"] = MediaStanza(newFlagAssetId, "New flag"),
                ["photo"] = MediaStanza(photoAssetId, "Eiffel Tower")
            }
        });

        Assert.That(updated, Is.Not.Null);
        var rows = await _db.KnowledgeNodeMedia.AsNoTracking().Where(m => m.KnowledgeNodeId == knowledgeNode.Id).ToListAsync();
        Assert.That(rows.Select(r => r.Key), Is.EquivalentTo(new[] { "flag", "photo" }));

        var flagRow = rows.Single(r => r.Key == "flag");
        Assert.That(flagRow.MediaAssetId, Is.EqualTo(newFlagAssetId));
        Assert.That(flagRow.AltText, Is.EqualTo("New flag"));
        Assert.That(updated!.Media!["flag"]!["id"]!.GetValue<string>(), Is.EqualTo(newFlagAssetId.ToString()));
    }
}
