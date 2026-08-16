using Microsoft.EntityFrameworkCore;
using Moq;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.Repositories;
using MnemoToad.Knowledge.Tests.TestSupport;
using NUnit.Framework;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using MnemoToad.Knowledge.Data.DbUtil;

namespace MnemoToad.Knowledge.Tests.Repositories;

[TestFixture]
public class KnowledgeNodeRepositoryTests
{
    private MockableAppDbContext _db = null!;
    private Mock<IEntityJsonMapper<KnowledgeNodeMedia>> _mediaMapper = null!;
    private KnowledgeNodeRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new MockableAppDbContext();
        _mediaMapper = new Mock<IEntityJsonMapper<KnowledgeNodeMedia>>();
        _mediaMapper.Setup(m => m.ToJson(It.IsAny<KnowledgeNodeMedia>())).Returns(new JsonObject());
        _repository = new KnowledgeNodeRepository(_db, _mediaMapper.Object);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private static JsonObject MediaStanza(Guid mediaAssetId, string altText) =>
        new() { ["id"] = mediaAssetId.ToString(), ["alt_text"] = altText };

    [Test]
    public async Task GetAllAsync_ReturnsAllKnowledgeNodesOfThatNodeType()
    {
        var nodeType = new NodeType { Id = Guid.NewGuid(), Name = "Planet" };
        await _db.NodeType.AddAsync(nodeType);
        await _db.KnowledgeNode.AddRangeAsync(
            new KnowledgeNode { Id = Guid.NewGuid(), NodeTypeId = nodeType.Id, CanonicalName = "Venus" },
            new KnowledgeNode { Id = Guid.NewGuid(), NodeTypeId = nodeType.Id, CanonicalName = "Mercury" });
        await _db.SaveChangesAsync();

        var all = await _repository.GetAllAsync(nodeType.Name);

        Assert.That(all.Select(n => n.CanonicalName), Is.EquivalentTo(new[] { "Mercury", "Venus" }));
    }

    [Test]
    public async Task GetAllAsync_WithNodeTypeNameFilter_ReturnsOnlyMatchingNodes()
    {
        var nodeType1 = new NodeType { Id = Guid.NewGuid(), Name = "Planet" };
        var nodeType2 = new NodeType { Id = Guid.NewGuid(), Name = "Moon" };
        await _db.NodeType.AddRangeAsync(nodeType1, nodeType2);
        await _db.KnowledgeNode.AddRangeAsync(
            new KnowledgeNode { Id = Guid.NewGuid(), NodeTypeId = nodeType1.Id, CanonicalName = "Mercury" },
            new KnowledgeNode { Id = Guid.NewGuid(), NodeTypeId = nodeType2.Id, CanonicalName = "Moon" });
        await _db.SaveChangesAsync();

        var all = await _repository.GetAllAsync(nodeType1.Name);

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
    public async Task GetByIdAsync_WhenExists_PopulatesMediaUsingMapperToJsonKeyedByKey()
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
        var mappedJson = new JsonObject { ["id"] = mediaAssetId.ToString(), ["alt_text"] = "Flag of France" };
        _mediaMapper.Setup(m => m.ToJson(It.Is<KnowledgeNodeMedia>(r => r.Key == "flag"))).Returns(mappedJson);

        var found = await _repository.GetByIdAsync(knowledgeNode.Id);

        Assert.That(found!.Media!.Keys, Is.EquivalentTo(new[] { "flag" }));
        Assert.That(found.Media!["flag"], Is.SameAs(mappedJson));
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
            Attributes = new Dictionary<string, JsonValue> { ["isoCode"] = JsonValue.Create("FR") }
        };

        await _repository.CreateAsync(knowledgeNode);

