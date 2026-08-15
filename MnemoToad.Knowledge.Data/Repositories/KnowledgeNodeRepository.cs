using Microsoft.EntityFrameworkCore;
using MnemoToad.Knowledge.Data.Common;
using MnemoToad.Knowledge.Data.Entities;
using Npgsql;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Data.Repositories;

public class KnowledgeNodeRepository : IKnowledgeNodeRepository
{
    private readonly IAppDbContext _db;
    private readonly IEntityJsonMapper<KnowledgeNodeMedia> _mediaMapper;

    public KnowledgeNodeRepository(IAppDbContext db, IEntityJsonMapper<KnowledgeNodeMedia> mediaMapper)
    {
        _db = db;
        _mediaMapper = mediaMapper;
    }

    public Task<List<KnowledgeNode>> GetAllAsync(Guid nodeTypeId) =>
        _db.KnowledgeNode.Where(n => n.NodeTypeId == nodeTypeId).OrderBy(n => n.CanonicalName).ToListAsync();

    public async Task<KnowledgeNode?> GetByIdAsync(Guid id)
    {
        var node = await _db.KnowledgeNode.FindAsync(id);
        if (node is null) return null;

        var rows = await _db.KnowledgeNodeAttribute.Where(a => a.KnowledgeNodeId == id).ToListAsync();
        node.Attributes = rows.ToDictionary(r => r.Key, r => r.Value);
        node.Media = await GetMediaAsync(id);
        return node;
    }

    public async Task<KnowledgeNode> CreateAsync(KnowledgeNode knowledgeNode)
    {
        knowledgeNode.Attributes ??= new();
        knowledgeNode.Media ??= new();
        _db.KnowledgeNode.Add(knowledgeNode);

        foreach (var (key, value) in knowledgeNode.Attributes)
        {
            _db.KnowledgeNodeAttribute.Add(new KnowledgeNodeAttribute
            {
                KnowledgeNodeId = knowledgeNode.Id,
                Key = key,
                Value = value
            });
        }

        foreach (var (key, stanza) in knowledgeNode.Media)
        {
            _db.KnowledgeNodeMedia.Add(BuildMedia(knowledgeNode.Id, key, stanza));
        }

        await SaveChangesAsync();
        knowledgeNode.Media = await GetMediaAsync(knowledgeNode.Id);
        return knowledgeNode;
    }

    public async Task<KnowledgeNode?> UpdateAsync(KnowledgeNode knowledgeNode)
    {
        var existing = await _db.KnowledgeNode.FindAsync(knowledgeNode.Id);
        if (existing is null) return null;

        knowledgeNode.Attributes ??= new();
        knowledgeNode.Media ??= new();
        existing.NodeTypeId = knowledgeNode.NodeTypeId;
        existing.CanonicalName = knowledgeNode.CanonicalName;
        existing.Description = knowledgeNode.Description;

        var currentRows = await _db.KnowledgeNodeAttribute.Where(a => a.KnowledgeNodeId == existing.Id).ToListAsync();

        foreach (var row in currentRows.Where(r => !knowledgeNode.Attributes.ContainsKey(r.Key)))
            _db.KnowledgeNodeAttribute.Remove(row);

        foreach (var (key, value) in knowledgeNode.Attributes)
        {
            var existingRow = currentRows.FirstOrDefault(r => r.Key == key);
            if (existingRow is not null)
                existingRow.Value = value;
            else
                _db.KnowledgeNodeAttribute.Add(new KnowledgeNodeAttribute { KnowledgeNodeId = existing.Id, Key = key, Value = value });
        }

        var currentMediaRows = await _db.KnowledgeNodeMedia.Where(m => m.KnowledgeNodeId == existing.Id).ToListAsync();

        foreach (var row in currentMediaRows.Where(r => !knowledgeNode.Media.ContainsKey(r.Key)))
            _db.KnowledgeNodeMedia.Remove(row);

        foreach (var (key, stanza) in knowledgeNode.Media)
        {
            var existingRow = currentMediaRows.FirstOrDefault(r => r.Key == key);
            if (existingRow is not null)
                UpdateMedia(key, stanza, existingRow);
            else
                _db.KnowledgeNodeMedia.Add(BuildMedia(existing.Id, key, stanza));
        }

        await SaveChangesAsync();
        existing.Attributes = knowledgeNode.Attributes;
        existing.Media = await GetMediaAsync(existing.Id);
        return existing;
    }

    private async Task<Dictionary<string, JsonObject>> GetMediaAsync(Guid knowledgeNodeId)
    {
        var rows = await _db.KnowledgeNodeMedia.Where(m => m.KnowledgeNodeId == knowledgeNodeId).ToListAsync();
        return rows.ToDictionary(r => r.Key, r => _mediaMapper.ToJson(r));
    }

    private KnowledgeNodeMedia BuildMedia(Guid knowledgeNodeId, string key, JsonObject stanza)
    {
        var media = new KnowledgeNodeMedia { KnowledgeNodeId = knowledgeNodeId, Key = key };
        UpdateMedia(key, stanza, media);
        return media;
    }

    private void UpdateMedia(string key, JsonObject stanza, KnowledgeNodeMedia existing)
    {
        try
        {
            _mediaMapper.UpdateFromJson(stanza, existing);
        }
        catch (ValidationException ex)
        {
            throw new ValidationException($"The media entry '{key}' {ex.Message}");
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            return await _db.ExecuteDeleteAsync(_db.KnowledgeNode.Where(n => n.Id == id)) > 0;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
            TableName: "knowledge_relation"
        })
        {
            throw new ValidationException("The KnowledgeNode cannot be deleted because it is referenced by one or more KnowledgeRelations.");
        }
    }

    private async Task SaveChangesAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "uq_knowledge_node_node_type_id_canonical_name"
        })
        {
            throw new ValidationException("A KnowledgeNode with the same NodeType and CanonicalName already exists.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "pk_knowledge_node_attribute"
        })
        {
            throw new ValidationException("An attribute with that key already exists for this KnowledgeNode.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
            TableName: "knowledge_node"
        })
        {
            throw new ValidationException("The specified NodeType does not exist.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "pk_knowledge_node_media"
        })
        {
            throw new ValidationException("A media entry with that key already exists for this KnowledgeNode.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "uq_knowledge_node_media_media_asset_id"
        })
        {
            throw new ValidationException("The specified MediaAsset is already linked to another KnowledgeNode.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.CheckViolation,
            ConstraintName: "ck_knowledge_node_attribute_key"
        })
        {
            throw new ValidationException("An attribute key must contain only letters.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.CheckViolation,
            ConstraintName: "ck_knowledge_node_media_key"
        })
        {
            throw new ValidationException("A media key must contain only letters.");
        }
    }
}
