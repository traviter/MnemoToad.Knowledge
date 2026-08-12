using Microsoft.EntityFrameworkCore;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.Repositories;
using MnemoToad.Knowledge.Tests.TestSupport;
using NUnit.Framework;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Tests.Repositories;

[TestFixture]
public class KnowledgeNodeRepositoryTests
{
    private MockableAppDbContext _db = null!;
    private KnowledgeNodeRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new MockableAppDbContext();
        _repository = new KnowledgeNodeRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private static JsonObject MediaStanza(Guid mediaAssetId, string altText) =>
        new() { ["id"] = mediaAssetId.ToString(), ["alt_text"] = altText };

    [Test]
    public async Task GetAllAsync_ReturnsKnowledgeNodesOrderedByCanonicalName()
    {
        var nodeTypeId = Guid.NewGuid();
        await _db.KnowledgeNode.AddRangeAsync(
            new KnowledgeNode { Id = Guid.NewGuid(), NodeTypeId = nodeTypeId, CanonicalName = "Venus" },
            new KnowledgeNode { Id = Guid.NewGuid(), NodeTypeId = nodeTypeId, CanonicalName = "Mercury" });
        await _db.SaveChangesAsync();

        var all = await _repository.GetAllAsync(nodeTypeId);

        Assert.That(all.Select(n => n.CanonicalName), Is.EqualTo(new[] { "Mercury", "Venus" }));
    }

    [Test]
    public async Task GetAllAsync_WithNodeTypeIdFilter_ReturnsOnlyMatchingNodes()
    {
        var nodeTypeId = Guid.NewGuid();
        await _db.KnowledgeNode.AddRangeAsync(
            new KnowledgeNode { Id = Guid.NewGuid(), NodeTypeId = nodeTypeId, CanonicalName = "Mercury" },
            new KnowledgeNode { Id = Guid.NewGuid(), NodeTypeId = Guid.NewGuid(), CanonicalName = "Venus" });
        await _db.SaveChangesAsync();

        var all = await _repository.GetAllAsync(nodeTypeId);

        Assert.That(all.Select(n => n.CanonicalName), Is.EqualTo(new[] { "Mercury" }));
    }

    [Test]
    public async Task GetByIdAsync_WhenExists_ReturnsKnowledgeNode()
    {
        var knowledgeNode = new KnowledgeNode { Id = Guid.NewGuid(), CanonicalName = "Mercury" };
        await _db.KnowledgeNode.AddAsync(knowledgeNode);
        await _db.SaveChangesAsync();

        var found = await _repository.GetByIdAsync(knowledgeNode.Id);

        Assert.That(found?.CanonicalName, Is.EqualTo("Mercury"));
        Assert.That(found!.Attributes, Is.Empty);
        Assert.That(found.Media, Is.Empty);
    }

    [Test]
    public async Task GetByIdAsync_WhenExists_PopulatesAttributesKeyedByKey()
    {
        var knowledgeNode = new KnowledgeNode { Id = Guid.NewGuid(), CanonicalName = "France" };
        await _db.KnowledgeNode.AddAsync(knowledgeNode);
        await _db.KnowledgeNodeAttribute.AddAsync(new KnowledgeNodeAttribute
        {
            KnowledgeNodeId = knowledgeNode.Id,
            Key = "isoCode",
            Value = JsonValue.Create("FR")
        });
        await _db.SaveChangesAsync();

        var found = await _repository.GetByIdAsync(knowledgeNode.Id);

        Assert.That(found!.Attributes!.Keys, Is.EquivalentTo(new[] { "isoCode" }));
        Assert.That(found.Attributes!["isoCode"]!.GetValue<string>(), Is.EqualTo("FR"));
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
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        var found = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.That(found, Is.Null);
    }

    [Test]
    public async Task CreateAsync_PersistsAndReturnsKnowledgeNode()
    {
        var knowledgeNode = new KnowledgeNode { Id = Guid.NewGuid(), CanonicalName = "Mercury" };

        var created = await _repository.CreateAsync(knowledgeNode);

        Assert.That(created, Is.SameAs(knowledgeNode));
        Assert.That(await _db.KnowledgeNode.FindAsync(knowledgeNode.Id), Is.Not.Null);
    }

