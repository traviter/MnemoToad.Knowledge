using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Data.Entities;

public static class KnowledgeNodeMediaExtensions
{
    public static JsonObject ToJson(this KnowledgeNodeMedia media)
    {
        var json = media.Metadata is null ? new JsonObject() : (JsonObject)media.Metadata.DeepClone();
        json["id"] = media.MediaAssetId.ToString();
        json["alt_text"] = media.AltText;
        return json;
    }
}
