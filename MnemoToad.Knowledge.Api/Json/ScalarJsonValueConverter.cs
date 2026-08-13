using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MnemoToad.Knowledge.Api.Json;

// The built-in JsonValueConverter throws InvalidOperationException (not JsonException) when the
// token is an object/array, which ASP.NET Core's SystemTextJsonInputFormatter doesn't translate into
// a 400 — it only catches JsonException. This converter rejects the same inputs but as a JsonException,
// so a bad attribute value comes back as a normal 400 instead of a 500.
public class ScalarJsonValueConverter : JsonConverter<JsonValue>
{
    // Without this, System.Text.Json intercepts a JSON null token before Read() ever runs and
    // assigns null directly — Read()'s own null rejection would never fire.
    public override bool HandleNull => true;

    public override JsonValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        if (document.RootElement.ValueKind is not (JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False))
            throw new JsonException("Attribute values must be a string, number, or boolean — null, objects, and arrays are not supported.");

        return JsonValue.Create(document.RootElement.Clone());
    }

    public override void Write(Utf8JsonWriter writer, JsonValue value, JsonSerializerOptions options) =>
        value.WriteTo(writer, options);
}
