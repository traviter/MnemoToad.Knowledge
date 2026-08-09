using System.ComponentModel.DataAnnotations;

namespace MnemoToad.Knowledge.Api.Contracts;

/// <summary>The body for creating or replacing a MediaAsset.</summary>
/// <param name="Url">The asset's URL. Required.</param>
public record MediaAssetRequest([Required] string Url);