    [Test]
    public async Task CreateAsync_WithAttributes_CreatesRows()
    {
        var knowledgeNode = new KnowledgeNode
        {
            Id = Guid.NewGuid(),
            CanonicalName = "France",
            Attributes = new Dictionary<string, JsonValue?> { ["isoCode"] = JsonValue.Create("FR") }
        };

        await _repository.CreateAsync(knowledgeNode);

        var row = await _db.KnowledgeNodeAttribute.AsNoTracking().SingleAsync(a => a.KnowledgeNodeId == knowledgeNode.Id);
        Assert.That(row.Key, Is.EqualTo("isoCode"));
        Assert.That(row.Value!.GetValue<string>(), Is.EqualTo("FR"));
    }

    [Test]
    public async Task CreateAsync_WithMedia_CreatesRowAndStoresStanzaVerbatim()
    {
        var mediaAssetId = Guid.NewGuid();
        var knowledgeNode = new KnowledgeNode
        {
            Id = Guid.NewGuid(),
            CanonicalName = "France",
            Media = new Dictionary<string, JsonObject?> { ["flag"] = MediaStanza(mediaAssetId, "Flag of France") }
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
            Media = new Dictionary<string, JsonObject?> { ["flag"] = stanza }
        };

        var created = await _repository.CreateAsync(knowledgeNode);

        Assert.That(created.Media!["flag"]!["other_metadata"]!.GetValue<int>(), Is.EqualTo(2323));
    }

    [Test]
    public void CreateAsync_WithMediaMissingId_ThrowsValidationException()
    {
        var knowledgeNode = new KnowledgeNode
        {
            Id = Guid.NewGuid(),
            CanonicalName = "Mercury",
            Media = new Dictionary<string, JsonObject?> { ["flag"] = new JsonObject { ["alt_text"] = "x" } }
        };

        var ex = Assert.ThrowsAsync<ValidationException>(() => _repository.CreateAsync(knowledgeNode));

        Assert.That(ex!.Message, Is.EqualTo("The media entry 'flag' must include a valid 'id'."));
    }

    [Test]
    public void CreateAsync_WithMediaNonGuidId_ThrowsValidationException()
    {
        var knowledgeNode = new KnowledgeNode
        {
            Id = Guid.NewGuid(),
            CanonicalName = "Mercury",
            Media = new Dictionary<string, JsonObject?> { ["flag"] = new JsonObject { ["id"] = "not-a-guid", ["alt_text"] = "x" } }
        };

        var ex = Assert.ThrowsAsync<ValidationException>(() => _repository.CreateAsync(knowledgeNode));

        Assert.That(ex!.Message, Is.EqualTo("The media entry 'flag' must include a valid 'id'."));
    }

    [Test]
    public void CreateAsync_WithMediaMissingAltText_ThrowsValidationException()
    {
        var knowledgeNode = new KnowledgeNode
        {
            Id = Guid.NewGuid(),
            CanonicalName = "Mercury",
            Media = new Dictionary<string, JsonObject?> { ["flag"] = new JsonObject { ["id"] = Guid.NewGuid().ToString() } }
        };

        var ex = Assert.ThrowsAsync<ValidationException>(() => _repository.CreateAsync(knowledgeNode));

        Assert.That(ex!.Message, Is.EqualTo("The media entry 'flag' must include a valid 'alt_text'."));
    }

    [Test]
    public void CreateAsync_WithMediaNonStringAltText_ThrowsValidationException()
    {
        var knowledgeNode = new KnowledgeNode
        {
            Id = Guid.NewGuid(),
            CanonicalName = "Mercury",
            Media = new Dictionary<string, JsonObject?> { ["flag"] = new JsonObject { ["id"] = Guid.NewGuid().ToString(), ["alt_text"] = 123 } }
        };

        var ex = Assert.ThrowsAsync<ValidationException>(() => _repository.CreateAsync(knowledgeNode));

        Assert.That(ex!.Message, Is.EqualTo("The media entry 'flag' must include a valid 'alt_text'."));
    }