        var row = await _db.KnowledgeNodeAttribute.AsNoTracking().SingleAsync(a => a.KnowledgeNodeId == knowledgeNode.Id);
        Assert.That(row.Key, Is.EqualTo("isoCode"));
        Assert.That(row.Value!.GetValue<string>(), Is.EqualTo("FR"));
    }

    [Test]
    public async Task CreateAsync_WithMedia_CallsUpdateFromJsonOnEntityAlreadyHavingKnowledgeNodeIdAndKeySet()
    {
        var stanza = MediaStanza(Guid.NewGuid(), "Flag of France");
        var mappedMediaAssetId = Guid.NewGuid();
        _mediaMapper.Setup(m => m.UpdateFromJson(It.IsAny<KnowledgeNodeMedia>(), stanza))
            .Callback<KnowledgeNodeMedia, JsonObject>((media, _) =>
            {
                media.MediaAssetId = mappedMediaAssetId;
                media.AltText = "mapped alt text";
                media.Metadata = stanza;
            });
        var knowledgeNode = new KnowledgeNode
        {
            Id = Guid.NewGuid(),
            CanonicalName = "France",
            Media = new Dictionary<string, JsonObject> { ["flag"] = stanza }
        };

        await _repository.CreateAsync(knowledgeNode);

        var row = await _db.KnowledgeNodeMedia.AsNoTracking().SingleAsync(m => m.KnowledgeNodeId == knowledgeNode.Id);
        Assert.That(row.Key, Is.EqualTo("flag"));
        Assert.That(row.MediaAssetId, Is.EqualTo(mappedMediaAssetId));
        Assert.That(row.AltText, Is.EqualTo("mapped alt text"));
        _mediaMapper.Verify(m => m.UpdateFromJson(It.Is<KnowledgeNodeMedia>(r => r.Key == "flag" && r.KnowledgeNodeId == knowledgeNode.Id), stanza), Times.Once);
    }

    [Test]
    public void CreateAsync_WhenMapperThrowsValidationException_PropagatesItUnchanged()
    {
        var stanza = new JsonObject();
        _mediaMapper.Setup(m => m.UpdateFromJson(It.IsAny<KnowledgeNodeMedia>(), stanza)).Throws(new ValidationException("The media entry 'flag' must include a valid 'id'."));
        var knowledgeNode = new KnowledgeNode
        {
            Id = Guid.NewGuid(),
            CanonicalName = "Mercury",
            Media = new Dictionary<string, JsonObject> { ["flag"] = stanza }
        };

        var ex = Assert.ThrowsAsync<ValidationException>(() => _repository.CreateAsync(knowledgeNode));

        Assert.That(ex!.Message, Is.EqualTo("The media entry 'flag' must include a valid 'id'."));
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
            Attributes = new Dictionary<string, JsonValue>
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
    public async Task UpdateAsync_MediaFullReplace_CallsUpdateFromJsonForExistingAndNewKeysAndRemovesOmittedRow()
    {
        var nodeTypeId = Guid.NewGuid();
        var knowledgeNode = new KnowledgeNode { Id = Guid.NewGuid(), NodeTypeId = nodeTypeId, CanonicalName = "France" };
        var flagAssetId = Guid.NewGuid();
        var photoAssetId = Guid.NewGuid();
        await _db.KnowledgeNode.AddAsync(knowledgeNode);
        await _db.KnowledgeNodeMedia.AddRangeAsync(
            new KnowledgeNodeMedia { KnowledgeNodeId = knowledgeNode.Id, Key = "flag", MediaAssetId = flagAssetId, AltText = "Old flag", Metadata = MediaStanza(flagAssetId, "Old flag") },
            new KnowledgeNodeMedia { KnowledgeNodeId = knowledgeNode.Id, Key = "pronunciation", MediaAssetId = photoAssetId, AltText = "Pronunciation", Metadata = MediaStanza(photoAssetId, "Pronunciation") });
        await _db.SaveChangesAsync();

        var flagStanza = MediaStanza(Guid.NewGuid(), "New flag");
        var photoStanza = MediaStanza(photoAssetId, "Eiffel Tower");
        var mappedPhotoAssetId = Guid.NewGuid();
        _mediaMapper.Setup(m => m.UpdateFromJson(It.IsAny<KnowledgeNodeMedia>(), photoStanza))
            .Callback<KnowledgeNodeMedia, JsonObject>((media, _) =>
            {
                media.MediaAssetId = mappedPhotoAssetId;
                media.AltText = "Eiffel Tower";
                media.Metadata = photoStanza;
            });

        var updated = await _repository.UpdateAsync(new KnowledgeNode
        {
            Id = knowledgeNode.Id,
            NodeTypeId = nodeTypeId,
            CanonicalName = "France",
            Media = new Dictionary<string, JsonObject> { ["flag"] = flagStanza, ["photo"] = photoStanza }
        });

        Assert.That(updated, Is.Not.Null);
        _mediaMapper.Verify(m => m.UpdateFromJson(It.Is<KnowledgeNodeMedia>(r => r.Key == "flag"), flagStanza), Times.Once);
        _mediaMapper.Verify(m => m.UpdateFromJson(It.Is<KnowledgeNodeMedia>(r => r.Key == "photo" && r.KnowledgeNodeId == knowledgeNode.Id), photoStanza), Times.Once);
        var rows = await _db.KnowledgeNodeMedia.AsNoTracking().Where(m => m.KnowledgeNodeId == knowledgeNode.Id).ToListAsync();
        Assert.That(rows.Select(r => r.Key), Is.EquivalentTo(new[] { "flag", "photo" }));
        var photoRow = rows.Single(r => r.Key == "photo");
        Assert.That(photoRow.MediaAssetId, Is.EqualTo(mappedPhotoAssetId));
    }

    [Test]
    public async Task UpdateAsync_WhenMapperThrowsValidationExceptionOnExistingKey_PropagatesItUnchanged()
    {
        var nodeTypeId = Guid.NewGuid();
        var knowledgeNode = new KnowledgeNode { Id = Guid.NewGuid(), NodeTypeId = nodeTypeId, CanonicalName = "France" };
        var flagAssetId = Guid.NewGuid();
        await _db.KnowledgeNode.AddAsync(knowledgeNode);
        await _db.KnowledgeNodeMedia.AddAsync(
            new KnowledgeNodeMedia { KnowledgeNodeId = knowledgeNode.Id, Key = "flag", MediaAssetId = flagAssetId, AltText = "Old flag", Metadata = MediaStanza(flagAssetId, "Old flag") });
        await _db.SaveChangesAsync();

        var invalidStanza = new JsonObject();
        _mediaMapper.Setup(m => m.UpdateFromJson(It.IsAny<KnowledgeNodeMedia>(), invalidStanza)).Throws(new ValidationException("The media entry 'flag' must include a valid 'id'."));

        var ex = Assert.ThrowsAsync<ValidationException>(() => _repository.UpdateAsync(new KnowledgeNode
        {
            Id = knowledgeNode.Id,
            NodeTypeId = nodeTypeId,
            CanonicalName = "France",
            Media = new Dictionary<string, JsonObject> { ["flag"] = invalidStanza }
        }));

        Assert.That(ex!.Message, Is.EqualTo("The media entry 'flag' must include a valid 'id'."));
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
            Attributes = new Dictionary<string, JsonValue> { ["isoCode"] = JsonValue.Create("FR") }
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
            Media = new Dictionary<string, JsonObject> { ["flag"] = MediaStanza(Guid.NewGuid(), "x") }
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
            Media = new Dictionary<string, JsonObject> { ["flag"] = MediaStanza(Guid.NewGuid(), "x") }
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
            Attributes = new Dictionary<string, JsonValue> { ["iso-code"] = JsonValue.Create("FR") }
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
            Media = new Dictionary<string, JsonObject> { ["flag2"] = MediaStanza(Guid.NewGuid(), "x") }
        }));

        Assert.That(ex!.Message, Is.EqualTo("A media key must contain only letters."));
    }
}
