using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Data.Entities;

public class KnowledgeNodeAttribute
{
    public Guid KnowledgeNodeId { get; set; }
    public string Key { get; set; } = string.Empty;
    public JsonValue Value { get; set; } = null!;
}