    [Test]
    public async Task UpdateAsync_WhenNotFound_ReturnsNull()
    {
        var updated = await _repository.UpdateAsync(new KnowledgeNode { Id = Guid.NewGuid(), CanonicalName = "Mercury" });

        Assert.That(updated, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_WithValidData_UpdatesAndReturnsKnowledgeNode()
    {
        var knowledgeNode = new KnowledgeNode { Id = Guid.NewGuid(), CanonicalName = "Mercury", Description = "Old" };
        await _db.KnowledgeNode.AddAsync(knowledgeNode);
        await _db.SaveChangesAsync();

        var updated = await _repository.UpdateAsync(
            new KnowledgeNode { Id = knowledgeNode.Id, CanonicalName = "Mercury", Description = "New description" });

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Description, Is.EqualTo("New description"));
    }

    [Test]
    public async Task UpdateAsync_FullReplace_RemovesOmittedUpdatesChangedAndAddsNew()
    {
        var nodeTypeId = Guid.NewGuid();
        var knowledgeNode = new KnowledgeNode { Id = Guid.NewGuid(), NodeTypeId = nodeTypeId, CanonicalName = "France" };
        await _db.KnowledgeNode.AddAsync(knowledgeNode);

        await _db.KnowledgeNodeAttribute.AddRangeAsync(
            new KnowledgeNodeAttribute { KnowledgeNodeId = knowledgeNode.Id, Key = "isoCode", Value = JsonValue.Create("FR") },
            new KnowledgeNodeAttribute { KnowledgeNodeId = knowledgeNode.Id, Key = "population", Value = JsonValue.Create(1000) });
        await _db.SaveChangesAsync();

        var updated = await _repository.UpdateAsync(new KnowledgeNode
        {
            Id = knowledgeNode.Id,
            NodeTypeId = nodeTypeId,
            CanonicalName = "France",
            Attributes = new Dictionary<string, JsonValue?>
            {
                ["isoCode"] = JsonValue.Create("FR-NEW"),
                ["isEuMember"] = JsonValue.Create(true)
            }
        });

        Assert.That(updated, Is.Not.Null);
        var rows = await _db.KnowledgeNodeAttribute.AsNoTracking().Where(a => a.KnowledgeNodeId == knowledgeNode.Id).ToListAsync();
        Assert.That(rows.Select(r => r.Key), Is.EquivalentTo(new[] { "isoCode", "isEuMember" }));

        var isoRow = rows.Single(r => r.Key == "isoCode");
        Assert.That(isoRow.Value!.GetValue<string>(), Is.EqualTo("FR-NEW"));
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
            Media = new Dictionary<string, JsonObject?>
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

    [Test]
    public async Task DeleteAsync_WhenExists_RemovesKnowledgeNodeAndReturnsTrue()
    {
        var knowledgeNode = new KnowledgeNode { Id = Guid.NewGuid(), CanonicalName = "Mercury" };
        await _db.KnowledgeNode.AddAsync(knowledgeNode);
        await _db.SaveChangesAsync();

        var result = await _repository.DeleteAsync(knowledgeNode.Id);

        Assert.That(result, Is.True);
        Assert.That(await _db.KnowledgeNode.AsNoTracking().FirstOrDefaultAsync(n => n.Id == knowledgeNode.Id), Is.Null);
    }

    [Test]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        var result = await _repository.DeleteAsync(Guid.NewGuid());

        Assert.That(result, Is.False);
    }

    [Test]
    public void CreateAsync_OnUniqueViolation_ThrowsValidationExceptionWithDuplicateMessage()
    {
        _db.ThrowOnSaveChanges(PostgresExceptionFactory.UniqueViolation(constraintName: "uq_knowledge_node_node_type_id_canonical_name"));

        var ex = Assert.ThrowsAsync<ValidationException>(
            () => _repository.CreateAsync(new KnowledgeNode { Id = Guid.NewGuid(), CanonicalName = "Mercury" }));

        Assert.That(ex!.Message, Is.EqualTo("A KnowledgeNode with the same NodeType and CanonicalName already exists."));
    }

    [Test]
    public void CreateAsync_OnKnowledgeNodeAttributePrimaryKeyViolation_ThrowsValidationExceptionAboutDuplicateAttribute()
    {
        _db.ThrowOnSaveChanges(PostgresExceptionFactory.UniqueViolation(constraintName: "pk_knowledge_node_attribute"));

        var ex = Assert.ThrowsAsync<ValidationException>(() => _repository.CreateAsync(new KnowledgeNode
        {
            Id = Guid.NewGuid(),
            CanonicalName = "Mercury",
            Attributes = new Dictionary<string, JsonValue?> { ["isoCode"] = JsonValue.Create("FR") }
        }));

        Assert.That(ex!.Message, Is.EqualTo("An attribute with that key already exists for this KnowledgeNode."));
    }

    [Test]
    public void CreateAsync_OnKnowledgeNodeMediaPrimaryKeyViolation_ThrowsValidationExceptionAboutDuplicateMedia()
    {
        _db.ThrowOnSaveChanges(PostgresExceptionFactory.UniqueViolation(constraintName: "pk_knowledge_node_media"));

        var ex = Assert.ThrowsAsync<ValidationException>(() => _repository.CreateAsync(new KnowledgeNode
        {
            Id = Guid.NewGuid(),
            CanonicalName = "Mercury",
            Media = new Dictionary<string, JsonObject?> { ["flag"] = MediaStanza(Guid.NewGuid(), "x") }
        }));

        Assert.That(ex!.Message, Is.EqualTo("A media entry with that key already exists for this KnowledgeNode."));
    }

    [Test]
    public void CreateAsync_OnMediaAssetUniqueViolation_ThrowsValidationExceptionAboutAlreadyLinked()
    {
        _db.ThrowOnSaveChanges(PostgresExceptionFactory.UniqueViolation(constraintName: "uq_knowledge_node_media_media_asset_id"));

        var ex = Assert.ThrowsAsync<ValidationException>(() => _repository.CreateAsync(new KnowledgeNode
        {
            Id = Guid.NewGuid(),
            CanonicalName = "Mercury",
            Media = new Dictionary<string, JsonObject?> { ["flag"] = MediaStanza(Guid.NewGuid(), "x") }
        }));

        Assert.That(ex!.Message, Is.EqualTo("The specified MediaAsset is already linked to another KnowledgeNode."));
    }

    [Test]
    public void CreateAsync_OnForeignKeyViolationForKnowledgeNodeTable_ThrowsValidationExceptionAboutNodeType()
    {
        _db.ThrowOnSaveChanges(PostgresExceptionFactory.ForeignKeyViolation(tableName: "knowledge_node"));

        var ex = Assert.ThrowsAsync<ValidationException>(
            () => _repository.CreateAsync(new KnowledgeNode { Id = Guid.NewGuid(), CanonicalName = "Mercury" }));

        Assert.That(ex!.Message, Is.EqualTo("The specified NodeType does not exist."));
    }

    [Test]
    public async Task DeleteAsync_OnForeignKeyViolationForKnowledgeRelationTable_ThrowsValidationExceptionAboutReferences()
    {
        var knowledgeNode = new KnowledgeNode { Id = Guid.NewGuid(), CanonicalName = "Mercury" };
        await _db.KnowledgeNode.AddAsync(knowledgeNode);
        await _db.SaveChangesAsync();
        _db.ThrowOnExecuteDelete<KnowledgeNode>(PostgresExceptionFactory.ForeignKeyViolation(tableName: "knowledge_relation"));

        var ex = Assert.ThrowsAsync<ValidationException>(() => _repository.DeleteAsync(knowledgeNode.Id));

        Assert.That(ex!.Message, Is.EqualTo("The KnowledgeNode cannot be deleted because it is referenced by one or more KnowledgeRelations."));
    }

    [Test]
    public void CreateAsync_OnAttributeKeyCheckViolation_ThrowsValidationExceptionAboutLettersOnly()
    {
        _db.ThrowOnSaveChanges(PostgresExceptionFactory.CheckViolation(constraintName: "ck_knowledge_node_attribute_key"));

        var ex = Assert.ThrowsAsync<ValidationException>(() => _repository.CreateAsync(new KnowledgeNode
        {
            Id = Guid.NewGuid(),
            CanonicalName = "Mercury",
            Attributes = new Dictionary<string, JsonValue?> { ["iso-code"] = JsonValue.Create("FR") }
        }));

        Assert.That(ex!.Message, Is.EqualTo("An attribute key must contain only letters."));
    }

    [Test]
    public void CreateAsync_OnMediaKeyCheckViolation_ThrowsValidationExceptionAboutLettersOnly()
    {
        _db.ThrowOnSaveChanges(PostgresExceptionFactory.CheckViolation(constraintName: "ck_knowledge_node_media_key"));

        var ex = Assert.ThrowsAsync<ValidationException>(() => _repository.CreateAsync(new KnowledgeNode
        {
            Id = Guid.NewGuid(),
            CanonicalName = "Mercury",
            Media = new Dictionary<string, JsonObject?> { ["flag2"] = MediaStanza(Guid.NewGuid(), "x") }
        }));

        Assert.That(ex!.Message, Is.EqualTo("A media key must contain only letters."));
    }
}
