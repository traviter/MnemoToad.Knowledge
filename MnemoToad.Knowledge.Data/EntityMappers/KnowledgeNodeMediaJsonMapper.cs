using MnemoToad.Knowledge.Data.Common;
using MnemoToad.Knowledge.Data.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Data.EntityMappers;

public class KnowledgeNodeMediaJsonMapper : IEntityJsonMapper<KnowledgeNodeMedia>
{
    public JsonObject ToJson(KnowledgeNodeMedia entity)
    {
        var json = entity.Metadata is null ? new JsonObject() : (JsonObject)entity.Metadata.DeepClone();
        json["id"] = entity.MediaAssetId.ToString();
        json["alt_text"] = entity.AltText;
        return json;
    }

    public void UpdateFromJson(KnowledgeNodeMedia entity, JsonObject json)
    {
        var (mediaAssetId, altText) = ExtractMediaFields(entity.Key, json);
        entity.MediaAssetId = mediaAssetId;
        entity.AltText = altText;
        entity.Metadata = json;
    }

    private static (Guid MediaAssetId, string AltText) ExtractMediaFields(string key, JsonObject? stanza)
    {
        if (stanza is null
            || !stanza.TryGetPropertyValue("id", out var idNode)
            || idNode is not JsonValue idValue
            || !idValue.TryGetValue<string>(out var idString)
            || !Guid.TryParse(idString, out var mediaAssetId))
        {
            throw new ValidationException($"The media entry '{key}' must include a valid 'id'.");
        }

        if (!stanza.TryGetPropertyValue("alt_text", out var altNode)
            || altNode is not JsonValue altValue
            || !altValue.TryGetValue<string>(out var altText))
        {
            throw new ValidationException($"The media entry '{key}' must include a valid 'alt_text'.");
        }

        return (mediaAssetId, altText);
    }
}
