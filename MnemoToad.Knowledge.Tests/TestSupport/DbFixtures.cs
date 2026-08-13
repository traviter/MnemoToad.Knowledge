using MnemoToad.Knowledge.Data;
using MnemoToad.Knowledge.Data.Entities;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Tests.TestSupport;

internal static class DbFixtures
{
    public static async Task<NodeType> CreateNodeTypeAsync(this IAppDbContext db, string? name = null, string? description = null)
    {
        var nodeType = new NodeType { Name = name ?? $"NodeType_{Guid.NewGuid()}", Description = description };
        db.NodeType.Add(nodeType);
        await db.SaveChangesAsync();
        return nodeType;
    }

    public static async Task<KnowledgeNode> CreateKnowledgeNodeAsync(this IAppDbContext db, Guid nodeTypeId, string? canonicalName = null, string? description = null)
    {
        var knowledgeNode = new KnowledgeNode
        {
            NodeTypeId = nodeTypeId,
            CanonicalName = canonicalName ?? $"KnowledgeNode_{Guid.NewGuid()}",
            Description = description
        };
        db.KnowledgeNode.Add(knowledgeNode);
        await db.SaveChangesAsync();
        return knowledgeNode;
    }

    public static async Task<RelationshipType> CreateRelationshipTypeAsync(this IAppDbContext db, string? name = null, string? description = null)
    {
        var relationshipType = new RelationshipType
        {
            Name = name ?? $"RelationshipType_{Guid.NewGuid()}",
            Description = description
        };
        db.RelationshipType.Add(relationshipType);
        await db.SaveChangesAsync();
        return relationshipType;
    }

    public static async Task<KnowledgeRelation> CreateKnowledgeRelationAsync(this IAppDbContext db, Guid sourceNodeId, Guid relationshipTypeId, Guid targetNodeId)
    {
        var knowledgeRelation = new KnowledgeRelation
        {
            SourceNodeId = sourceNodeId,
            RelationshipTypeId = relationshipTypeId,
            TargetNodeId = targetNodeId
        };
        db.KnowledgeRelation.Add(knowledgeRelation);
        await db.SaveChangesAsync();
        return knowledgeRelation;
    }

    public static async Task<KnowledgeNodeAttribute> CreateKnowledgeNodeAttributeAsync(this IAppDbContext db, Guid knowledgeNodeId, string key, JsonValue? value = null)
    {
        var knowledgeNodeAttribute = new KnowledgeNodeAttribute
        {
            KnowledgeNodeId = knowledgeNodeId,
            Key = key,
            Value = value ?? JsonValue.Create($"Value_{Guid.NewGuid()}")
        };
        db.KnowledgeNodeAttribute.Add(knowledgeNodeAttribute);
        await db.SaveChangesAsync();
        return knowledgeNodeAttribute;
    }

    public static async Task<MediaAsset> CreateMediaAssetAsync(this IAppDbContext db, string? url = null)
    {
        var mediaAsset = new MediaAsset { Url = url ?? $"https://example.com/{Guid.NewGuid()}.svg" };
        db.MediaAsset.Add(mediaAsset);
        await db.SaveChangesAsync();
        return mediaAsset;
    }

    public static async Task<KnowledgeNodeMedia> CreateKnowledgeNodeMediaAsync(this IAppDbContext db, Guid knowledgeNodeId, string key, Guid mediaAssetId, string altText, JsonObject? metadata = null)
    {
        var stanza = metadata ?? new JsonObject { ["id"] = mediaAssetId.ToString(), ["alt_text"] = altText };
        var knowledgeNodeMedia = new KnowledgeNodeMedia
        {
            KnowledgeNodeId = knowledgeNodeId,
            Key = key,
            MediaAssetId = mediaAssetId,
            AltText = altText,
            Metadata = stanza
        };
        db.KnowledgeNodeMedia.Add(knowledgeNodeMedia);
        await db.SaveChangesAsync();
        return knowledgeNodeMedia;
    }
}
