namespace MnemoToad.Knowledge.Data.Entities;

/// <summary>A catalogued URL — no blob storage, just a link — that a KnowledgeNode's media can point at.</summary>
public class MediaAsset
{
    /// <summary>The MediaAsset's id.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The asset's URL.</summary>
    public string Url { get; set; } = string.Empty;
}
